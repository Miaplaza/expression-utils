using BenchmarkDotNet.Attributes;
using MiaPlaza.ExpressionUtils;
using MiaPlaza.ExpressionUtils.Evaluating;
using MiaPlaza.ExpressionUtils.Expanding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionUtilsPerf {
	/// <summary>
	/// Determines the performance of partial evaluation of expression trees.
	/// </summary>
	public class ExpressionStructureComparing {

		static Expression<Func<int, int, int>> buildSimpleExpression(int c)
			=> (x, y) => x + y * c;

		static int square(int x) => x * x;

		static Expression<Func<int, int, int>> buildComplexExpression(int c)
			=> (x, y) => x % 2 == 1 ? x ^ y + x * square(y) | c : x & c;

		public enum ExpressionTypes {
			Simple,
			Complex,
		}

		[Params(ExpressionTypes.Simple, ExpressionTypes.Complex)]
		public ExpressionTypes ExpressionType { get; set; }

		[Setup]
		public void Setup() {
			Func<int, Expression<Func<int, int, int>>> builder;

			switch (ExpressionType) {
				case ExpressionTypes.Simple:
					builder = buildSimpleExpression;
					break;
				case ExpressionTypes.Complex:
					builder = buildComplexExpression;
					break;
				default:
					throw new NotSupportedException(ExpressionType.ToString());
			}

			expression = PartialEvaluator.PartialEvalBody(builder(5), ExpressionInterpreter.Instance);
			similarExpression = PartialEvaluator.PartialEvalBody(builder(5), ExpressionInterpreter.Instance);
			nonSimilarExpression = PartialEvaluator.PartialEvalBody(builder(42), ExpressionInterpreter.Instance);
		}

		Expression<Func<int, int, int>> expression;
		Expression<Func<int, int, int>> similarExpression;
		Expression<Func<int, int, int>> nonSimilarExpression;

		[Benchmark]
		public bool CompareSameExpressions()
			=> expression.StructuralIdentical(expression);

		[Benchmark]
		public bool CompareSimilarExpressions()
			=> expression.StructuralIdentical(similarExpression);
		
		[Benchmark]
		public bool CompareNonSimilarExpressions()
			=> expression.StructuralIdentical(nonSimilarExpression);
	}
}
