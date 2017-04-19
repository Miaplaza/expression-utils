using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiaPlaza.ExpressionUtils;
using NUnit.Framework;
using System.Linq.Expressions;
using System.Reflection;

namespace MiaPlaza.Test.ExpressionUtilsTest {
	/// <summary>
	/// Tests the <see cref="ExpressionUtils.ParameterSubstituter"/>.
	/// </summary>
	[TestFixture]
	class ParameterSubstituter {
		class LabelAttribute : Attribute {
			public readonly string Label;

			public LabelAttribute(string label) {
				Label = label;
			}
		}

		interface IInterface {
			[Label("interface")]
			bool Property { get; }

			[Label("interface")]
			string Method(int parameter);
		}

		class InterfaceImplementation : IInterface {
			[Label("implementation")]
			public bool Property {
				get {
					return CORRECT_PROPERTY_RESULT;
				}
			}

			[Label("implementation")]
			public string Method(int parameter) {
				return CORRECT_METHOD_RESULT;
			}
		}

		class ExplicitInterfaceImplementation : IInterface {
			[Label("nottheimplementation")]
			public bool Property {
				get {
					return INCORRECT_PROPERTY_RESULT;
				}
			}

			[Label("nottheimplementation")]
			public string Method(int parameter) {
				return INCORRECT_METHOD_RESULT;
			}

			[Label("explicitimplementation")]
			bool IInterface.Property {
				get {
					return CORRECT_PROPERTY_RESULT;
				}
			}

			[Label("explicitimplementation")]
			string IInterface.Method(int parameter) {
				return CORRECT_METHOD_RESULT;
			}
		}

		abstract class AbstractClass {
			[Label("abstract")]
			public abstract bool Property { get; }

			[Label("abstract")]
			public abstract string Method(int parameter);
		}

		class ImplementingClass : AbstractClass {
			[Label("implementation")]
			public override bool Property {
				get {
					return CORRECT_PROPERTY_RESULT;
				}
			}

			[Label("implementation")]
			public override string Method(int parameter) {
				return CORRECT_METHOD_RESULT;
			}
		}

		class ShadowingClass : ImplementingClass {
			[Label("shadowing")]
			public new bool Property {
				get {
					return INCORRECT_PROPERTY_RESULT;
				}
			}

			[Label("shadowing")]
			public new string Method(int parameter) {
				return INCORRECT_METHOD_RESULT;
			}
		}

		class VirtualClass {
			[Label("virtual")]
			public virtual bool Property { get { return INCORRECT_PROPERTY_RESULT; } }

			[Label("virtual")]
			public virtual string Method(int parameter) => INCORRECT_METHOD_RESULT;
		}

		class OverridingClass : VirtualClass {
			[Label("override")]
			public override bool Property {
				get {
					return CORRECT_PROPERTY_RESULT;
				}
			}

			[Label("override")]
			public override string Method(int parameter) {
				return CORRECT_METHOD_RESULT;
			}
		}


		private const bool CORRECT_PROPERTY_RESULT = true;
		private const string CORRECT_METHOD_RESULT = "correct";
		private const bool INCORRECT_PROPERTY_RESULT = false;
		private const string INCORRECT_METHOD_RESULT = "incorrect";

		private static readonly Expression<Func<int, string, bool>> simpleExpression = (i, s) => i.ToString() == s;
		private static readonly Expression<Func<bool>> parameterlessExpression = () => true;
		private static readonly Expression<Func<IInterface, bool>> interfacePropertyExpression = i => i.Property;
		private static readonly Expression<Func<IInterface, bool>> interfacePropertyCastExpression = i => ((IInterface)i).Property;
		private static readonly Expression<Func<IInterface, string>> interfaceMethodExpression = i => i.Method(42);
		private static readonly Expression<Func<IInterface, string>> interfaceMethodCastExpression = i => ((IInterface)i).Method(42);
		private static readonly Expression<Func<AbstractClass, bool>> abstractPropertyExpression = i => i.Property;
		private static readonly Expression<Func<AbstractClass, bool>> abstractPropertyCastExpression = i => ((AbstractClass)i).Property;
		private static readonly Expression<Func<AbstractClass, string>> abstractMethodExpression = i => i.Method(42);
		private static readonly Expression<Func<AbstractClass, string>> abstractMethodCastExpression = i => ((AbstractClass)i).Method(42);
		private static readonly Expression<Func<VirtualClass, bool>> virtualPropertyExpression = i => i.Property;
		private static readonly Expression<Func<VirtualClass, bool>> virtualPropertyCastExpression = i => ((VirtualClass)i).Property;
		private static readonly Expression<Func<VirtualClass, string>> virtualMethodExpression = i => i.Method(42);
		private static readonly Expression<Func<VirtualClass, string>> virtualMethodCastExpression = i => ((VirtualClass)i).Method(42);

		[Test]
		public void TestArgumentNullExceptions() {
			Assert.Throws<ArgumentNullException>(() =>
				ExpressionUtils.ParameterSubstituter.SubstituteParameter(simpleExpression, replacements: null));
			Assert.Throws<ArgumentNullException>(() =>
				ExpressionUtils.ParameterSubstituter.SubstituteParameter(expression: null, replacements: null));
			Assert.Throws<ArgumentNullException>(() =>
				ExpressionUtils.ParameterSubstituter.SubstituteParameter(null, replacements: new[] {
					Expression.Constant(5),
					Expression.Constant("42")
				}));
		}

