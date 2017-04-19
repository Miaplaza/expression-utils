using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiaPlaza.ExpressionUtils;
using NUnit.Framework;
using System.Linq.Expressions;
using MiaPlaza.ExpressionUtils.Expanding;
using MiaPlaza.ExpressionUtils.Expanding.Attributes;

namespace MiaPlaza.Test.ExpressionUtilsTest {
	/// <summary>
	/// Tests the expansion of expressions
	/// </summary>
	[TestFixture]
	class ExpressionExpansion {
		private static readonly Expression<Func<int, int>> squareExpression = i => i * i;
		
		[SetUp]
		public void SetEvaluator() {
			ExpandingExtensions.Evaluator = ExpressionUtils.Evaluating.ExpressionInterpreter.Instance;
		}

		[Test]
		public void SimpleEvalExpandTest() {
			Expression<Func<int, bool>> predicate = i => squareExpression.Eval(i) > 5;
			predicate = ExpressionExpanderVisitor.ExpandBody(predicate);

			Expression<Func<int, bool>> expected = i => i * i > 5;

			Assert.That(predicate.StructuralIdentical(expected), 
				$"actual: {predicate.ToString()}, expected: {expected.ToString()}");
		}

		[Test]
		public void RecursiveArgumentEvalExpandTest() {
			Expression<Func<int, int>> squareSquareExpression = i => squareExpression.Eval(squareExpression.Eval(i));

			squareSquareExpression = ExpressionExpanderVisitor.ExpandBody(squareSquareExpression);

			Expression<Func<int, int>> expected = i => (i * i) * (i * i);

			Assert.That(squareSquareExpression.StructuralIdentical(expected), 
				$"actual: {squareSquareExpression.ToString()}, expected: {expected.ToString()}");
		}

		[Test]
		public void RecursiveBodyEvalExpandTest() {
			Expression<Func<int, int>> squarePlusOneExpression = i => squareExpression.Eval(i) + 1;
			Expression<Func<int, bool>> predicate = i => squarePlusOneExpression.Eval(i) > 5;

			predicate = ExpressionExpanderVisitor.ExpandBody(predicate);

			Expression<Func<int, bool>> expected = i => i * i + 1 > 5;

			Assert.That(predicate.StructuralIdentical(expected),
				$"actual: {predicate.ToString()}, expected: {expected.ToString()}");
		}

		[Test]
		public void NullEvalExpandTest() {
			Expression<Func<int>> valueExpression = null;
			Expression<Func<int, bool>> predicate = i => i == valueExpression.Eval();

			// Expanding itself does not throw an exception
			predicate = ExpressionExpanderVisitor.ExpandBody(predicate);
			
			// But the exception is thrown when trying to execute it
			Assert.Throws<CustomExpanderException>(() => predicate.Compile()(42));
		}
	}
}
