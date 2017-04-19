using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MiaPlaza.ExpressionUtils.Expanding.Attributes {
	/// <summary>
	/// Indicates that a <see cref="MethodCallExpression"/> using the method this attribute is attached to
	/// should be rewritten by the <see cref="ExpressionExpanderVisitor"/> according to the 
	/// <see cref="ExpressionExpander{EXP}"/> <see cref="CustomExpander"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public abstract class ExpressionExpandableMethodAttribute : Attribute {
		public readonly ExpressionExpander<MethodCallExpression> CustomExpander;

		protected ExpressionExpandableMethodAttribute(ExpressionExpander<MethodCallExpression> customExpander) {
			CustomExpander = customExpander;
		}
	}

	/// <summary>
	/// Indicates that a <see cref="MethodCallExpression"/> using the method this attribute is attached to
	/// should be rewritten by the <see cref="ExpressionExpanderVisitor"/> with a an 
	/// <see cref="ExpressionExpander{EXP}"/> of the provided type.
	/// </summary>
	public sealed class ExpanderTypeExpressionExpandableMethodAttribute : ExpressionExpandableMethodAttribute {
		public ExpanderTypeExpressionExpandableMethodAttribute(Type expanderType)
			: base(CompiledActivator.ForBaseType<ExpressionExpander<MethodCallExpression>>.Create(expanderType)) { }
	}
}
