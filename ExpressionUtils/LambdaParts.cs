using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MiaPlaza.ExpressionUtils {

	/// <summary>
	/// Sometimes we don't have actual <see cref="LambdaExpression"/> object, but instead <see cref="LambdaExpression.Body"/> and <see cref="LambdaExpression.Parameters"/> that represent lambda.
	/// We could call <see cref="Expression.Lambda(Expression, ParameterExpression[])"/> to get it, but such calls can be expensive due to locking that is used inside.
	/// So this object plays the role of a container that holds the data that is needed to represent a lambda expression.
	/// </summary>
	/// <remarks>
	/// Before <see cref="LambdaExpression"/> objects were used to fullfill that role. This was added to replace those.
	/// </remarks>
	public class LambdaParts {
		public Expression Body { get; set; }
		public IReadOnlyCollection<ParameterExpression> Parameters { get; set; }

		public static implicit operator LambdaParts(LambdaExpression lambda) 
			=> lambda is null ? null : new LambdaParts { Body = lambda.Body, Parameters = lambda.Parameters };
	}
}
