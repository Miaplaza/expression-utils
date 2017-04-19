using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace MiaPlaza.ExpressionUtils {
	/// <summary>
	/// Collects custom extension-methods to ease things we do with reflection
	/// </summary>
	public static class ReflectionExtension {
		private static readonly BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

		/// <summary>
		/// Determines the implementation of an interface-property in an implementing type.
		/// </summary>
		public static PropertyInfo GetImplementationInfo(this PropertyInfo interfaceProperty, Type implementingType) {
			var interfaceGetter = interfaceProperty.GetGetMethod();

			if (interfaceGetter != null) {
				var getterImpl = GetImplementationInfo(interfaceGetter, implementingType);

				return implementingType.GetProperties(bindingFlags | BindingFlags.GetProperty).FirstOrDefault(p => p.GetGetMethod(true) == getterImpl) 
					?? GetImplementationInfo(interfaceProperty, implementingType.BaseType);
			}
			
			var interfaceSetter = interfaceProperty.GetSetMethod();

			if (interfaceSetter != null) {
				var setterImpl = GetImplementationInfo(interfaceSetter, implementingType);

				return implementingType.GetProperties(bindingFlags | BindingFlags.SetProperty).FirstOrDefault(p => p.GetSetMethod(true) == setterImpl)
					?? GetImplementationInfo(interfaceProperty, implementingType.BaseType);
			}

			throw new InvalidOperationException("Properties must have at least a getter or setter!");
		}

		/// <summary>
		/// Determines the implementation of an interface-property in an implementing type.
		/// </summary>
		public static MethodInfo GetImplementationInfo(this MethodInfo interfaceMethod, Type implementingType) {
			var interfaceType = interfaceMethod.DeclaringType;

			if(!interfaceType.IsInterface)
				throw new InvalidOperationException("Method is no interface-declaration!");

			var interfaceMap = implementingType.GetInterfaceMap(interfaceType);

			for (int i = 0; i < interfaceMap.InterfaceMethods.Length; i++)
				if (interfaceMap.InterfaceMethods[i] == interfaceMethod)
					return interfaceMap.TargetMethods[i];
			throw new InvalidOperationException("Interfaces must be implemented completly!");
		}
	}
}
