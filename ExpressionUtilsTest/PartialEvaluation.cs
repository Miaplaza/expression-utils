using MiaPlaza.ExpressionUtils;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using MiaPlaza.ExpressionUtils.Evaluating;
using MiaPlaza.ExpressionUtils.Expanding;

namespace MiaPlaza.Test.ExpressionUtilsTest {
	[TestFixture]
	public class PartialEvaluation {
		private static readonly IExpressionEvaluator[] evaluators = {
			ExpressionInterpreter.Instance,
			CachedExpressionCompiler.Instance,
		};

		[SetUp]
		public void SetEvaluator() {
			ExpandingExtensions.SetEvaluator(ExpressionInterpreter.Instance);
		}

		[Test]
		public void ConstantEvaluation([ValueSource(nameof(evaluators))] IExpressionEvaluator evaluator) {
			Expression<Func<bool>> expr = () => true;

			expr = PartialEvaluator.PartialEval(expr, evaluator);

			Assert.IsInstanceOf<ConstantExpression>(expr.Body);
			Assert.AreEqual(expected: true, actual: (expr.Body as ConstantExpression).Value);
		}

		[Test]
		public void SimpleEvaluation([ValueSource(nameof(evaluators))] IExpressionEvaluator evaluator) {
			Expression<Func<bool>> expr = () => 42 > 13;

			expr = PartialEvaluator.PartialEval(expr, evaluator);

			Assert.IsInstanceOf<ConstantExpression>(expr.Body);
			Assert.AreEqual(expected: true, actual: (expr.Body as ConstantExpression).Value);
		}

		[Test]
		public void VariableEvaluation([ValueSource(nameof(evaluators))] IExpressionEvaluator evaluator) {
			int x = 23;

			Expression<Func<bool>> expr = () => 42 > x;

			var eval_expr = PartialEvaluator.PartialEval(expr, evaluator);

			Assert.IsInstanceOf<ConstantExpression>(eval_expr.Body);
			Assert.AreEqual(expected: true, actual: (eval_expr.Body as ConstantExpression).Value);

			x = 50;
			eval_expr = PartialEvaluator.PartialEval(expr, evaluator);

			Assert.IsInstanceOf<ConstantExpression>(eval_expr.Body);
			Assert.AreEqual(expected: false, actual: (eval_expr.Body as ConstantExpression).Value);
		}

		private bool method(int a, int b) {
			return a % b == 1;
		}

		[Test]
		public void MethodEvaluation([ValueSource(nameof(evaluators))] IExpressionEvaluator evaluator) {
			Expression<Func<bool>> expr = () => method(31, 5);

			expr = PartialEvaluator.PartialEval(expr, evaluator);

			Assert.IsInstanceOf<ConstantExpression>(expr.Body);
			Assert.AreEqual(expected: true, actual: (expr.Body as ConstantExpression).Value);
		}

		[NoPartialEvaluation]
		private bool nonEvaluateableMethod(int a, int b) {
			Assert.Fail("Method may not be partially evaluated!");

			return a % b == 1;
		}

		[Test]
		public void NonEvaluateableMethodEvaluation([ValueSource(nameof(evaluators))] IExpressionEvaluator evaluator) {
			Expression<Func<bool>> expr = () => nonEvaluateableMethod(31, 5);

			expr = PartialEvaluator.PartialEval(expr, evaluator);

			Assert.IsInstanceOf<MethodCallExpression>(expr.Body);
			Assert.AreEqual(expected: 2, actual: (expr.Body as MethodCallExpression).Arguments.Count);
			Assert.AreEqual(expected: 31, actual: ((expr.Body as MethodCallExpression).Arguments[0] as ConstantExpression).Value);
			Assert.AreEqual(expected: 5, actual: ((expr.Body as MethodCallExpression).Arguments[1] as ConstantExpression).Value);
		}

		[Test]
		public void NonEvaluateableMethodEvaluationWithEvaluateableSubtrees([ValueSource(nameof(evaluators))] IExpressionEvaluator evaluator) {
			int x = 3;

			Expression<Func<bool>> expr = () => nonEvaluateableMethod(31, 2 + x);

			Assert.IsInstanceOf<MethodCallExpression>(expr.Body);
			Assert.AreEqual(expected: 2, actual: (expr.Body as MethodCallExpression).Arguments.Count);
			Assert.AreEqual(expected: 31, actual: ((expr.Body as MethodCallExpression).Arguments[0] as ConstantExpression).Value);
			Assert.IsInstanceOf<BinaryExpression>((expr.Body as MethodCallExpression).Arguments[1]);
			Assert.AreEqual(expected: 2, actual: (((expr.Body as MethodCallExpression).Arguments[1] as BinaryExpression).Left as ConstantExpression).Value);

			expr = PartialEvaluator.PartialEval(expr, evaluator);

			Assert.IsInstanceOf<MethodCallExpression>(expr.Body);
			Assert.AreEqual(expected: 2, actual: (expr.Body as MethodCallExpression).Arguments.Count);
			Assert.AreEqual(expected: 31, actual: ((expr.Body as MethodCallExpression).Arguments[0] as ConstantExpression).Value);
			Assert.AreEqual(expected: 5, actual: ((expr.Body as MethodCallExpression).Arguments[1] as ConstantExpression).Value);
		}

