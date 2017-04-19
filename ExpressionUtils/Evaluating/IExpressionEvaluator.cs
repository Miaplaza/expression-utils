using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MiaPlaza.ExpressionUtils.Evaluating {
	/// <summary>
	/// Abstracts a technique to get the value of an expression.
	/// </summary>
	public interface IExpressionEvaluator {
		/// <summary>
		/// Get the value of an expression that does not contain any unbound <see cref="ParameterExpression"/>s
		/// in it. 
		/// </summary>
		object Evaluate(Expression unparametrizedExpression);

		/// <summary>
		/// Returns a delegate that can be used to get the value of a parametrized expression.
		/// </summary>
		/// <remarks>
		/// The "value" of a <see cref="LambdaExpression"/> is a delegate. 
		/// </remarks>
		VariadicArrayParametersDelegate EvaluateLambda(LambdaExpression lambdaExpression);
	}
}
