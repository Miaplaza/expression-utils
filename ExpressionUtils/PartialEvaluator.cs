using MiaPlaza.ExpressionUtils.Evaluating;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace MiaPlaza.ExpressionUtils {
	public static class PartialEvaluator {
		/// <summary>
		/// Performs evaluation & replacement of independent sub-trees
		/// </summary>
		/// <param name="expression">The root of the expression tree.</param>
		/// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
		/// <returns>A new tree with sub-trees evaluated and replaced.</returns>
		public static Expression PartialEval(Expression expression, IExpressionEvaluator evaluator, Func<Expression, bool> fnCanBeEvaluated) 
			=> new SubtreeEvaluator(evaluator, Nominator.Nominate(expression, fnCanBeEvaluated)).Eval(expression);

		/// <summary>
		/// Performs evaluation & replacement of independent sub-trees
		/// </summary>
		/// <param name="expression">The root of the expression tree.</param>
		/// <returns>A new tree with sub-trees evaluated and replaced.</returns>
		public static Expression PartialEval(Expression expression, IExpressionEvaluator evaluator) 
			=> PartialEval(expression, evaluator, canBeEvaluated);

		/// <summary>
		/// Performs evaluation & replacement of independent sub-trees in the body
		/// of an typed <see cref="LambdaExpression"/>.
		/// </summary>
		/// <param name="expFunc">The lambda expression whichs body to
		/// partially evaluate.</param>
		/// <returns>A new typed <see cref="LambdaExpression"/> with sub-trees in the 
		/// body evaluated and replaced.</returns>
		public static Expression<D> PartialEvalBody<D>(Expression<D> expFunc, IExpressionEvaluator evaluator) 
			=> expFunc.Update(PartialEval(expFunc.Body, evaluator), expFunc.Parameters);

		/// <summary>
		/// Performs evaluation & replacement of independent sub-trees in the body
		/// of an <see cref="LambdaExpression"/>.
		/// </summary>
		/// <param name="expFunc">The lambda expression whichs body to
		/// partially evaluate.</param>
		/// <returns>A new <see cref="LambdaExpression"/> with sub-trees in the body 
		/// evaluated and replaced.</returns>
		public static LambdaExpression PartialEvalBody(LambdaExpression expFunc, IExpressionEvaluator evaluator) 
			=> Expression.Lambda(PartialEval(expFunc.Body, evaluator), expFunc.Parameters);

		private static bool canBeEvaluated(Expression expression) {
			if (expression.NodeType == ExpressionType.Parameter) {
				return false;
			}

			MemberInfo memberAccess = (expression as MethodCallExpression)?.Method 
				?? (expression as NewExpression)?.Constructor
				?? (expression as MemberExpression)?.Member;

			return memberAccess == null || !memberAccess.IsDefined(typeof(NoPartialEvaluationAttribute), inherit: false);
		}

		/// <summary>
		/// Evaluates & replaces sub-trees when first candidate is reached (top-down)
		/// </summary>
		class SubtreeEvaluator : ExpressionVisitor {
			readonly HashSet<Expression> candidates;
			readonly IExpressionEvaluator evaluator;

			internal SubtreeEvaluator(IExpressionEvaluator evaluator, HashSet<Expression> candidates) {
				this.candidates = candidates;
				this.evaluator = evaluator;
			}

			internal Expression Eval(Expression exp) {
				return this.Visit(exp);
			}

			public override Expression Visit(Expression exp) {
				if (exp == null) {
					return null;
				}
				if (candidates.Contains(exp)) {
					if (exp is ConstantExpression) {
						return exp;
					}

					try {
						return Expression.Constant(evaluator.Evaluate(exp), exp.Type);
					} catch (Exception exception) {
						return ExceptionClosure.MakeExceptionClosureCall(exception, exp.Type);
					}
				}
				return base.Visit(exp);
			}
		}

		/// <summary>
		/// Performs bottom-up analysis to determine which nodes can possibly
		/// be part of an evaluated sub-tree.
		/// </summary>
		class Nominator : ExpressionVisitor {
			private readonly Func<Expression, bool> fnCanBeEvaluated;
			private readonly HashSet<Expression> candidates = new HashSet<Expression>();
			bool canBeEvaluated;

			private Nominator(Func<Expression, bool> fnCanBeEvaluated) {
				this.fnCanBeEvaluated = fnCanBeEvaluated;
			}

			public static HashSet<Expression> Nominate(Expression expression, Func<Expression, bool> fnCanBeEvaluated) {
				var visitor = new Nominator(fnCanBeEvaluated);
				visitor.Visit(expression);
				return visitor.candidates;
			}

			public override Expression Visit(Expression expression) {
				if (expression != null) {
					bool saveCanBeEvaluated = this.canBeEvaluated;
					this.canBeEvaluated = true;
					base.Visit(expression);
					if (this.canBeEvaluated) {
						if (this.fnCanBeEvaluated(expression)) {
							this.candidates.Add(expression);
						} else {
							this.canBeEvaluated = false;
						}
					}
					this.canBeEvaluated &= saveCanBeEvaluated;
				}
				return expression;
			}
		}
	}
}
