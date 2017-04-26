using MiaPlaza.ExpressionUtils.Evaluating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MiaPlaza.ExpressionUtils.Expanding {
	/// <summary>
	/// Rewrites an expression of type <see cref="EXP"/> to another expression.
	/// </summary>
	public abstract class ExpressionExpander<EXP> where EXP : Expression {
		public abstract Expression Expand(EXP expr, IExpressionEvaluator evaluator);
	}
}
