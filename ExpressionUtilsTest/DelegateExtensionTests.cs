using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using MiaPlaza.ExpressionUtils;

namespace MiaPlaza.Test.ExpressionUtilsTest {
	[TestFixture]
	public class DelegateExtensionTests {
		[Test]
		public void CreateLazyIsLazyTest() {
			int creationCount = 0;
			Func<VariadicArrayParametersDelegate> creator = () => {
				creationCount++;
				return args => "success!";
			};
			Assert.AreEqual(expected: 0, actual: creationCount);

			VariadicArrayParametersDelegate delegat = creator.CreateLazy();
			Assert.AreEqual(expected: 0, actual: creationCount);

			Assert.AreEqual(expected: "success!", actual: delegat.Invoke());
			Assert.AreEqual(expected: 1, actual: creationCount);

			Assert.AreEqual(expected: "success!", actual: delegat.Invoke());
			Assert.AreEqual(expected: 1, actual: creationCount);
		}

		[Test]
		public void ChainFallbacksTest() {
			int callsToFirst = 0, callsToSecond = 0;
			VariadicArrayParametersDelegate
				first = args => { throw new Exception($"{++callsToFirst}"); },
				second = args => { callsToSecond++; return 4; };
			Assert.AreEqual(expected: 0, actual: callsToFirst);
			Assert.AreEqual(expected: 0, actual: callsToSecond);

			var result = new[] { first, second }.ChainFallbacks();
			Assert.AreEqual(expected: 0, actual: callsToFirst);
			Assert.AreEqual(expected: 0, actual: callsToSecond);

			Assert.Throws<Exception>(() => first());
			Assert.AreEqual(expected: 1, actual: callsToFirst);
			Assert.AreEqual(expected: 0, actual: callsToSecond);

			Assert.AreEqual(expected: 4, actual: second());
			Assert.AreEqual(expected: 1, actual: callsToFirst);
			Assert.AreEqual(expected: 1, actual: callsToSecond);

			Assert.AreEqual(expected: 4, actual: result());
			Assert.AreEqual(expected: 2, actual: callsToFirst);
			Assert.AreEqual(expected: 2, actual: callsToSecond);

			Assert.AreEqual(expected: 4, actual: result());
			Assert.AreEqual(expected: 2, actual: callsToFirst);
			Assert.AreEqual(expected: 3, actual: callsToSecond);
		}

		[Test]
		public void DelegateWrappingTest() {
			VariadicArrayParametersDelegate del = args => (int)args[0] + (int)args[1];

			Func<int, int, int> wrapped = del.WrapDelegate<Func<int, int, int>>();

			int result = wrapped(30, 12);

			Assert.AreEqual(expected: 42, actual: result);
		}
	}
}
