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
	/// This does not test cases when different expressions should not use same cached result.
	/// (does not test cases when cache shouldnt be used, <see cref="ExpressionEvaluation"/> already does that to seme degree)
	/// </remarks>
	public class CachedExpressionCompilerTest {

		static bool someMethod(int value) {
			throw new Exception();
		}

		/// <summary>
		/// Provides test cases with structure-identical expressions that differ only in their constants
		/// </summary>
		private static object[] getStructureIdenticalExpressions() {
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

		/// <summary>
		/// Tests that the caching mechanism operates based on the structure of expressions.
		/// Ensures that structurally identical expressions are cached and recognized as such,
		/// regardless of differences in constants or captured variables within the expressions.
		/// This test method tests cases when input expressions are lambdas.
		/// </summary>
		[TestCaseSource(nameof(getStructureIdenticalExpressions))]
		public void CachedCompileExpression_ShouldCacheExpressionsByStructure_WhenExpressionsAreLambdas(LambdaExpression one, LambdaExpression two) {
			testExpressionsCached(one, two);
		}

		/// <summary>
		/// Provides test cases with unparameterized expressions derived from structure-identical expressions by taking bodies from lambdas with no parameters
		/// </summary>
		private static object[] getStructureIdenticalUnparametrizedExpressions() {
			return getStructureIdenticalExpressions()
				.Select(lambdas => lambdas as object[])
				// Take only unparametrized expressions
				.Where(lambdas => (lambdas[0] as LambdaExpression).Parameters.Count == 0)
				.Select(lambdas => new[] { (lambdas[0] as LambdaExpression).Body, (lambdas[1] as LambdaExpression).Body })
				.ToArray();
		}

		/// <summary>
		/// Tests that the caching mechanism operates based on the structure of expressions.
		/// Ensures that structurally identical expressions are cached and recognized as such,
		/// regardless of differences in constants or captured variables within the expressions.
		/// This test method tests cases when input expressions are non lambdas.
		/// </summary>
		[TestCaseSource(nameof(getStructureIdenticalUnparametrizedExpressions))]
		public void CachedCompileExpression_ShouldCacheExpressionsByStructure_WhenExpressionsAreNotLambdas(Expression one, Expression two) {
			testExpressionsCached(one, two);
		}

		/// <summary>
		/// Tests that if you run <see cref="CachedExpressionCompiler.CachedCompileExpression(Expression)"/> on any of two provided expressions, cache will contain the result for the other expression
		/// </summary>
		private void testExpressionsCached(Expression one, Expression two) {
			CachedExpressionCompiler.Instance.ClearCache();
			CachedExpressionCompiler.Instance.CachedCompileExpression(one);
			Assert.IsTrue(CachedExpressionCompiler.Instance.IsCached(two));

			CachedExpressionCompiler.Instance.ClearCache();
			CachedExpressionCompiler.Instance.CachedCompileExpression(two);
			Assert.IsTrue(CachedExpressionCompiler.Instance.IsCached(one));
		}
	}
}
