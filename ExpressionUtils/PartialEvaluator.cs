using MiaPlaza.ExpressionUtils.Evaluating;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

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

			var memberAccess = (expression as MethodCallExpression)?.Method
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

			/// <summary>
			/// Short-circuiting boolean operations are simplified while they are being evaluated: the left operand
			/// is visited first and, if it turns out to decide the result, the right operand is never visited and
			/// therefore never evaluated. This matters because an evaluation candidate on the right might only be
			/// safe to evaluate under the guard on the left (e.g. <c>start == null || start.Value > x</c>).
			/// Simplifying after the fact would be too late; the exception would already have been thrown.
			/// </summary>
			protected override Expression VisitBinary(BinaryExpression node) {
				var isShortCircuitingBooleanOperation =
					(node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.OrElse)
					&& node.Method == null
					&& (node.Type == typeof(bool) || node.Type == typeof(bool?));

				if (!isShortCircuitingBooleanOperation) {
					return base.VisitBinary(node);
				}

				// The left-hand value that decides the result on its own: true for ||, false for &&.
				var shortCircuitValue = node.NodeType == ExpressionType.OrElse;

				var left = Visit(node.Left);
				if (left.IsConstant(shortCircuitValue)) {
					// true || X	→ true
					// false && X	→ false
					return left;
				}
				if (left.IsConstant(!shortCircuitValue)) {
					// false || X	→ X
					// true && X	→ X
					return Visit(node.Right);
				}

				var right = Visit(node.Right);
				if (right.IsConstant(shortCircuitValue)) {
					// X || true	→ true
					// X && false	→ false
					// X is dropped from the tree even though it was not evaluated, matching the long-standing
					// behavior of the Simplifier; X is a parameter-dependent predicate, not a side effect.
					return right;
				}
				if (right.IsConstant(!shortCircuitValue)) {
					// X || false	→ X
					// X && true	→ X
					return left;
				}

				return node.Update(left, node.Conversion, right);
			}

			/// <summary>
			/// Same idea as for <see cref="VisitBinary"/>: a test that evaluates to a constant decides which branch is
			/// live, so the dead branch is never visited and therefore never evaluated.
			/// </summary>
			protected override Expression VisitConditional(ConditionalExpression node) {
				var test = Visit(node.Test);
				if (test.IsConstant(true)) {
					return Visit(node.IfTrue);
				}
				if (test.IsConstant(false)) {
					return Visit(node.IfFalse);
				}
				return node.Update(test, Visit(node.IfTrue), Visit(node.IfFalse));
			}

			private Expression evaluate(Expression exp) {
				try {
					// ReSharper disable InvalidXmlDocComment
					/// It seems like the intention of the original author here might have been to use expression return type as the type of constant
					/// but exp.Type is not that in some cases, so this might be a bug. For example
					/// <see cref="LambdaExpression.Type"/> would be delegate type of the method, but what we
					/// would really want as a type of constant is <see cref="LambdaExpression.ReturnType"/>.
					/// So in case of <see cref="LambdaExpression"/> here this would throw exception, while it could have been evaluated.
					/// That said, it seems like this is not currently a problem, so just leaving comment here
					/// for anybody possibly wondering about this in future.
					// ReSharper restore InvalidXmlDocComment
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
					var saveCanBeEvaluated = this.canBeEvaluated;
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
