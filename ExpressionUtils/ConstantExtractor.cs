using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MiaPlaza.ExpressionUtils {
	internal class ConstantExtractor : ExpressionVisitor {
		public struct ExtractionResult {
			public readonly Expression ConstantfreeExpression;
			public readonly IReadOnlyCollection<ParameterExpression> Parameters;
			public readonly IReadOnlyList<object> ExtractedConstants;

			public ExtractionResult(Expression constantFreeExpression, IReadOnlyCollection<ParameterExpression> parameters, IReadOnlyList<object> extractedConstants) {
				ConstantfreeExpression = constantFreeExpression;
				Parameters = parameters;
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
			return new ExtractionResult(constantFreeBody, visitor.parameters, visitor.constants);
		}

		private ConstantExtractor(bool rewriteTree) {
			this.rewriteTree = rewriteTree;
		}

		readonly List<object> constants = new List<object>();
		readonly List<ParameterExpression> parameters = new List<ParameterExpression>();
		readonly bool rewriteTree;

#if __MonoCS__
		protected override Expression VisitBinary(BinaryExpression node) {
			if ((node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual)
				&& node.IsLifted) {
				return visitLiftedEquality(node);
			} else {
				return base.VisitBinary(node);
			}
		}

		/// <summary>
		/// Extract constants from a lifted equality/inequality check in a way that works around a bug in Mono.
		/// Consider the following code:
		/// Expression<Func<DateTime?,bool>> p = date => date == null;
		/// In Microsoft's implementation the binary expression essentially gets rendered to:
		/// Expression.Equal(Parameter("date"), Constant(null, typeof(DateTime?)));
		/// In Mono, however, this gets rendered to:
		/// Expression.Equal(Parameter("date"), Constant(null, typeof(System.Object));
		/// 
		/// The latter is a problem when we replace bits of the expression, say we replace
		/// Constant(null) with something else of type Object in this visitor. Once this replacement happens,
		/// BinaryExpression.Update() gets called which tries to find the correct == operator for a DateTime? and a
		/// System.Object. But no such operator exists. So we need to explicitly cast the operands to the right
		/// type in equality/inequality checks that involve nullables, (i.e., in "lifted" binary expressions.)
		/// </summary>
		private Expression visitLiftedEquality(BinaryExpression node) {
			var newLeft = base.Visit(node.Left);
			var newRight = base.Visit(node.Right);
			if (node.Conversion != null) {
				throw new ArgumentException("node", "node must be an equality operator, not a coalesce or a compound assignment.");
			}

			if (object.ReferenceEquals(newLeft, node.Left) && object.ReferenceEquals(newRight, node.Right)) {
				// left and right are unchanged; no need to do any special treatment
				return node;
			}

			bool leftIsNullable = Nullable.GetUnderlyingType(newLeft.Type) != null;
			bool rightIsNullable = Nullable.GetUnderlyingType(newRight.Type) != null;

			if (!leftIsNullable && !rightIsNullable) {
				throw new ArgumentException("node", "node must be a lifted operator but none of the operands is nullable.");
			}

			if (!leftIsNullable) {
				// cast the left hand side from System.Object to the correct nullable type
				newLeft = Expression.Convert(newLeft, node.Right.Type);
			}
			if (!rightIsNullable) {
				// cast the right hand side from System.Object to the correct nullable type
				newRight = Expression.Convert(newRight, node.Left.Type);
			}

			return node.Update(newLeft, null, newRight);
		}
#endif

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
