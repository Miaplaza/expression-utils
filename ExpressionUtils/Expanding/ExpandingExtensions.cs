using MiaPlaza.ExpressionUtils.Evaluating;
using MiaPlaza.ExpressionUtils.Expanding.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MiaPlaza.ExpressionUtils.Expanding {
	public static class ExpandingExtensions {
		public static IExpressionEvaluator Evaluator;

		/// <summary>
		/// Evaluates ('calls') a typed expression without arguments. 
		/// If used in another expression, the subexpression can be inlined 
		/// using the <see cref="SubExpressionExpander"/>. Therefore, cyclic 
		/// Evals must not be used.
		/// </summary>
		[ExpanderTypeExpressionExpandableMethod(typeof(SubExpressionExpander))]
		public static R Eval<R>(this Expression<Func<R>> expression) {
			return (R)Evaluator.EvaluateLambda(expression).Invoke();
		}

		/// <summary>
		/// Evaluates ('calls') a typed expression with the specified arguments. 
		/// If used in another expression, the subexpression can be inlined using
		/// the <see cref="SubExpressionExpander"/>. Therefore, cyclic Evals must
		/// not be used.
		/// </summary>
		[ExpanderTypeExpressionExpandableMethod(typeof(SubExpressionExpander))]
		public static R Eval<R, P1>(this Expression<Func<P1, R>> expression, P1 p1) {
			return (R)Evaluator.EvaluateLambda(expression).Invoke(p1);
		}

		/// <summary>
		/// Evaluates ('calls') a typed expression with the specified arguments. 
		/// If used in another expression, the subexpression can be inlined using
		/// the <see cref="SubExpressionExpander"/>. Therefore, cyclic Evals must
		/// not be used.
		/// </summary>
		[ExpanderTypeExpressionExpandableMethod(typeof(SubExpressionExpander))]
		public static R Eval<R, P1, P2>(this Expression<Func<P1, P2, R>> expression, P1 p1, P2 p2) {
			return (R)Evaluator.EvaluateLambda(expression).Invoke(p1, p2);
		}

		/// <summary>
		/// Evaluates ('calls') a typed expression with the specified arguments. 
		/// If used in another expression, the subexpression can be inlined using
		/// the <see cref="SubExpressionExpander"/>. Therefore, cyclic Evals must
		/// not be used.
		/// </summary>
		[ExpanderTypeExpressionExpandableMethod(typeof(SubExpressionExpander))]
		public static R Eval<R, P1, P2, P3>(this Expression<Func<P1, P2, P3, R>> expression, P1 p1, P2 p2, P3 p3) {
			return (R)Evaluator.EvaluateLambda(expression).Invoke(p1, p2, p3);
		}

		/// <summary>
		/// Evaluates ('calls') a typed expression with the specified arguments. 
		/// If used in another expression, the subexpression can be inlined using
		/// the <see cref="SubExpressionExpander"/>. Therefore, cyclic Evals must
		/// not be used.
		/// </summary>
		[ExpanderTypeExpressionExpandableMethod(typeof(SubExpressionExpander))]
		public static R Eval<R, P1, P2, P3, P4>(this Expression<Func<P1, P2, P3, P4, R>> expression, P1 p1, P2 p2, P3 p3, P4 p4) {
			return (R)Evaluator.EvaluateLambda(expression).Invoke(p1, p2, p3, p4);
		}

		/// <summary>
		/// Evaluates ('calls') a typed expression with the specified arguments. 
		/// If used in another expression, the subexpression can be inlined using
		/// the <see cref="SubExpressionExpander"/>. Therefore, cyclic Evals must
		/// not be used.
		/// </summary>
		[ExpanderTypeExpressionExpandableMethod(typeof(SubExpressionExpander))]
		public static R Eval<R, P1, P2, P3, P4, P5>(this Expression<Func<P1, P2, P3, P4, P5, R>> expression, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5) {
			return (R)Evaluator.EvaluateLambda(expression).Invoke(p1, p2, p3, p4, p5);
		}

		/// <summary>
		/// Evaluates ('calls') a typed expression with the specified arguments. 
		/// If used in another expression, the subexpression can be inlined using
		/// the <see cref="SubExpressionExpander"/>. Therefore, cyclic Evals must
		/// not be used.
		/// </summary>
		[ExpanderTypeExpressionExpandableMethod(typeof(SubExpressionExpander))]
		public static R Eval<R, P1, P2, P3, P4, P5, P6>(this Expression<Func<P1, P2, P3, P4, P5, P6, R>> expression, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6) {
			return (R)Evaluator.EvaluateLambda(expression).Invoke(p1, p2, p3, p4, p5, p6);
		}

		/// <summary>
		/// Rewrites a call to any of the 'Eval' methods above by inlining the 
		/// expression that would have been evaluated.
		/// </summary>
		class SubExpressionExpander : ExpressionExpander<MethodCallExpression> {
			public override Expression Expand(MethodCallExpression methodCallExpression) {
				// The first argument of any 'Eval' call is always the expression to be evaluated.
				// Its retrival must not throw exceptions (unless in an invalid subtree).
				var lambda = Evaluator.Evaluate(methodCallExpression.Arguments[0]) as LambdaExpression;

				var substituted = ParameterSubstituter.SubstituteParameter(
					lambda,
					methodCallExpression.Arguments.Skip(1));

				return substituted;
			}
		}
	}
}
