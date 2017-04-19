using BenchmarkDotNet.Attributes;
using MiaPlaza.ExpressionUtils;
using System;
using System.Linq.Expressions;

namespace ExpressionUtilsPerf {
	/// <summary>
	/// Determines the performance of partial evaluation of expression trees.
	/// </summary>
	public class PartialEvaluation {

		private static bool method(int a, int b) {
			return a % b == 1;
		}

		private static int property { get { return 42; } }

		static readonly Expression<Func<int, bool>> staticReadonlyExpression = i => i == 0 || method(i + 1, 15)&& method(16, 15) && i < property;

		[Benchmark]
		public void StaticReadonlyPartialEvaluation() {
			PartialEvaluator.PartialEvalBody(staticReadonlyExpression, MiaPlaza.ExpressionUtils.Evaluating.ExpressionInterpreter.Instance);
		}

		static int x = 16;
		static readonly Expression<Func<int, bool>> staticReadonlyClosureExpression = i => i == 0 || method(i + 1, 15) && method(x, 15) && i < property;

		[Benchmark]
		public void StaticReadonlyClosurePartialEvaluation() {
			PartialEvaluator.PartialEvalBody(staticReadonlyClosureExpression, MiaPlaza.ExpressionUtils.Evaluating.ExpressionInterpreter.Instance);
		}

		[Benchmark]
		public void ClosurePartialEvaluation() {
			int x = 16;
			Expression<Func<int, bool>> closureExpression = i => i == 0 || method(i + 1, 15) && method(x, 15) && i < property;

			PartialEvaluator.PartialEvalBody(closureExpression, MiaPlaza.ExpressionUtils.Evaluating.ExpressionInterpreter.Instance);
		}
	}
}
