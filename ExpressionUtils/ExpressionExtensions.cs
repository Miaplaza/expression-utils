using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Generic;
using MiaPlaza.ExpressionUtils.Evaluating;

namespace MiaPlaza.ExpressionUtils {
	public static class ExpressionExtensions {
		
		/// <summary>
		/// Removes outer cast- and typeAs-expressions from the expression to ease parsing.
		/// </summary>
		public static Expression UnwrapCasts(this Expression exp) {
			while (exp.NodeType == ExpressionType.TypeAs || exp.NodeType == ExpressionType.Convert) {
				exp = (exp as UnaryExpression).Operand;
			}
			return exp;
		}

		/// <summary>
		/// Return whether <paramref name="exp"/> is constant an its value is equal to <paramref name="value"/>.
		/// </summary>
		public static bool IsConstant(this Expression exp, object value) {
			if (!exp.IsConstant()) {
				return false;
			}

			var constant = ((ConstantExpression)exp.UnwrapCasts()).Value;
			return Equals(constant, value);
		}

		/// <summary>
		/// Return whether <paramref name="exp"/> could be a null value.
		/// I.e., returns false iff <paramref name="exp"/> is a non-null constant or representing a value of a type
		/// that can not be null.
		/// </summary>
		public static bool CouldBeNull(this Expression exp){
			if (exp.IsConstant()) {
				return !exp.IsConstant(null);
			}
			return !exp.Type.IsValueType || Nullable.GetUnderlyingType(exp.Type) != null;
		}

		/// <summary>
		/// Return whether <paramref name="exp"/> is a (possibly cast and converted) constant.
		/// </summary>
		public static bool IsConstant(this Expression exp) {
			exp = UnwrapCasts(exp);
			return exp is ConstantExpression;
		}
	}
}
