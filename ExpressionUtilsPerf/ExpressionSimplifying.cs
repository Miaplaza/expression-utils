using BenchmarkDotNet.Attributes;
using MiaPlaza.ExpressionUtils;
using System;
using System.Linq.Expressions;

namespace ExpressionUtilsPerf {
	/// <summary>
	/// Tests the performance of the expression <see cref="Simplifier"/>
	/// </summary>
	public class ExpressionSimplifying {

		/// <summary>
		/// Dummy class to construct a null reference property access.
		/// </summary>
		class Foo {
			public readonly int Bar = 42;
		}

		Expression<Func<int, bool>> nothingToSimplifyExpression = i => i > 15;
		Expression<Func<int, bool>> nullCheckSimplifyExpression = null;
		Expression<Func<int, bool>> nastySimplifyExpression = null;

		[Setup]
		public void Setup() {
			Foo nullRef = null;

			Expression<Func<int, bool>> rawNullCheckSimplifyExpression = i => nullRef == null || nullRef.Bar == i;
			Expression<Func<int, bool>> rawNastySimplifyExpression = i => ((i == 13 || (i % 2) == 0) || nullRef == null) || nullRef.Bar == i;

			// These expressions capture the local nullRef variable and therefore need partial evaluation. 
			// Using 'null' in the expressions directly will cause the C# compiler to simplify the expressions
			// itself from 'null == null' to 'true', leaving no work for the simplifier to do.
			nullCheckSimplifyExpression = PartialEvaluator.PartialEvalBody(rawNullCheckSimplifyExpression, MiaPlaza.ExpressionUtils.Evaluating.ExpressionInterpreter.Instance);
			nastySimplifyExpression = PartialEvaluator.PartialEvalBody(rawNastySimplifyExpression, MiaPlaza.ExpressionUtils.Evaluating.ExpressionInterpreter.Instance);
		}

		[Benchmark]
		public Expression NoSimplification() {
			return Simplifier.SimplifyBody(nothingToSimplifyExpression);
		}
		
		[Benchmark]
		public Expression NullCheckSimplification() {
			return Simplifier.SimplifyBody(nullCheckSimplifyExpression);
		}

		[Benchmark]
		public Expression NastySimplification() {
			return Simplifier.SimplifyBody(nastySimplifyExpression);
		}
	}
}
