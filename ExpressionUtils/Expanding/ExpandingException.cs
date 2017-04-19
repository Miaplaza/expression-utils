using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MiaPlaza.ExpressionUtils.Expanding {
	/// <summary>
	/// An exception that happend during expanding an expression
	/// </summary>
	public abstract class ExpandingException : InvalidOperationException {
		public readonly Expression Expression;

		internal ExpandingException(string message, Expression exp, Exception inner) 
			: base(message, inner) {
			Expression = exp;
		}
	}

	/// <summary>
	/// An exception that was thrown from a custom expander. Usually wrapped in a 
	/// <see cref="ExceptionClosure"/> and left in the result AST
	/// </summary>
	public class CustomExpanderException : ExpandingException {
		public readonly object CustomExpander;

		private CustomExpanderException(object expander, Expression exp, Exception inner) 
			: base("An exception happend during custom expanding!", exp, inner) {
			CustomExpander = expander;
		}

		/// <summary>
		/// Creates a new <see cref="CustomExpanderException"/>. Since constructors must not take 
		/// generic parameters, this is the only way to pass a <see cref="ExpressionExpander{EXP}"/>.
		/// </summary>
		internal static CustomExpanderException Create<EXP>(ExpressionExpander<EXP> expander, EXP expression, Exception exception) where EXP : Expression {
			return new CustomExpanderException(expander, expression, exception);
		}
	}
}