		/// <summary>
		/// After expansion, `start` is substituted with Constant(null, DateTime?), so the subtree
		/// `start.Value` is a parameter-free evaluation candidate. Evaluating it used to throw a
		/// TargetException (caught internally and embedded as an ExceptionClosure). Since the null-guard
		/// `start == null` short-circuits it away, partial evaluation must not evaluate it at all —
		/// no exception may be thrown anymore.
		/// </summary>
		[Test]
		public void PartialEvalAfterExpansionDoesNotThrow([ValueSource(nameof(evaluators))] IExpressionEvaluator evaluator) {
			Expression<Func<Entity, int, DateTime?, bool>> expression =
				(entity, id, start) =>
					entity.Id == id
					&& (start == null || entity.Date >= start.Value);

			Expression<Func<Entity, int, bool>> predicate =
				(entity, id) => expression.Eval(entity, id, null);

			predicate = ExpressionExpanderVisitor.Expand(predicate, evaluator);

			var exceptions = new ConcurrentQueue<Exception>();
			EventHandler<FirstChanceExceptionEventArgs> handler = (sender, args) => exceptions.Enqueue(args.Exception);
			AppDomain.CurrentDomain.FirstChanceException += handler;
			try {
				predicate = PartialEvaluator.PartialEval(predicate, evaluator);
			} finally {
				AppDomain.CurrentDomain.FirstChanceException -= handler;
			}

			Assert.IsEmpty(exceptions);

			var entity = new Entity {
				Id = 1,
				Date = DateTime.Now
			};
			var compiled = predicate.Compile();
			Assert.IsTrue(compiled(entity, 1));
			Assert.IsFalse(compiled(entity, 0));
		}

		/// <summary>
		/// Minimal shape of the case above (as it looks after expansion): accessing .Value on a null
		/// nullable constant, guarded by a short-circuiting null check. The guard folds to a constant,
		/// so the guarded subtree is never evaluated and the whole disjunction collapses.
		/// </summary>
		[Test]
		public void PartialEvalShortCircuitsNullGuardedNullableAccess([ValueSource(nameof(evaluators))] IExpressionEvaluator evaluator) {
			DateTime? start = null;

			Expression<Func<Entity, bool>> expr = e => start == null || e.Date >= start.Value;

			var result = PartialEvaluator.PartialEval(expr, evaluator);

			Assert.IsInstanceOf<ConstantExpression>(result.Body);
			Assert.AreEqual(expected: true, actual: ((ConstantExpression)result.Body).Value);
		}

		[Test]
		public void PartialEvalShortCircuitsConjunctionWithFalseGuard([ValueSource(nameof(evaluators))] IExpressionEvaluator evaluator) {
			DateTime? start = null;

			Expression<Func<Entity, bool>> expr = e => start != null && e.Date >= start.Value;

			var result = PartialEvaluator.PartialEval(expr, evaluator);

			Assert.IsInstanceOf<ConstantExpression>(result.Body);
			Assert.AreEqual(expected: false, actual: ((ConstantExpression)result.Body).Value);
		}

		[Test]
		public void PartialEvalStillEvaluatesLiveBranchOfShortCircuit([ValueSource(nameof(evaluators))] IExpressionEvaluator evaluator) {
			DateTime? start = new DateTime(2026, 9, 7);

			Expression<Func<Entity, bool>> expr = e => start == null || e.Date >= start.Value;

			var result = PartialEvaluator.PartialEval(expr, evaluator);

			// `start == null` folds to false, so the result is the partially evaluated right side.
			Assert.IsInstanceOf<BinaryExpression>(result.Body);
			var comparison = (BinaryExpression)result.Body;
			Assert.AreEqual(expected: ExpressionType.GreaterThanOrEqual, actual: comparison.NodeType);
			Assert.AreEqual(expected: start.Value, actual: ((ConstantExpression)comparison.Right).Value);
		}

		private class Entity {
			public int Id { get; set; }
			public DateTime Date { get; set; }
		}
	}
}
