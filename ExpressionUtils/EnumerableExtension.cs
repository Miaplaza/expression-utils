using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiaPlaza.ExpressionUtils {
	internal static class EnumerableExtension {
		struct EqualityComparisonComparer<T> : IEqualityComparer<T> {
			readonly Func<T, T, bool> equalityComparison;

			public EqualityComparisonComparer(Func<T, T, bool> equalityComparison) {
				this.equalityComparison = equalityComparison;
			}

			bool IEqualityComparer<T>.Equals(T x, T y) => equalityComparison(x, y);

			int IEqualityComparer<T>.GetHashCode(T obj) => 0;
		}

		public static bool SequenceEqual<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, T, bool> equalityComparison) {
			return first.SequenceEqual(second, new EqualityComparisonComparer<T>(equalityComparison));
		}

		public static bool SequenceEqualOrBothNull<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, T, bool> equalityComparison = null) {
			if (first == second) {
				return true;
			} else if (first == null || second == null) {
				return false;
			} else if (equalityComparison == null) {
				return first.SequenceEqual(second);
			} else {
				return first.SequenceEqual(second, equalityComparison);
			}
		}
	}
}
