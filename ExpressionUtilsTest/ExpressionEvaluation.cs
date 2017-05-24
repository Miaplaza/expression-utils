using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiaPlaza.ExpressionUtils;
using NUnit.Framework;
using System.Linq.Expressions;
using MiaPlaza.ExpressionUtils.Evaluating;

namespace MiaPlaza.Test.ExpressionUtilsTest {
	/// <summary>
	/// Tests of correctness for different ways of expression evaluation
	/// </summary>
	[TestFixtureSource(nameof(Evaluators))]
	class ExpressionEvaluation {
		public static IEnumerable<TestFixtureData> Evaluators = new[] {
			// in here for reference
			new TestFixtureData(ExpressionCompiler.Instance),
			new TestFixtureData(CachedExpressionCompiler.Instance),
			new TestFixtureData(ExpressionInterpreter.Instance),
		};

		private readonly IExpressionEvaluator evaluator;

		public ExpressionEvaluation(IExpressionEvaluator evaluator) {
			this.evaluator = evaluator;
		}

		[Test]
		public void TestConstantExpression() {
			Expression<Func<bool>> expr = () => true;

			Assert.AreEqual(expected: true, actual: evaluator.Evaluate(expr.Body));
		}

		[Test]
		public void TestClosureExpression() {
			int variable = 42;
			Expression<Func<int>> expr = () => variable;

			Assert.AreEqual(expected: 42, actual: evaluator.Evaluate(expr.Body));
		}

		[Test]
		public void TestAdditionExpression() {
			int variable = 42;
			Expression<Func<int>> expr = () => variable + 1000;

			Assert.AreEqual(expected: 1042, actual: evaluator.Evaluate(expr.Body));
		}

		bool throwsException() {
			throw new Exception();
		}

		[Test]
		public void TestShortCircuitExpression() {
			int variable = 42;
			Expression<Func<bool>> expr = () => variable > 1 || throwsException();

			Assert.AreEqual(expected: true, actual: evaluator.Evaluate(expr.Body));

			expr = () => variable < 1 && throwsException();

			Assert.AreEqual(expected: false, actual: evaluator.Evaluate(expr.Body));
		}

		enum MyEnum {
			First,
			Second,
			Third
		}

		[Test]
		public void TestEnumToIntConvertExpression() {
			var value = MyEnum.Second;
			Expression<Func<int>> expression = () => (int)value;

			Assert.AreEqual(expected: 1, actual: evaluator.Evaluate(expression.Body));
		}

		[Test]
		public void TestEnumToNullableIntConvertExpression() {
			var value = MyEnum.Second;
			Expression<Func<int?>> expression = () => (int?)value;
			
			Assert.AreEqual(expected: 1, actual: evaluator.Evaluate(expression.Body));
		}

		[Test]
		public void TestNullableEnumToNullableIntConvertExpression() {
			MyEnum? value = MyEnum.Second;
			Expression<Func<int?>> expression = () => (int?)value;

			Assert.AreEqual(expected: 1, actual: evaluator.Evaluate(expression.Body));

			value = null;
			Assert.AreEqual(expected: null, actual: evaluator.Evaluate(expression.Body));
		}

		[Test]
		public void TestNullableEnumToIntConvertExpression() {
			MyEnum? value = MyEnum.Second;
			Expression<Func<int?>> expression = () => (int)value;

			Assert.AreEqual(expected: 1, actual: evaluator.Evaluate(expression.Body));

			value = null;
			try {
				evaluator.Evaluate(expression.Body);
				Assert.Fail();
			} catch { }
		}

		public static readonly IEnumerable<int> TestOffsets = new[] {
			1,
			100,
			128,
			int.MaxValue / 2,
			int.MaxValue / 2 + 42
		};

		[Test]
		public void TestNumericConvertExpression() {
			Expression<Func<int, byte>> expression;
			byte expectedOver, expectedUnder;

			foreach (int offset in TestOffsets) {
				unchecked {
					expression = v => (byte)v;
					expectedOver = (byte)(byte.MaxValue + offset);
					expectedUnder = (byte)(byte.MinValue - offset);
				}

				Assert.AreEqual(expected: byte.MaxValue, actual: evaluator.EvaluateTypedLambda(expression)(byte.MaxValue));
				Assert.AreEqual(expected: expectedOver, actual: evaluator.EvaluateTypedLambda(expression)(byte.MaxValue + offset));
				Assert.AreEqual(expected: expectedUnder, actual: evaluator.EvaluateTypedLambda(expression)(byte.MinValue - offset));

				checked {
					expression = v => (byte)v;
				}

				Assert.AreEqual(expected: byte.MaxValue, actual: evaluator.EvaluateTypedLambda(expression)(byte.MaxValue));
				try {
					evaluator.EvaluateTypedLambda(expression)(byte.MaxValue + offset);
					Assert.Fail();
				} catch (Exception ex) when (ex.InnerException is OverflowException) {
				} catch (OverflowException) {
				} catch {
					Assert.Fail();
				}

				try {
					evaluator.EvaluateTypedLambda(expression)(byte.MinValue - offset);
					Assert.Fail();
				} catch (Exception ex) when (ex.InnerException is OverflowException) {
				} catch (OverflowException) {
				} catch {
					Assert.Fail();
				}
			}
		}

		[Test]
		public void TestNewArrayExpression() {
			Expression<Func<int[]>> expression = () => new[] { 2, 3, 5, 7 };

			var res = evaluator.Evaluate(expression.Body);

			Assert.That(res, Is.InstanceOf<int[]>());
			Assert.That(((int[])res)[0], Is.EqualTo(2));
			Assert.That(((int[])res)[1], Is.EqualTo(3));
			Assert.That(((int[])res)[2], Is.EqualTo(5));
			Assert.That(((int[])res)[3], Is.EqualTo(7));
		}

		[Test]
		public void TestQuoteExpression() {
			Expression<Func<int, Expression<Func<int, int>>>> adderExpressionBuilderExpression = x => y => x + y;
			Func<int, Expression<Func<int, int>>> adderBuilder = evaluator.EvaluateTypedLambda(adderExpressionBuilderExpression);

			Expression<Func<int, int>> adderWithFiveExpression = adderBuilder(5);

			Func<int, int> adderWithFive = evaluator.EvaluateTypedLambda(adderWithFiveExpression);

			Assert.AreEqual(expected: 7, actual: adderWithFive(2));
		}
		
		[Test]
		public void TestLambdaExpression() {
			Expression<Func<int, bool>> expression = i => i < 43 && i > 12 && (i % 3) == 2 && i != 15 || i == 0;

			evaluator.EvaluateLambda(expression)(14);
		}
	}
}