		[Test]
		public void TestArgumentExceptionsForInvalidArguments() {
			Assert.Throws<ArgumentException>(() =>
				ExpressionUtils.ParameterSubstituter.SubstituteParameter(simpleExpression, Expression.Constant(5)));
			Assert.Throws<ArgumentException>(() =>
				ExpressionUtils.ParameterSubstituter.SubstituteParameter(simpleExpression,
					Expression.Constant(5), Expression.Constant(42)));
			Assert.Throws<ArgumentException>(() =>
				ExpressionUtils.ParameterSubstituter.SubstituteParameter(simpleExpression,
					Expression.Constant(5), Expression.Constant("42"), Expression.Constant(true)));
		}

		[Test]
		public void TestDoesNotThrowOnValidArguments() {
			ExpressionUtils.ParameterSubstituter.SubstituteParameter(simpleExpression,
					Expression.Constant(5), Expression.Constant("42"));
		}

		[Test]
		public void TestNoReplacementsCauseNoChange() {
			var substituted = ExpressionUtils.ParameterSubstituter.SubstituteParameter(parameterlessExpression);

			Assert.AreSame(expected: parameterlessExpression.Body, actual: substituted);
		}

		[Test]
		public void TestInterfacePropertyImplementation() {
			foreach (var expression in new[] { interfacePropertyExpression, interfacePropertyCastExpression }) {
				var result = ExpressionUtils.ParameterSubstituter.SubstituteParameter(expression, Expression.Constant(new InterfaceImplementation()));
				Assert.AreEqual(expected: "implementation", actual: ((MemberExpression)result).Member.GetCustomAttribute<LabelAttribute>().Label);
			}
		}

		[Test]
		public void TestInterfaceMethodImplementation() {
			foreach (var expression in new[] { interfaceMethodExpression, interfaceMethodCastExpression }) {
				var result = ExpressionUtils.ParameterSubstituter.SubstituteParameter(expression, Expression.Constant(new InterfaceImplementation()));
				Assert.AreEqual(expected: "implementation", actual: ((MethodCallExpression)result).Method.GetCustomAttribute<LabelAttribute>().Label);
			}
		}

		[Test]
		public void TestExplicitInterfacePropertyImplementation() {
			foreach (var expression in new[] { interfacePropertyExpression, interfacePropertyCastExpression }) {
				var result = ExpressionUtils.ParameterSubstituter.SubstituteParameter(expression, Expression.Constant(new ExplicitInterfaceImplementation()));
				Assert.AreEqual(expected: "explicitimplementation", actual: ((MemberExpression)result).Member.GetCustomAttribute<LabelAttribute>().Label);
			}
		}

		[Test]
		public void TestExplicitInterfaceMethodImplementation() {
			foreach (var expression in new[] { interfaceMethodExpression, interfaceMethodCastExpression }) {
				var result = ExpressionUtils.ParameterSubstituter.SubstituteParameter(expression, Expression.Constant(new ExplicitInterfaceImplementation()));
				Assert.AreEqual(expected: "explicitimplementation", actual: ((MethodCallExpression)result).Method.GetCustomAttribute<LabelAttribute>().Label);
			}
		}

		[Test]
		public void TestAbstractPropertyImplementation() {
			foreach (var expression in new[] { abstractPropertyExpression, abstractPropertyCastExpression }) {
				var result = ExpressionUtils.ParameterSubstituter.SubstituteParameter(expression, Expression.Constant(new ImplementingClass()));
				Assert.AreEqual(expected: "implementation", actual: ((MemberExpression)result).Member.GetCustomAttribute<LabelAttribute>().Label);
			}
		}

		[Test]
		public void TestAbstractMethodImplementation() {
			foreach (var expression in new[] { abstractMethodExpression, abstractMethodCastExpression }) {
				var result = ExpressionUtils.ParameterSubstituter.SubstituteParameter(expression, Expression.Constant(new ImplementingClass()));
				Assert.AreEqual(expected: "implementation", actual: ((MethodCallExpression)result).Method.GetCustomAttribute<LabelAttribute>().Label);
			}
		}

		[Test]
		public void TestOverridePropertyImplementation() {
			foreach (var expression in new[] { virtualPropertyExpression, virtualPropertyCastExpression }) {
				var result = ExpressionUtils.ParameterSubstituter.SubstituteParameter(expression, Expression.Constant(new OverridingClass()));
				Assert.AreEqual(expected: "override", actual: ((MemberExpression)result).Member.GetCustomAttribute<LabelAttribute>().Label);
			}
		}

		[Test]
		public void TestOverrideMethodImplementation() {
			foreach (var expression in new[] { virtualMethodExpression, virtualMethodCastExpression }) {
				var result = ExpressionUtils.ParameterSubstituter.SubstituteParameter(expression, Expression.Constant(new OverridingClass()));
				Assert.AreEqual(expected: "override", actual: ((MethodCallExpression)result).Method.GetCustomAttribute<LabelAttribute>().Label);
			}
		}

		[Test]
		public void TestShadowedAbstractPropertyImplementation() {
			foreach (var expression in new[] { abstractPropertyExpression, abstractPropertyCastExpression }) {
				var result = ExpressionUtils.ParameterSubstituter.SubstituteParameter(expression, Expression.Constant(new ShadowingClass()));
				Assert.Ignore("Overload detection in classes with hiding members does not work yet.");
				Assert.AreEqual(expected: "implementation", actual: ((MemberExpression)result).Member.GetCustomAttribute<LabelAttribute>().Label);
			}
		}

		[Test]
		public void TestShadowedAbstractMethodImplementation() {
			foreach (var expression in new[] { abstractMethodExpression, abstractMethodCastExpression }) {
				var result = ExpressionUtils.ParameterSubstituter.SubstituteParameter(expression, Expression.Constant(new ShadowingClass()));
				Assert.Ignore("Overload detection in classes with hiding members does not work yet.");
				Assert.AreEqual(expected: "implementation", actual: ((MethodCallExpression)result).Method.GetCustomAttribute<LabelAttribute>().Label);
			}
		}
	}
}
