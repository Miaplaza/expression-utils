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
		/// Works like calling <see cref="Delegate.DynamicInvoke(object[])"/> on <see cref="Expression{TDelegate}.Compile"/>.
		/// </summary>
		/// <remarks>
		/// The "value" of a <see cref="LambdaExpression"/> is a delegate. 
		/// </remarks>
		VariadicArrayParametersDelegate EvaluateLambda(LambdaExpression lambdaExpression);

		/// <summary>
		/// Returns a typed delegate that can be used to get the value of a parametrized expression.
		/// Works like calling <see cref="Expression{TDelegate}.Compile"/>.
		/// </summary>
		/// <remarks>
		/// The "value" of an <see cref="Expression{TDelegate}"/> is a delegate of that type. 
		/// </remarks>
		DELEGATE EvaluateTypedLambda<DELEGATE>(Expression<DELEGATE> expression) where DELEGATE : class;
	}
}
