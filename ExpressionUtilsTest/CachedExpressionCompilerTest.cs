using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiaPlaza.ExpressionUtils;
using System.Linq.Expressions;
using MiaPlaza.ExpressionUtils.Evaluating;
using NUnit.Framework;
using System.IO;

namespace MiaPlaza.Test.ExpressionUtilsTest {
	/// <summary>
	/// Tests that <see cref="CachedExpressionCompiler"/> actually caches identical expressions.
	/// </summary>
	/// <remarks>
	/// This does not test cases when different expressions should not use same cached result. <see cref="ExpressionEvaluation"/> already does that.
	/// </remarks>
	class CachedExpressionCompilerTest {

		static bool someMethod(int value) {
			throw new Exception();
		}

		static object[] getStructureIdenticalExpressions()
		{
			Expression<Func<int>> getCapturedVariableExpression(int capturedVariableValue) {
				int capturedVariable = capturedVariableValue;
				return () => capturedVariable;
			}

			return new object[]
			{
				// Different constants
				new object[] { (Expression<Func<bool>>)(() => true), (Expression<Func<bool>>)(() => false) },
				// Captured variable with different values
				new object[] { getCapturedVariableExpression(42), getCapturedVariableExpression(11) },
				// Method call with different constant parameters
				new object[] { (Expression<Func<bool>>)(() => someMethod(3)), (Expression<Func<bool>>)(() => someMethod(16)) },
				// Different constant values being converted to a another type. Trees are manually constructed to prevent compile time simplification.
				new object[] { Expression.Lambda(Expression.Convert(Expression.Constant(FileAccess.Read), typeof(int))), Expression.Lambda(Expression.Convert(Expression.Constant(FileAccess.Write), typeof(int))) },
				// Initialization of a new constant object of same type with same literal structure but different literal values
				new object[] { (Expression<Func<int[]>>)(() => new[] { 2, 3, 5, 7 }), (Expression<Func<int[]>>)(() => new[] { 1, 1, 2, 3 }) },
				// Many constants
				new object[] { (Expression<Func<int, bool>>)((i) => i < 43 && i > 12 && (i % 3) == 2 && i != 15 || i == 0), (Expression<Func<int, bool>>)((i) => i < 42 && i > 11 && (i % 2) == 1 && i != 14 || i == -1) },
			};
		}

		[TestCaseSource(nameof(getStructureIdenticalExpressions))]
		public void TestLambdaExpressionCaching(LambdaExpression one, LambdaExpression two) {
			CachedExpressionCompiler.Instance.ClearCache();
			CachedExpressionCompiler.Instance.CachedCompileExpression(one);
			Assert.IsTrue(CachedExpressionCompiler.Instance.IsCached(two));

			CachedExpressionCompiler.Instance.ClearCache();
			CachedExpressionCompiler.Instance.CachedCompileExpression(two);
			Assert.IsTrue(CachedExpressionCompiler.Instance.IsCached(one));
		}

		static object[] getStructureIdenticalUnparametrizedExpressions()
		{
            return getStructureIdenticalExpressions()
				.SkipLast(1)
				.Select(lambdas => lambdas as object[])
				.Select(lambdas => new[] { (lambdas[0] as LambdaExpression).Body, (lambdas[1] as LambdaExpression).Body })
				.ToArray();
		}

		[TestCaseSource(nameof(getStructureIdenticalUnparametrizedExpressions))]
		public void TestUnparametrizedExpressionCaching(Expression one, Expression two)
		{
			CachedExpressionCompiler.Instance.ClearCache();
			CachedExpressionCompiler.Instance.CachedCompileExpression(one);
			Assert.IsTrue(CachedExpressionCompiler.Instance.IsCached(two));

			CachedExpressionCompiler.Instance.ClearCache();
			CachedExpressionCompiler.Instance.CachedCompileExpression(two);
			Assert.IsTrue(CachedExpressionCompiler.Instance.IsCached(one));
		}
	}
}
