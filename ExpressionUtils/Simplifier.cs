using System.Linq.Expressions;

namespace MiaPlaza.ExpressionUtils {

	/// <summary>
	/// Simplifies an expression by removing unnecessary Binary- and Conditional-Expressions.
	/// </summary>
	public class Simplifier : ExpressionVisitor {

		public static Expression<D> SimplifyBody<D>(Expression<D> expFunc) {
			return expFunc.Update(Simplify(expFunc.Body), expFunc.Parameters);
		}

		public static LambdaExpression SimplifyBody(LambdaExpression expFunc) {
			return Expression.Lambda(Simplify(expFunc.Body), expFunc.Parameters);
		}

		public static Expression Simplify(Expression expression) {
			return new Simplifier().Visit(expression);
		}

		Simplifier() { }

		protected override Expression VisitBinary(BinaryExpression node) {
			node = visitChildren(node);

			if (node.NodeType == ExpressionType.AndAlso) {
				if (node.Left.IsConstant(false) || node.Right.IsConstant(true)) {
					return node.Left;
				}
				if (node.Left.IsConstant(true) || node.Right.IsConstant(false)) {
					return node.Right;
				}
			}

			if (node.NodeType == ExpressionType.OrElse) {
				if (node.Left.IsConstant(true) || node.Right.IsConstant(false)) {
					return node.Left;
				}
				if (node.Left.IsConstant(false) || node.Right.IsConstant(true)) {
					return node.Right;
				}
			}

			return node;
		}

		private BinaryExpression visitChildren(BinaryExpression node) {
			var left = Visit(node.Left);
			var right = Visit(node.Right);

			if (left != node.Left || right != node.Right) {
				node = Expression.MakeBinary(node.NodeType, left, right);
			}
			return node;
		}

		protected override Expression VisitConditional(ConditionalExpression node) {
			node = visitChildren(node);

			if (node.Test.IsConstant(true)) {
				return node.IfTrue;
			}

			if (node.Test.IsConstant(false)) {
				return node.IfFalse;
			}

			return node;
		}

		private ConditionalExpression visitChildren(ConditionalExpression node) {
			var test = Visit(node.Test);
			var ifTrue = Visit(node.IfTrue);
			var ifFalse = Visit(node.IfFalse);

			if (test != node.Test || ifTrue != node.IfTrue || ifFalse != node.IfFalse) {
				node = Expression.Condition(test, ifTrue, ifFalse);
			}
			return node;
		}
	}

}
