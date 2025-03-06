using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MiaPlaza.ExpressionUtils {
	/// <summary>
	/// Replaces all parameters in an lambda-expression by other expressions, i.e., given a lambda
	/// <c>(t0, …, tn) → f(t0,…,tn)</c> and expressions <c>(x0,…,xn)</c>, it returns the expression <c>f(x0,…,xn)</c>.
	/// </summary>
	/// <remarks>
	/// Does nothing else than that, esepicially does not change the structure of the expression or removes closures.false
	/// If you want that and other optimizations, use see <cref="ParameterSubstituter" />
	/// TODO: Make this just a static class and not a visitor. This should not be an expression visitor as it would break if 
	/// you visit LambdaExpression and try to replace parameter with constant. See: https://github.com/Miaplaza/expression-utils/issues/33
	/// </remarks>
	public class SimpleParameterSubstituter : ExpressionVisitor {
		public static Expression SubstituteParameter(LambdaExpression expression, params Expression[] replacements)
			=> SubstituteParameter(expression, replacements as IReadOnlyCollection<Expression>);

		public static Expression SubstituteParameter(LambdaExpression expression, IEnumerable<Expression> replacements)
			=> SubstituteParameter(expression, replacements.ToList());

		public static Expression SubstituteParameter(LambdaExpression expression, IReadOnlyCollection<Expression> replacements) {
			if (expression == null) {
				throw new ArgumentNullException(nameof(expression));
			}

			if (replacements == null) {
				throw new ArgumentNullException(nameof(replacements));
			}

			if (expression.Parameters.Count != replacements.Count) {
				throw new ArgumentException($"Replacement count does not match parameter count ({replacements.Count} vs {expression.Parameters.Count})");
			}

			var dict = new Dictionary<ParameterExpression, Expression>();

			foreach (var tuple in expression.Parameters.Zip(replacements, (p, r) => new { parameter = p, replacement = r })) {
				if (!tuple.parameter.Type.IsAssignableFrom(tuple.replacement.Type)) {
					throw new ArgumentException($"The expression {tuple.replacement} cannot be used as replacement for the parameter {tuple.parameter}.");
				}
				dict[tuple.parameter] = tuple.replacement;
			}

			return new SimpleParameterSubstituter(dict).Visit(expression.Body);
		}

		public static Expression SubstituteParameter(Expression expression, IReadOnlyDictionary<ParameterExpression, Expression> replacements)
			=> new SimpleParameterSubstituter(replacements).Visit(expression);

		readonly IReadOnlyDictionary<ParameterExpression, Expression> replacements;

		protected SimpleParameterSubstituter(IReadOnlyDictionary<ParameterExpression, Expression> replacements) {
			this.replacements = replacements;
		}

		protected override Expression VisitParameter(ParameterExpression node) {
			Expression replacement;
			if (replacements.TryGetValue(node, out replacement)) {
				return replacement;
			} else {
				return node;
			}
		}
	}
}
