using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiaPlaza.ExpressionUtils {
	/// <summary>
	/// A delegate that accepts an array of objects and returns an object. Used to 
	/// represent the compilation result of <see cref="Delegate"/>s without statically 
	/// known type or internally for syntactic reasons (code generation).
	/// </summary>
	public delegate object VariadicArrayParametersDelegate(params object[] arguments);

	/// <summary>
	/// A delegate that accepts a read only list of objects and returns an object. Used
	/// internally instead of <see cref="VariadicArrayParametersDelegate"/> for performance
	/// reasons (If the arguments need to be changed, copying arrays takes too much time).
	/// </summary>
	delegate object ParameterListDelegate(IReadOnlyList<object> parameters);

	public static class DelegateExtension {
		internal static VariadicArrayParametersDelegate CreateLazy(this Func<VariadicArrayParametersDelegate> creator) {
			VariadicArrayParametersDelegate delegat = null;

			return args => {
				if (delegat == null) {
					delegat = creator();
				}

				return delegat.Invoke(args);
			};
		}

		/// <summary>
		/// Tries calling multiple <see cref="VariadicArrayParametersDelegate"/>s if the current one throws an error.
		/// </summary>
		internal static VariadicArrayParametersDelegate ChainFallbacks(this IEnumerable<VariadicArrayParametersDelegate> delegates) {
			var exceptions = new List<Exception>();
			var delegateEnumerator = delegates.GetEnumerator();

			if (!delegateEnumerator.MoveNext()) { // Move to first.
				throw new ArgumentException(nameof(delegates) + " were empty!");
			}

			return args => {
				do {
					try {
						return delegateEnumerator.Current.Invoke(args);
					} catch (Exception ex) {
						exceptions.Add(ex);
					}
				} while (delegateEnumerator.MoveNext());

				throw new AggregateException("No options left!", exceptions);
			};
		}
	}
}
