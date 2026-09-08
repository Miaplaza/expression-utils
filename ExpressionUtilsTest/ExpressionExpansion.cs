using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using MiaPlaza.ExpressionUtils;
using NUnit.Framework;
using System.Linq.Expressions;
using MiaPlaza.ExpressionUtils.Expanding;

namespace MiaPlaza.Test.ExpressionUtilsTest {
	/// <summary>
	/// Tests the expansion of expressions
	/// </summary>
	[TestFixture]
	internal class ExpressionExpansion {
		private static readonly Expression<Func<int, int>> squareExpression = i => i * i;

		[SetUp]
		public void SetEvaluator() {
			ExpandingExtensions.SetEvaluator(ExpressionUtils.Evaluating.ExpressionInterpreter.Instance);
		}

		[Test]
		public void SimpleEvalExpandTest() {
			Expression<Func<int, bool>> predicate = i => squareExpression.Eval(i) > 5;
			predicate = ExpressionExpanderVisitor.Expand(predicate, ExpressionUtils.Evaluating.ExpressionInterpreter.Instance);

			Expression<Func<int, bool>> expected = i => i * i > 5;

			Assert.That(predicate.StructuralIdentical(expected),
				$"actual: {predicate}, expected: {expected}");
		}

		[Test]
		public void RecursiveArgumentEvalExpandTest() {
			Expression<Func<int, int>> squareSquareExpression = i => squareExpression.Eval(squareExpression.Eval(i));

			squareSquareExpression = ExpressionExpanderVisitor.Expand(squareSquareExpression, ExpressionUtils.Evaluating.ExpressionInterpreter.Instance);

			Expression<Func<int, int>> expected = i => (i * i) * (i * i);

			Assert.That(squareSquareExpression.StructuralIdentical(expected),
				$"actual: {squareSquareExpression}, expected: {expected}");
		}

		[Test]
		public void RecursiveBodyEvalExpandTest() {
			Expression<Func<int, int>> squarePlusOneExpression = i => squareExpression.Eval(i) + 1;
			Expression<Func<int, bool>> predicate = i => squarePlusOneExpression.Eval(i) > 5;

			predicate = ExpressionExpanderVisitor.Expand(predicate, ExpressionUtils.Evaluating.ExpressionInterpreter.Instance);

			Expression<Func<int, bool>> expected = i => i * i + 1 > 5;

			Assert.That(predicate.StructuralIdentical(expected),
				$"actual: {predicate}, expected: {expected}");
		}

		[Test]
		public void NullEvalExpandTest() {
			Expression<Func<int>> valueExpression = null;
			Expression<Func<int, bool>> predicate = i => i == valueExpression.Eval();

			// Expanding itself does not throw an exception
			predicate = ExpressionExpanderVisitor.Expand(predicate, ExpressionUtils.Evaluating.ExpressionInterpreter.Instance);

			// But the exception is thrown when trying to execute it
			Assert.Throws<CustomExpanderException>(() => predicate.Compile()(42));
		}

		/// <summary>
		/// Expanding an 'Eval' call on a null expression reference must not be exception-driven:
		/// the failure is embedded as an ExceptionClosure without any exception being thrown
		/// (and caught) internally. See <see cref="NullEvalExpandTest"/> for the execution behavior.
		/// </summary>
		[Test]
		public void NullEvalExpandDoesNotThrowInternally() {
			Expression<Func<int>> valueExpression = null;
			Expression<Func<int, bool>> predicate = i => i == valueExpression.Eval();

			var exceptions = new ConcurrentQueue<Exception>();
			EventHandler<FirstChanceExceptionEventArgs> handler = (sender, args) => exceptions.Enqueue(args.Exception);
			AppDomain.CurrentDomain.FirstChanceException += handler;
			try {
				ExpressionExpanderVisitor.Expand(predicate, ExpressionUtils.Evaluating.ExpressionInterpreter.Instance);
			} finally {
				AppDomain.CurrentDomain.FirstChanceException -= handler;
			}

			Assert.IsEmpty(exceptions);
		}
	}
}

