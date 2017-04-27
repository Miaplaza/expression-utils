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
	/// The replacement performed by this visitor does not only fill in the expressions, but it also
	/// resolves a more specific override if possible. For example, suppose that <c>t</c> is a parameter
	/// of type <c>T</c> and <c>x</c> is an expression of type <c>X:T</c>. Replacing <c>t</c> with <c>x</c>
	/// in <c>t.foo()</c> also replaces the <see cref="MethodCallExpression"/> associated with <c>.foo()</c>
	/// by the more specific override that is provided by <c>X</c> (<see cref="VisitMethodCall"/> and
	/// <see cref="VisitMember"/>.) This is important for later inspection of the expression tree, e.g., to
	/// read attributes on <c>foo</c> to find out how <c>.foo()</c> can be rendered for the database.
	/// To that end, the visitor also removes unecessary casts to find the most specific override
	/// possible (<see cref="VisitUnary"/>.)
	/// </remarks>
	public class ParameterSubstituter : ExpressionVisitor {
		public static Expression SubstituteParameter(LambdaExpression expression, params Expression[] replacements) {
			return SubstituteParameter(expression, replacements == null ? null : new ReadOnlyCollection<Expression>(replacements));
		}

		public static Expression SubstituteParameter(LambdaExpression expression, IEnumerable<Expression> replacements) {
			return SubstituteParameter(expression, replacements.ToList());
		}

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
				if (!tuple.parameter.Type.GetTypeInfo().IsAssignableFrom(tuple.replacement.Type.GetTypeInfo())) {
					throw new ArgumentException($"The expression {tuple.replacement} cannot be used as replacement for the parameter {tuple.parameter}.");
				}
				dict[tuple.parameter] = tuple.replacement;
			}

			return new ParameterSubstituter(dict).Visit(expression.Body);
		}

		readonly IReadOnlyDictionary<ParameterExpression, Expression> replacements;

		ParameterSubstituter(IReadOnlyDictionary<ParameterExpression, Expression> replacements) {
			this.replacements = replacements;
		}

		protected override Expression VisitParameter(ParameterExpression node) {
			return replacements.ContainsKey(node) ? replacements[node] : node;
		}

		protected override Expression VisitMember(MemberExpression node) {
			var baseCallResult = (MemberExpression)base.VisitMember(node);

			if (!(baseCallResult.Member is PropertyInfo) // fields
				|| node.Expression == null // static properties
				|| baseCallResult.Expression.Type == node.Expression.Type) {
				return baseCallResult;
			}

			return Expression.MakeMemberAccess(
				baseCallResult.Expression,
				getImplementationToCallOn(
					baseCallResult.Expression.Type,
					(PropertyInfo)baseCallResult.Member));
		}

		protected override Expression VisitMethodCall(MethodCallExpression node) {
			var baseCallResult = (MethodCallExpression)base.VisitMethodCall(node);

			if (node.Object == null // static methods
				|| baseCallResult.Object.Type == node.Object.Type) {
				return baseCallResult;
			}

			return Expression.Call(
				baseCallResult.Object,
				getImplementationToCallOn(
					baseCallResult.Object.Type,
					baseCallResult.Method),
				baseCallResult.Arguments);
		}

		protected override Expression VisitUnary(UnaryExpression node) {
			if (node.NodeType == ExpressionType.Convert
				// node.Method is non-null if there is a explicit/implicit cast implemented
				&& node.Method == null) {
				var operand = this.Visit(node.Operand);
				if (node.Type.GetTypeInfo().IsAssignableFrom(operand.Type.GetTypeInfo())
					// a cast is lifted if it casts to a Nullable<>
					&& !node.IsLifted) {
					return operand;
				} else {
					return Expression.Convert(operand, node.Type);
				}
			}
			return base.VisitUnary(node);
		}

		private static MethodInfo getImplementationToCallOn(Type t, MethodInfo method) {
			if (method.DeclaringType.GetTypeInfo().IsInterface) {
				return method.GetImplementationInfo(t);
			} else {
				return t.GetTypeInfo().GetDeclaredMethod(method.Name);
			}
		}

		private static PropertyInfo getImplementationToCallOn(Type t, PropertyInfo property) {
			if (property.DeclaringType.GetTypeInfo().IsInterface) {
				return property.GetImplementationInfo(t);
			} else {
				return t.GetTypeInfo().GetDeclaredProperty(property.Name);
			}
		}
	}
}
