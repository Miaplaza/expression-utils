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
		/// <returns>A new tree with sub-trees evaluated and replaced.</returns>
		public static TExpression PartialEval<TExpression>(TExpression expression, IExpressionEvaluator evaluator) where TExpression : LambdaExpression {
			return (TExpression) new SubtreeEvaluator(evaluator, Nominator.Nominate(expression.Body, canBeEvaluated)).Eval(expression);
		}

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
		private class SubtreeEvaluator : ExpressionVisitor {
			private readonly HashSet<Expression> candidates;
			private readonly IExpressionEvaluator evaluator;

			internal SubtreeEvaluator(IExpressionEvaluator evaluator, HashSet<Expression> candidates) {
				this.candidates = candidates;
				this.evaluator = evaluator;
			}

			internal Expression Eval(LambdaExpression exp) {
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

					return evaluate(exp);
				}

				return base.Visit(exp);
			}

			private Expression evaluate(Expression exp) {
				try {
					/// It seems like the intention of the original author here might have been to use expression return type as the type of constant
					/// but exp.Type is not that in some cases, so this might be a bug. For example
					/// <see cref="LambdaExpression.Type"/> would be delegate type of the method, but what we
					/// would really want as a type of constant is <see cref="LambdaExpression.ReturnType"/>.
					/// So in case of <see cref="LambdaExpression"/> here this would throw exception, while it could have been evaluated.
					/// That said, it seems like this is not currently a problem, so just leaving comment here
					/// for anybody possibly wondering about this in future.
					return Expression.Constant(evaluator.Evaluate(exp), exp.Type);
				} catch (Exception exception) {
					return ExceptionClosure.MakeExceptionClosureCall(exception, exp.Type);
				}
			}
		}

		/// <summary>
		/// Performs bottom-up analysis to determine which nodes can possibly
		/// be part of an evaluated sub-tree.
		/// </summary>
		private class Nominator : ExpressionVisitor {
			private readonly Func<Expression, bool> fnCanBeEvaluated;
			private readonly HashSet<Expression> candidates = new HashSet<Expression>();
			private bool canBeEvaluated;

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
