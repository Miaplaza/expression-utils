using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MiaPlaza.ExpressionUtils.Expanding.Attributes {
	/// <summary>
	/// Indicates that a <see cref="MemberExpression"/> using the member this attribute is attached to
	/// should be rewritten by the <see cref="ExpressionExpanderVisitor"/> according to the 
	/// <see cref="ExpressionExpander{EXP}"/> <see cref="CustomExpander"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public abstract class ExpressionExpandablePropertyAttribute : Attribute {
		public readonly ExpressionExpander<MemberExpression> CustomExpander;

		protected ExpressionExpandablePropertyAttribute(ExpressionExpander<MemberExpression> customExpander) {
			CustomExpander = customExpander;
		}
	}

	/// <summary>
	/// Indicates that a <see cref="MemberExpression"/> using the member this attribute is attached to
	/// should be rewritten by the <see cref="ExpressionExpanderVisitor"/> with a an 
	/// <see cref="ExpressionExpander{EXP}"/> of the provided type.
	/// </summary>
	public sealed class ExpanderTypeExpressionExpandablePropertyAttribute : ExpressionExpandablePropertyAttribute {
		public ExpanderTypeExpressionExpandablePropertyAttribute(Type expanderType)
			: base(CompiledActivator.ForBaseType<ExpressionExpander<MemberExpression>>.Create(expanderType)) { }
	}
}
