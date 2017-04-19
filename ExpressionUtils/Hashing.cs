using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MiaPlaza.ExpressionUtils {
	/// <summary>
	/// Helpers for FNV hashing (http://isthe.com/chongo/tech/comp/fnv/). It works really well.
	/// </summary>
	public static class Hashing {
		/// <summary>
		/// The value to multiply the hash by for each byte
		/// </summary>
		public const int FnvPrime = 16777619;
		public const long FnvPrimeL = 1099511628211L;

		/// <summary>
		/// The initial value of the hash
		/// </summary>
		public const int FnvOffset = unchecked((int)2166136261);
		public const long FnvOffsetL = unchecked((long)14695981039346656037L);

		public static void Hash(ref int hash, byte val) {
			hash ^= val;
			hash *= FnvPrime;
		}

		public static void Hash(ref long hash, byte val) {
			hash ^= val;
			hash *= FnvPrimeL;
		}

		public static void Hash(ref int hash, int val) {
			hash ^= val & 0xFF;
			hash *= FnvPrime;
			hash ^= (val >> 8) & 0xFF;
			hash *= FnvPrime;
			hash ^= (val >> 16) & 0xFF;
			hash *= FnvPrime;
			hash ^= val >> 24;
			hash *= FnvPrime;
		}
		
		public static void Hash(ref long hash, int val) {
			hash ^= val & 0xFF;
			hash *= FnvPrimeL;
			hash ^= (val >> 8) & 0xFF;
			hash *= FnvPrimeL;
			hash ^= (val >> 16) & 0xFF;
			hash *= FnvPrimeL;
			hash ^= val >> 24;
			hash *= FnvPrimeL;
		}
		
		public static void Hash(ref long hash, long val) {
			hash ^= val & 0xFF;
			hash *= FnvPrimeL;
			hash ^= (val >> 8) & 0xFF;
			hash *= FnvPrimeL;
			hash ^= (val >> 16) & 0xFF;
			hash *= FnvPrimeL;
			hash ^= (val >> 24) & 0xFF;
			hash *= FnvPrimeL;
			hash ^= (val >> 32) & 0xFF;
			hash *= FnvPrimeL;
			hash ^= (val >> 40) & 0xFF;
			hash *= FnvPrimeL;
			hash ^= (val >> 48) & 0xFF;
			hash *= FnvPrimeL;
			hash ^= val >> 56;
			hash *= FnvPrimeL;
		}

		/// <summary>
		/// Return a hash value for the ordered sequence of elements in <param name="objects"/>.
		/// <seealso cref="EnumerableExtension.GetSequenceHashCode"/>
		/// </summary>
		/// <remarks>
		/// This method is not called <c>Hash()</c> because it is easy to get confused by the exact
		/// overload that gets called between this method and <c>Hash(params object[])</c> which would
		/// have very different results.
		/// </remarks>
		public static int HashAll(IEnumerable objects) {
			int ret = 0;
			foreach (var o in objects) {
				if (o == null) {
					Hash(ref ret, 1337);
				} else {
					int hash;
					unchecked {
						// we add a random integer to the hash code, so small
						// integers (such as zero) do not have trivial effect
						hash = o.GetHashCode() + 1928529265;
					}
					Hash(ref ret, hash);
				}
			}
			return ret;
		}

		/// <summary>
		/// Return a hash value for the ordered sequence of elements in <param name="objects"/>.
		/// </summary>
		public static int Hash(params object[] objects) => HashAll(objects);
	}
}