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
		class ThrowingEvaluator : IExpressionEvaluator {
			/// <remarks>
			/// This method is not needed for <see cref="Eval{R}(Expression{Func{R}})"/>.
			/// </remarks>
			public object Evaluate(Expression unparametrizedExpression) => throw new NotImplementedException();
			public VariadicArrayParametersDelegate EvaluateLambda(LambdaExpression lambdaExpression) 
				=> throw new InvalidOperationException("No evaluator set for handling 'Eval' calls!");
			public DELEGATE EvaluateTypedLambda<DELEGATE>(Expression<DELEGATE> expression) where DELEGATE : class => throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the evaluator used to handle <see cref="Eval{R}(Expression{Func{R}})"/>-calls. Use only if
		/// you plan on using Eval outside of expression expansion as a "shortcut" for Evaluating+Executing.
		/// </summary>
		/// <remarks>
		/// <see cref="Eval{R}(Expression{Func{R}})"/> is primarily meant to be used for expression composition.
		/// Therefore, it should be used inside other expressions and treated like 'method calls' there (with
		/// the limitation not to use circular calls). Finally, in order to "inline" all the Eval-calls and 
		/// 'flatten' the expression tree, one should use the <see cref="ExpressionExpanderVisitor"/>.
		/// </remarks>
		public static void SetEvaluator(IExpressionEvaluator evaluator) 
			=> ExpandingExtensions.evaluator = evaluator;
		private static IExpressionEvaluator evaluator = new ThrowingEvaluator();

		/// <summary>
		/// Evaluates ('calls') a typed expression without arguments. 
		/// If used in another expression, the subexpression can be inlined 
		/// using the <see cref="SubExpressionExpander"/>. Therefore, cyclic 
		/// Evals must not be used.
		/// </summary>
		[ExpanderTypeExpressionExpandableMethod(typeof(SubExpressionExpander))]
		public static R Eval<R>(this Expression<Func<R>> expression) 
			=> (R)evaluator.EvaluateLambda(expression).Invoke();

		/// <summary>
		/// Evaluates ('calls') a typed expression with the specified arguments. 
		/// If used in another expression, the subexpression can be inlined using
		/// the <see cref="SubExpressionExpander"/>. Therefore, cyclic Evals must
		/// not be used.
		/// </summary>
		[ExpanderTypeExpressionExpandableMethod(typeof(SubExpressionExpander))]
		public static R Eval<R, P1>(this Expression<Func<P1, R>> expression, P1 p1) 
			=> (R)evaluator.EvaluateLambda(expression).Invoke(p1);

		/// <summary>
		/// Evaluates ('calls') a typed expression with the specified arguments. 
		/// If used in another expression, the subexpression can be inlined using
		/// the <see cref="SubExpressionExpander"/>. Therefore, cyclic Evals must
		/// not be used.
		/// </summary>
		[ExpanderTypeExpressionExpandableMethod(typeof(SubExpressionExpander))]
		public static R Eval<R, P1, P2>(this Expression<Func<P1, P2, R>> expression, P1 p1, P2 p2) 
			=> (R)evaluator.EvaluateLambda(expression).Invoke(p1, p2);

		/// <summary>
		/// Evaluates ('calls') a typed expression with the specified arguments. 
		/// If used in another expression, the subexpression can be inlined using
		/// the <see cref="SubExpressionExpander"/>. Therefore, cyclic Evals must
		/// not be used.
		/// </summary>
		[ExpanderTypeExpressionExpandableMethod(typeof(SubExpressionExpander))]
		public static R Eval<R, P1, P2, P3>(this Expression<Func<P1, P2, P3, R>> expression, P1 p1, P2 p2, P3 p3) 
			=> (R)evaluator.EvaluateLambda(expression).Invoke(p1, p2, p3);

		/// <summary>
		/// Evaluates ('calls') a typed expression with the specified arguments. 
		/// If used in another expression, the subexpression can be inlined using
		/// the <see cref="SubExpressionExpander"/>. Therefore, cyclic Evals must
		/// not be used.
		/// </summary>
		[ExpanderTypeExpressionExpandableMethod(typeof(SubExpressionExpander))]
		public static R Eval<R, P1, P2, P3, P4>(this Expression<Func<P1, P2, P3, P4, R>> expression, P1 p1, P2 p2, P3 p3, P4 p4) 
			=> (R)evaluator.EvaluateLambda(expression).Invoke(p1, p2, p3, p4);

		/// <summary>
		/// Evaluates ('calls') a typed expression with the specified arguments. 
		/// If used in another expression, the subexpression can be inlined using
		/// the <see cref="SubExpressionExpander"/>. Therefore, cyclic Evals must
		/// not be used.
		/// </summary>
		[ExpanderTypeExpressionExpandableMethod(typeof(SubExpressionExpander))]
		public static R Eval<R, P1, P2, P3, P4, P5>(this Expression<Func<P1, P2, P3, P4, P5, R>> expression, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5) 
			=> (R)evaluator.EvaluateLambda(expression).Invoke(p1, p2, p3, p4, p5);

		/// <summary>
		/// Evaluates ('calls') a typed expression with the specified arguments. 
		/// If used in another expression, the subexpression can be inlined using
		/// the <see cref="SubExpressionExpander"/>. Therefore, cyclic Evals must
		/// not be used.
		/// </summary>
		[ExpanderTypeExpressionExpandableMethod(typeof(SubExpressionExpander))]
		public static R Eval<R, P1, P2, P3, P4, P5, P6>(this Expression<Func<P1, P2, P3, P4, P5, P6, R>> expression, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6) 
			=> (R)evaluator.EvaluateLambda(expression).Invoke(p1, p2, p3, p4, p5, p6);

		/// <summary>
		/// Rewrites a call to any of the 'Eval' methods above by inlining the 
		/// expression that would have been evaluated.
		/// </summary>
		class SubExpressionExpander : ExpressionExpander<MethodCallExpression> {
			public override Expression Expand(MethodCallExpression methodCallExpression, IExpressionEvaluator evaluator) {
				// The first argument of any 'Eval' call is always the expression to be evaluated.
				// Its retrival must not throw exceptions (unless in an invalid subtree).
				var lambda = evaluator.Evaluate(methodCallExpression.Arguments[0]) as LambdaExpression;

				var substituted = ParameterSubstituter.SubstituteParameter(
					lambda,
					methodCallExpression.Arguments.Skip(1));

				return substituted;
			}
		}
	}
}
