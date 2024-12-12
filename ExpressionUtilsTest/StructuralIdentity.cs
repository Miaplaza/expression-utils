using System;
using NUnit.Framework;
using MiaPlaza.ExpressionUtils;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace MiaPlaza.Test.ExpressionUtilsTest {
	[TestFixture]
	public class StructuralIdentity {
		[Test]
		public void NullTreatmentTest() {
			Assert.IsTrue((null as Expression).StructuralIdentical(null));

			Expression exp = Expression.Constant(12);

			Assert.IsFalse(exp.StructuralIdentical(null));
			Assert.IsFalse((null as Expression).StructuralIdentical(exp));
		}

		[Test]
		public void DifferentNodeType() {
			Assert.IsFalse(Expression.Constant(12).StructuralIdentical(Expression.Parameter(typeof(int))));
		}

		[Test]
		public void Constants() {
			var expA = Expression.Constant(12);
			var expB = Expression.Constant(12);

			Assert.IsTrue(expA.StructuralIdentical(expB));

			expA = Expression.Constant(new object());
			expB = Expression.Constant(new object());

			Assert.IsFalse(expA.StructuralIdentical(expB));

			var obj = new object();

			expA = Expression.Constant(obj);
			expB = Expression.Constant(obj);

			Assert.IsTrue(expA.StructuralIdentical(expB));
		}

		[Test]
		public void Lambdas() {
			Func<LambdaExpression> factory = delegate {
				Expression<Func<bool>> exp = () => (5 + 17) == 20;
				return exp;
			};

			var expA = factory();
			var expB = factory();

			Assert.IsTrue(expA.StructuralIdentical(expB));
		}

		[Test]
		public void Closures() {
			Func<object, Expression<Func<bool>>> factory = delegate (object o) {
				return () => (5 + 17).Equals(o);
			};
			
			var expA = factory(12);
			var expB = factory(12);

			Assert.IsFalse(expA.StructuralIdentical(expB));

			expA = PartialEvaluator.PartialEval(expA, ExpressionUtils.Evaluating.ExpressionInterpreter.Instance);
			expB = PartialEvaluator.PartialEval(expB, ExpressionUtils.Evaluating.ExpressionInterpreter.Instance);

			Assert.IsTrue(expA.StructuralIdentical(expB));
		}

		[Test]
		public void ConstantIgnoring() {
			Func<int, LambdaExpression> buildExpression = c => {
				Expression<Func<bool>> exp = () => (5 + 17) == c;
				return exp;
			};

			var expA = buildExpression(10);
			var expB = buildExpression(12);

			Assert.IsTrue(expA.StructuralIdentical(expB, ignoreConstantValues: true));
		}

		[Test]
		public void Conditionals() {
			Func<LambdaExpression> buildExpression = () => {
				Expression<Func<int, bool>> exp = x => x % 2 == 0 ? x == 14 : x == 15;
				return exp;
			};

			var expA = buildExpression();
			var expB = buildExpression();

			Assert.IsTrue(expA.StructuralIdentical(expB));
		}

		[Test]
		public void IndexAccesses() {
			IReadOnlyList<int> array = new int[9];

			Func<LambdaExpression> buildExpression = () => {
				Expression<Func<int, bool>> exp = x => x < 9 && array[x] > 2;
				return exp;
			};

			var expA = buildExpression();
			var expB = buildExpression();

			Assert.IsTrue(expA.StructuralIdentical(expB));
		}

		[Test]
		public void TypeBinary() {
			Func<LambdaExpression> buildExpression = () => {
				Expression<Func<object, bool>> exp = x => x is string;
				return exp;
			};

			var expA = buildExpression();
			var expB = buildExpression();

			Assert.IsTrue(expA.StructuralIdentical(expB));
		}

		[Test]
		public void Unary() {
			Func<LambdaExpression> buildExpression = () => {
				Expression<Func<int, bool>> exp = x => -x > 4;
				return exp;
			};

			var expA = buildExpression();
			var expB = buildExpression();

			Assert.IsTrue(expA.StructuralIdentical(expB));
		}

		[Test]
		public void New() {
			Func<LambdaExpression> buildExpression = () => {
				Expression<Func<bool>> exp = () => new object() != new object();
				return exp;
			};

			var expA = buildExpression();
			var expB = buildExpression();

			Assert.IsTrue(expA.StructuralIdentical(expB));
		}

		[Test]
		public void NewArray() {
			Func<LambdaExpression> buildExpression = () => {
				Expression<Func<int[]>> exp = () => new int[0];
				return exp;
			};

			var expA = buildExpression();
			var expB = buildExpression();

			Assert.IsTrue(expA.StructuralIdentical(expB));
		}

		[Test]
		public void NewArrayInit() {
			Func<LambdaExpression> buildExpression = () => {
				Expression<Func<object>> exp = () => new List<int>() { 2, 3, 5, 7 };
				return exp;
			};

			var expA = buildExpression();
			var expB = buildExpression();

			Assert.IsTrue(expA.StructuralIdentical(expB));
		}

		[Test]
		public void ComplexLambda() {
			Func<Expression<Func<int, bool>>> buildExpression = () => i => i == 0 || (i < 43 && i > 12 && (i % 3) == 2 && i != 15);

			var expA = buildExpression();
			var expB = buildExpression();

			Assert.IsTrue(expA.StructuralIdentical(expB));
		}

		[Test]
		public void TestClosureLambda() {
			int variable = 7;
			Expression<Func<int, bool>> expA = (int a) => a != variable;
			Expression<Func<int, bool>> expB = (int a) => a != 8;

			Assert.IsFalse(expA.StructuralIdentical(expB, true));
		}		
	}
}
