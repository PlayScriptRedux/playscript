using System;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace PlayScript
{
	public static class Dynamic
	{
		public static object[] ConvertArgumentList(MethodInfo methodInfo, IList args)
		{
			if (args == null) return null;

			// TODO: Refactor this so we only have one method (and avoid the temporary allocation)
			object[] convertedArgs;
			object[] argsToConvert = new object[args.Count];
			args.CopyTo(argsToConvert, 0);
			if (ConvertMethodParameters(methodInfo, argsToConvert, out convertedArgs))
			{
				return convertedArgs;
			}
			return null;
		}

		public static bool ConvertMethodParameters(MethodBase m, object[] args, out object[] outArgs)
		{
			Stats.Increment(StatsCounter.Dynamic_ConvertMethodParametersInvoked);

			bool has_defaults = false;
			var parameters = m.GetParameters();
			var args_len = args.Length;
			var par_len = parameters.Length;
			int paramsIndex = -1;
			int parameterCheckLength = par_len;
			if (hasVariadicParameter(m)) {
				paramsIndex = --parameterCheckLength;
			}

			// Note that this code does not check if the arguments passed for variadic parameters
			// matches the variadic type (we still convert later though).
			// This might not be important with PlayScript though (where it is just object[]).
			for (var i = 0; i < parameterCheckLength; i++) {
				var p = parameters[i];
				if (i >= args.Length) {
					if ((p.Attributes & ParameterAttributes.HasDefault) != 0) {
						has_defaults = true;
						continue;
					} else {
						outArgs = null;
						return false;
					}
				} else {
					if (!CanConvertValue(args[i], p.ParameterType)) {
						outArgs = null;
						return false;
					}
				}
			}

			// default values and params are mutually exclusive in C# (they can't both be in the same signature)
			// TODO: Check that this is actually true... default, then variadic?
			// So it makes this code a bit simpler
			if (has_defaults) {
				var new_args = new object[par_len];
				for (var j = 0; j < par_len; j++) {
					if (j < args.Length) {
						new_args[j] = ConvertValue(args[j], parameters[j].ParameterType);
					}
					else
						new_args[j] = parameters[j].DefaultValue;
				}
				outArgs = new_args;
			} else if (paramsIndex >= 0) {
				if ((paramsIndex == 0) && (args_len == 1)) {
					// For variadic, there is a special case that we handle here
					// In the case where there is only one argument, and it matches the type of the variadic
					// we assume we receive the variadic array as input directly and no conversion is necessary.
					// This can happen with all the various level of stacks that we have (would be good to investigate
					// and cover with unit-tests).
					//
					// Note that there could be an issue that the passed parameter happened to be of the variadic type,
					// but was actually the first parameter of the variadic array. In this case, the behavior will be incorrect.
					if (parameters[paramsIndex].ParameterType == args[0].GetType()) {
						// Exact same type, assume that we are done.
						// This is a good place to put a breakpoint if you think this is an incorrect assumption.
						outArgs = args;
						return true;
					}
				} else {
					if (args_len < paramsIndex) {
						outArgs = null;
						return false;
					}
				}

				var new_args = new object[par_len];
				// We reserve the last parameter for special params handling
				// We verified earlier that there was enough args anyway to fill all the parameters (and optionally the parmsIndex)
				// Copy all the other parameters normally
				System.Type paramType = parameters[paramsIndex].ParameterType;
				Debug.Assert(paramType.IsArray, "Unexpected type");				// Because it is variadic the type is actually an array (like string[])
//				Debug.Assert(paramType.BaseType.IsArray, "Unexpected type");		// Its base type is an array too (the generic kind this time - System.Array)
				paramType = paramType.BaseType.BaseType;							// Get the type we are interested in (string) for each parameters
				Debug.Assert(paramType != null, "Unexpected type");

				for (var j = 0; j < paramsIndex; j++) {
					new_args[j] = ConvertValue(args[j], parameters[j].ParameterType);
				}
				// Then copy the additional parameters to the last parameter (params) as an array
				// Array can be empty if we have just enough parameters up to the params one
				int numberOfAdditionalParameters = args_len - paramsIndex;
				object[] additionalParameters = new object[numberOfAdditionalParameters];
				new_args[paramsIndex] = additionalParameters;
				for (var j = 0 ; j < numberOfAdditionalParameters ; j++) {
					additionalParameters[j] = args[paramsIndex + j];
				}
				System.Diagnostics.Debug.Assert(paramsIndex + numberOfAdditionalParameters == args_len, "Some arguments have not been handled properly");
				outArgs = new_args;

			} else if (par_len == args_len) {
				outArgs = args;
				// Let's make sure all the parameters are converted
				for (var i = 0 ; i < par_len ; i++) {
					outArgs[i] = ConvertValue(args[i], parameters[i].ParameterType);
				}

			}
			else
			{
				outArgs = null;
				return false;
			}

			// success
			return true;
		}

		public static MethodInfo FindPropertyGetter(Type type, string propertyName)
		{
			Stats.Increment(StatsCounter.Dynamic_FindPropertyGetterInvoked);

			do 
			{
				var prop = type.GetProperty(propertyName);
				if (prop != null) {
					var propType = prop.PropertyType;
					var getter = prop.GetGetMethod();
					if (getter != null) {
						return getter;
					}
				}
				// walk up heirarchy
				type = type.BaseType;
			} while (type != null);

			return null;
		}

		public static MethodInfo FindPropertySetter(Type type, string propertyName)
		{
			Stats.Increment(StatsCounter.Dynamic_FindPropertySetterInvoked);

			do 
			{
				var prop = type.GetProperty(propertyName);
				if (prop != null) {
					var propType = prop.PropertyType;
					var setter = prop.GetSetMethod();
					if (setter != null) {
						return setter;
					}
				}
				// walk up heirarchy
				type = type.BaseType;
			} while (type != null);

			return null;
		}

		public static object ConvertValue(object value, Type targetType)
		{
			Stats.Increment(StatsCounter.Dynamic_ConvertValueInvoked);

			if (value == null) return null;

			Type valueType = value.GetType();
			if (targetType == valueType) {
				return value;
			}

			if (targetType == typeof(System.Object)) {
				return value;
			}

			if (targetType.IsAssignableFrom(valueType)) {
				return value;
			} else {
				if (targetType == typeof(String)) {
					return value.ToString();
				}
				return System.Convert.ChangeType(value, targetType);
			}
		}

		public static bool CanConvertValue(object value, Type targetType)
		{
			Stats.Increment(StatsCounter.Dynamic_CanConvertValueInvoked);

			if (value == null) return true;

			Type valueType = value.GetType();
			if (targetType == valueType) {
				return true;
			}

			if (targetType == typeof(System.Object)) {
				return true;
			}

			if (targetType.IsAssignableFrom(valueType)) {
				return true;
			} else {
				if (targetType == typeof(String)) {
					return true;
				}

				try {
					return (System.Convert.ChangeType(value, targetType) != null);
				}
				catch {
					return false;
				}
			}
		}

		public static T ConvertValue<T>(object value)
		{
			Stats.Increment(StatsCounter.Dynamic_ConvertValueGenericInvoked);

			return Convert<T>.FromObject(value);
		}

		public static Type GetDelegateTypeForMethod(MethodInfo method)
		{
			Stats.Increment(StatsCounter.Dynamic_GetDelegateTypeForMethodInvoked);

			var plist = method.GetParameters();

			// build delegate type
			Type delegateType;
			if (method.ReturnType == typeof(void)) {
				var typeArgs = new Type[plist.Length];
				for (int i=0; i < plist.Length; i++) {
					typeArgs[i] = plist[i].ParameterType;
				}
				delegateType = Expression.GetActionType(typeArgs);
			} else {
				var typeArgs = new Type[plist.Length + 1];
				for (int i=0; i < plist.Length; i++) {
					typeArgs[i] = plist[i].ParameterType;
				}
				typeArgs[plist.Length] = method.ReturnType;
				delegateType = Expression.GetFuncType(typeArgs);
			}
			return delegateType;
		}

		public static bool GetInstanceMember(object o, string name, out object value)
		{
			Stats.Increment(StatsCounter.Dynamic_GetInstanceMemberInvoked);

			var type = o.GetType();
			
			var prop = FindPropertyGetter(type, name);
			if (prop != null) {
				value = prop.Invoke(o, null);
				return true;
			}
			
			var field = type.GetField(name);
			if (field != null) {
				value = field.GetValue(o); 
				return true;
			}

			var method = type.GetMethod(name);
			if (method != null) {
				value = Delegate.CreateDelegate(GetDelegateTypeForMethod(method), o, method);
				return true;
			}

			// not found
			value = null;
			return false;
		}


		public static bool SetInstanceMember(object o, string name, object value)
		{
			Stats.Increment(StatsCounter.Dynamic_SetInstanceMemberInvoked);

			var type = o.GetType();
			
			var prop = type.GetProperty(name);
			if (prop != null) {
				object newValue = Dynamic.ConvertValue(value, prop.PropertyType);
				prop.SetValue(o, newValue, null);
				return true;
			}
			
			var field = type.GetField(name);
			if (field != null) {
				object newValue = Dynamic.ConvertValue(value, field.FieldType);
				field.SetValue(o, newValue);
				return true;
			}

			return false;
		}


		public static bool GetStaticMember(Type type, string name, out object v)
		{
			Stats.Increment(StatsCounter.Dynamic_GetStaticMemberInvoked);

			var property = type.GetProperty(name);
			if (property != null) {
				v = property.GetValue(null, null);
				return true;
			}

			var field = type.GetField(name);
			if (field != null) {
				v = field.GetValue(null);
				return true;
			}

			var method = type.GetMethod(name);
			if (method != null) {
				v = Delegate.CreateDelegate(GetDelegateTypeForMethod(method), method);
				return true;
			}

			// not found
			v = null;
			return false;
		}
		
		public static bool SetStaticMember(Type type, string name, object v)
		{
			Stats.Increment(StatsCounter.Dynamic_SetStaticMemberInvoked);

			var property = type.GetProperty(name);
			if (property != null) {
				property.SetValue(null, v, null);
				return true;
			}

			var field = type.GetField(name);
			if (field != null) {
				field.SetValue(null, v);
				return true;
			}
			return false;
		}

		public static bool CastObjectToBool(object a)
		{
			Stats.Increment(StatsCounter.Dynamic_CastObjectToBoolInvoked);

			if (a is bool) {
				return (bool)a;
			} if (a is int) {
				return ((int)a) != 0;
			} else if (a is double) {
				return ((double)a) != 0.0;
			} else if (a is uint) {
				return ((uint)a) != 0;
			} else if (a is string) {
				return !string.IsNullOrEmpty((string)a);
			} else if (a == PlayScript.Undefined._undefined) {
				return false;
			} else {
				// see if object reference is non-null
				return (a != null);
			}
		}

		public static object InvokeStaticMethod(Type type, string methodName, IList args)
		{
			Stats.Increment(StatsCounter.Dynamic_InvokeStaticInvoked);

			var method = type.GetMethod(methodName);
			if (method == null) throw new Exception("Method not found");

			var newargs = ConvertArgumentList(method, args);
			return method.Invoke(null, newargs);
		}

		public static bool ObjectIsClass(object o, Type type)
		{
			Stats.Increment(StatsCounter.Dynamic_ObjectIsClassInvoked);

			if (o == null || type == null) return false;
			
			if (type.IsAssignableFrom(o.GetType())) {
				return true;
			} else {
				return false;
			}
		}

		public static bool hasOwnProperty(object o, object name)
		{
			if (name == null) {
				return false;
			}
			// convert name to string if its not already
			var strName = name as string;
			if (strName == null) {
				strName = name.ToString();
			}
			return HasOwnProperty(o, strName);
		}

		public static bool HasOwnProperty(object o, string name)
		{
			Stats.Increment(StatsCounter.Dynamic_HasOwnPropertyInvoked);

			if (o == null || o == PlayScript.Undefined._undefined) return false;

			// handle dictionaries
			var dict = o as IDictionary<string, object>;
			if (dict != null) {
				return dict.ContainsKey(name);
			} 

			var dc = o as IDynamicClass;
			if (dc != null) {
				return dc.__HasDynamicValue(name);
			}

			var otype = o.GetType();

			var prop = otype.GetProperty(name);
			if (prop != null) return true;

			var field = otype.GetField(name);
			if (field != null) return true;

			var method = otype.GetMethod(name);
			if (method != null) return true;

			// not found
			return false;
		}

		private static Dictionary<Type, Type>  sExtensions = new Dictionary<Type, Type>();
		public static void RegisterExtensionClass(System.Type type, System.Type extensionType)
		{
			sExtensions[type] = extensionType;
		} 

		public static Type GetExtensionClassForType(Type type)
		{
			if (!sExtensionMethodsInitialized) {
				InitializeExtensionMethods();
			}

			Type et;
			if (sExtensions.TryGetValue(type, out et)) {
				return et;
			} else {
				return null;
			}
		}

		// TODO: Change this to cache more method information (number of non-default parameters for example, this would make ConvertMethodParameters faster)
		private static Dictionary<MethodBase,bool> sMethodHasVariadicParameter = new Dictionary<MethodBase, bool>();
		private static bool hasVariadicParameter(MethodBase method)
		{
			// Finding variadic parameters is quite expensive, so we cache the result.
			bool hasVariadicParameter;
			if (sMethodHasVariadicParameter.TryGetValue(method, out hasVariadicParameter) == false)
			{
				hasVariadicParameter = false;

				var parameters = method.GetParameters();
				int numberOfParameters = parameters.Length;
				if (numberOfParameters != 0)
				{
					var p = parameters[numberOfParameters - 1];		// Variadic parameters are always the last parameter

					var paramArrayAttribute = p.GetCustomAttributes(typeof(ParamArrayAttribute), true);
					if ((paramArrayAttribute != null) && (paramArrayAttribute.Length != 0))
					{
						hasVariadicParameter = true;
					}
				}
				sMethodHasVariadicParameter.Add(method, hasVariadicParameter);
			}
			return hasVariadicParameter;
		}

		private static void InitializeExtensionMethods()
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (var type in assembly.GetTypes())
				{
					PlayScript.ExtensionAttribute attribute = Attribute.GetCustomAttribute(type, typeof(PlayScript.ExtensionAttribute)) as PlayScript.ExtensionAttribute;
					if (attribute != null)
					{
						RegisterExtensionClass(attribute.OverloadedType, type);
					}
				}
			}

			sExtensionMethodsInitialized = true;
		}

		private static bool sExtensionMethodsInitialized = false;
	}
}

