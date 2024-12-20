using System;
using System.Linq.Expressions;

namespace MiaPlaza.ExpressionUtils {
	/// <summary>
	/// A visitor class that traverses an expression tree and replaces constant values
	/// with alternative expressions as defined by a provided replacement function.
	/// </summary>
	internal class ConstantValueReplacer : ExpressionVisitor {
		private readonly Func<ConstantExpression, Expression> replacer;

		/// <summary>
		/// Replaces constants in <paramref name="expression"/> tree with values returned by <paramref name="replacer"/>
		/// </summary>
		public static Expression ReplaceConstants(Expression expression, Func<ConstantExpression, Expression> replacer) {
			var visitor = new ConstantValueReplacer(replacer);
			return visitor.Visit(expression);
		}

		private ConstantValueReplacer(Func<ConstantExpression, Expression> replacer) {
			this.replacer = replacer;
		}

		protected override Expression VisitConstant(ConstantExpression constant) {
			return replacer(constant);
		}
	}
}
