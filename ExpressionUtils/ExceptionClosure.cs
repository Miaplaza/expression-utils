using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MiaPlaza.ExpressionUtils {
	/// <summary>
	/// A little trick that allows us to embed exceptions in 
	/// expression trees. ThrowExpressions have Type 
	/// <see cref="void"/> and therefore cannot directly be
	/// included as replacement of another non-
	/// <see cref="void"/> subtree. The <see cref="Invoke{T}"/>
	/// function is generic and a CallExpression using it
	/// can therefore resemble any type.
	/// </summary>
	public struct ExceptionClosure {
		private static readonly MethodInfo genericExceptionClosureInvokeMethod = typeof(ExceptionClosure).GetMethod("Invoke");

		public readonly Exception Exception;

		public static Expression MakeExceptionClosureCall(Exception ex, Type expressionType) => 
			Expression.Call(
				instance: Expression.Constant(new ExceptionClosure(ex)),
				method: genericExceptionClosureInvokeMethod.MakeGenericMethod(expressionType));

		private ExceptionClosure(Exception ex) {
			Exception = ex;
		}

		public T Invoke<T>() {
			throw Exception;
		}

		public override string ToString() => Exception.ToString();
	}
}
