using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MiaPlaza.ExpressionUtils {
	class ConstantExtractor : ExpressionVisitor {
		public struct ExtractionResult {
			public readonly LambdaExpression ConstantfreeExpression;
			public readonly IReadOnlyList<object> ExtractedConstants;

			public ExtractionResult(LambdaExpression constantFreeExpression, IReadOnlyList<object> extractedConstants) {
				ConstantfreeExpression = constantFreeExpression;
				ExtractedConstants = extractedConstants;
			}
		}

		/// <summary>
		/// Extracts all constants from an expression (including closures) without rewriting the expression tree.
		/// </summary>
		public static IReadOnlyList<object> ExtractConstantsOnly(Expression expression) {
			var visitor = new ConstantExtractor(rewriteTree: false);
			visitor.Visit(expression);
			return visitor.constants;
		}

		/// <summary>
		/// Extracts all constants from an expression (including closures) and returns a lambda expression that takes all these constants as parameters.
		/// </summary>
		public static ExtractionResult ExtractConstants(Expression expression) {
			var visitor = new ConstantExtractor(rewriteTree: true);
			var constantFreeBody = visitor.Visit(expression);
			return new ExtractionResult(Expression.Lambda(constantFreeBody, visitor.parameters), visitor.constants);
		}

		private ConstantExtractor(bool rewriteTree) {
			this.rewriteTree = rewriteTree;
		}

		readonly List<object> constants = new List<object>();
		readonly List<ParameterExpression> parameters = new List<ParameterExpression>();
		readonly bool rewriteTree;

		protected override Expression VisitConstant(ConstantExpression constant) {
			constants.Add(constant.Value);

			if (rewriteTree) {
				var parameter = Expression.Parameter(constant.Type, "p" + constants.Count);
				parameters.Add(parameter);

				return parameter;
			} else {
				return constant;
			}
		}
	}
}
