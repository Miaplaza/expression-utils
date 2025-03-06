using MiaPlaza.ExpressionUtils;
using System;
using System.Linq.Expressions;
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
	}
}
