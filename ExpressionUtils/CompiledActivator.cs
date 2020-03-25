using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace MiaPlaza.ExpressionUtils {
	/// <summary>
	/// Provides fast default constructor invocation for code with a <c>T : new()</c> constraint.
	/// </summary>
	/// <remarks>
	/// Surprisingly, default constructor invocation for a type <c>T : new()</c> is very slow.
	/// It is much faster to compile a call to <c>new</c> for the specific type.
	/// https://stackoverflow.com/questions/367577/why-does-the-c-sharp-compiler-emit-activator-createinstance-when-calling-new-in
	/// </remarks>
	public static class CompiledActivator<T> where T : new() {
		private static readonly Func<T> cachedNew = Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();

		/// <summary>
		/// Create an instance of <typeparamref="T"/>.
		/// This is much faster than calling <c>new T()</c> for a generic type
		/// parameter with a <c>T : new()</c> constraint.
		/// </summary>
		public static T Create(){
			return cachedNew.Invoke();
		}
	}
		
	/// <summary>
	/// Provides fast default constructor invocation for code without a <c>T : new()</c> constraint.
	/// </summary>
	/// <remarks>
	/// <c>Activator.CreateInstance()</c> is very slow. It is much faster to compile a call to <c>new</c> for
	/// the specific type.
	/// https://stackoverflow.com/questions/6582259/fast-creation-of-objects-instead-of-activator-createinstancetype
	/// </remarks>
	public static class CompiledActivator {
		/// <summary>
		/// Creates instances which extend <typeparamref name="T"/>
		/// If your type supports a <c>T : new()</c> constraint, use <c>CompiledActivator&lt;T&gt;</c> instead
		/// which provides improved compile time safety.
		/// </summary>
		/// <remarks>
		/// This static subclass exists to reduce the number of concurrent accesses to the <c>cachedNew</c> dictionary below:
		/// For each <paramref name="T"/> we get a separate dictionary.
		/// </remarks>
		public static class ForBaseType<T> {
			private static ConcurrentDictionary<Type, Func<T>> cachedNew = new ConcurrentDictionary<Type, Func<T>>();

			/// <summary>
			/// Create a <paramref name="t"/> which can be cast to a <typeparamref name="T"/>,
			/// by invoking its default constructor.
			/// This is much faster than <c>(T)Activator.CreateInstance(t)</c>.
			/// </summary>
			public static T Create(Type t) {
				// we do not need to check whether t is really a T, the compilation below will fail if the cast is not possible.
				Func<T> constructor;
				// We do not need to lock the dictionary; another thread can only overwrite it with the same value
				if (!cachedNew.TryGetValue(t, out constructor)) {
					constructor = Expression.Lambda<Func<T>>(Expression.New(t)).Compile();
					cachedNew[t] = constructor;
				}
				return constructor.Invoke();
			}
		}
		
		public static class ForAnyType {
			private static ConcurrentDictionary<Type, Func<object>> cachedNew = new ConcurrentDictionary<Type, Func<object>>();

			/// <summary>
			/// Create a <paramref name="t"/> by invoking its default constructor.
			/// This is much faster than <c>(T)Activator.CreateInstance(t)</c>.
			/// </summary>
			public static object Create(Type t) {
				Func<object> constructor;
				// We do not need to lock the dictionary; another thread can only overwrite it with the same value
				if (!cachedNew.TryGetValue(t, out constructor)) {

					constructor = Expression.Lambda<Func<object>>(Expression.TypeAs(Expression.New(t), typeof(object))).Compile();
					cachedNew[t] = constructor;
				}
				return constructor.Invoke();
			}
		}
	}
}