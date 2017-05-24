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
	internal static class ReflectionExtension {
		/// <summary>
		/// Determines the implementation of an interface-property in an implementing type.
		/// </summary>
		public static PropertyInfo GetImplementationInfo(this PropertyInfo interfaceProperty, Type implementingType) {
			var interfaceGetter = interfaceProperty.GetMethod;

			if (interfaceGetter != null) {
				var getterImpl = GetImplementationInfo(interfaceGetter, implementingType);

				return implementingType.GetTypeInfo().DeclaredProperties.FirstOrDefault(p => p.GetMethod == getterImpl) 
					?? GetImplementationInfo(interfaceProperty, implementingType.GetTypeInfo().BaseType);
			}
			
			var interfaceSetter = interfaceProperty.SetMethod;

			if (interfaceSetter != null) {
				var setterImpl = GetImplementationInfo(interfaceSetter, implementingType);

				return implementingType.GetTypeInfo().DeclaredProperties.FirstOrDefault(p => p.SetMethod == setterImpl)
					?? GetImplementationInfo(interfaceProperty, implementingType.GetTypeInfo().BaseType);
			}

			throw new InvalidOperationException("Properties must have at least a getter or setter!");
		}

		/// <summary>
		/// Determines the implementation of an interface-property in an implementing type.
		/// </summary>
		public static MethodInfo GetImplementationInfo(this MethodInfo interfaceMethod, Type implementingType) {
			var interfaceType = interfaceMethod.DeclaringType;

			if(!interfaceType.GetTypeInfo().IsInterface)
				throw new InvalidOperationException("Method is no interface-declaration!");

			var interfaceMap = implementingType.GetTypeInfo().GetRuntimeInterfaceMap(interfaceType);

			for (int i = 0; i < interfaceMap.InterfaceMethods.Length; i++)
				if (interfaceMap.InterfaceMethods[i] == interfaceMethod)
					return interfaceMap.TargetMethods[i];
			throw new InvalidOperationException("Interfaces must be implemented completly!");
		}
	}
}
