using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MiaPlaza.ExpressionUtils.Expanding {
	/// <summary>
	/// Visitor that traverses the expression tree and replaces all nodes
	/// where an expander is attached to the method (via an 
	/// <see cref="Attributes.ExpanderTypeExpressionExpandableMethodAttribute"/>) 
	/// or property (via an 
	/// <see cref="Attributes.ExpanderTypeExpressionExpandablePropertyAttribute"/>)
	/// according to that expander.
	/// </summary>
	public class ExpressionExpanderVisitor : ExpressionVisitor {
		public static Expression Expand(Expression expr) {
			return instance.Visit(expr);
		}

		public static LambdaExpression ExpandBody(LambdaExpression expr) {
			return Expression.Lambda(Expand(expr.Body), expr.Parameters);
		}

		public static Expression<D> ExpandBody<D>(Expression<D> expr) {
			return expr.Update(Expand(expr.Body), expr.Parameters);
		}
		
		private static readonly ExpressionExpanderVisitor instance = new ExpressionExpanderVisitor();
		private ExpressionExpanderVisitor() { }

		protected override Expression VisitMethodCall(MethodCallExpression node) {
			var attr = node.Method.GetCustomAttribute<Attributes.ExpressionExpandableMethodAttribute>(inherit: false);
			if (attr == null) {
				return base.VisitMethodCall(node);
			}

			// Visit subtrees first
			node = Expression.Call(
				instance: Visit(node.Object),
				method: node.Method,
				arguments: node.Arguments.Select(Visit));
			Expression customExpanded;
			try {
				// Expand node itself
				customExpanded = attr.CustomExpander.Expand(node);
			} catch (Exception e) {
				return ExceptionClosure.MakeExceptionClosureCall(
					CustomExpanderException.Create(
						expander: attr.CustomExpander,
						expression: node,
						exception: e),
					node.Type);
			}

			// Visit result
			return Visit(customExpanded);
		}

		protected override Expression VisitMember(MemberExpression node) {
			if (!(node.Member is PropertyInfo)) {
				return base.VisitMember(node);
			}
			
			var attr = ((PropertyInfo)node.Member).GetCustomAttribute<Attributes.ExpressionExpandablePropertyAttribute>(inherit: false);
			if (attr == null) {
				return base.VisitMember(node);
			}

			// Visit subtrees first
			node = Expression.MakeMemberAccess(
				expression: Visit(node.Expression),
				member: node.Member);
			Expression customExpanded;
			try {
				// Expand node itself
				customExpanded = attr.CustomExpander.Expand(node);
			} catch (Exception e) {
				return ExceptionClosure.MakeExceptionClosureCall(
					CustomExpanderException.Create(
						expander: attr.CustomExpander,
						expression: node,
						exception: e),
					node.Type);
			}
			//Visit result
			return Visit(customExpanded);
		}
	}
}
