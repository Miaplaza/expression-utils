using System;
using NUnit.Framework;
using MiaPlaza.ExpressionUtils;
using System.Linq.Expressions;

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
			Func<object, LambdaExpression> factory = delegate (object o) {
				Expression<Func<bool>> exp = () => (5 + 17).Equals(o);
				return exp;
			};
			
			var expA = factory(12);
			var expB = factory(12);

			Assert.IsFalse(expA.StructuralIdentical(expB));

			expA = PartialEvaluator.PartialEvalBody(expA, ExpressionUtils.Evaluating.ExpressionInterpreter.Instance);
			expB = PartialEvaluator.PartialEvalBody(expB, ExpressionUtils.Evaluating.ExpressionInterpreter.Instance);

			Assert.IsTrue(expA.StructuralIdentical(expB));
		}
	}
}
