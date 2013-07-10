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

		public static bool ConvertMethodParameters(MethodInfo m, object[] args, out object[] outArgs)
		{
			bool has_defaults = false;
			int paramsIndex = -1;
			var parameters = m.GetParameters();
			var args_len = args.Length;
			var par_len = parameters.Length;

			for (var i = 0; i < par_len; i++) {
				var p = parameters[i];
				var paramArrayAttribute = p.GetCustomAttributes(typeof(ParamArrayAttribute), true);
				if ((paramArrayAttribute != null) && (paramArrayAttribute.Length != 0))
				{
					paramsIndex = i;
					Debug.Assert(paramsIndex == par_len - 1, "Unexpected index for the params parameter");
					// After a params... There is no need to continue to parse further
					if (args_len < paramsIndex) {
						outArgs = null;
						return false;
					}
					break;
				}
				if (i >= args.Length) {
					if ((p.Attributes & ParameterAttributes.HasDefault) != 0) {
						has_defaults = true;
						continue;
					} else {
						outArgs = null;
						return false;
					}
				} else {
					var ptype = p.ParameterType;
					if (args[i] != null) {
						if (!ptype.IsAssignableFrom(args[i].GetType ())) {
							outArgs = null;
							return false;
						}
					} else if (!ptype.IsClass || ptype == typeof(string)) { // $$TODO revisit this
						// argument is null
						outArgs = null;
						return false;
					}
				}
			}

			// default values and params are mutually exclusive in C# (they can't both be in the same signature)
			// So it makes this code a bit simpler
			if (has_defaults) {
				var new_args = new object[par_len];
				for (var j = 0; j < par_len; j++) {
					if (j < args.Length)
						new_args[j] = args[j];
					else
						new_args[j] = parameters[j].DefaultValue;
				}
				outArgs = new_args;
			} else if (paramsIndex >= 0) {
				var new_args = new object[par_len];
				// We reserve the last parameter for special params handling
				// We verified earlier that there was enough args anyway to fill all the parameters (and optionally the parmsIndex)
				// Copy all the other parameters normally
				for (var j = 0; j < paramsIndex; j++) {
					new_args[j] = args[j];
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

			} else {
				outArgs = args;
			}

			// success
			return true;
		}

		public static MethodInfo FindPropertyGetter(Type type, string propertyName)
		{
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
				return System.Convert.ChangeType(value, targetType);
			}
		}

		public static T ConvertValue<T>(object value)
		{
			if (value is T) {
				return (T)value;
			}

			if (value is System.Object) {
				return (T)value;
			}

			return (T)System.Convert.ChangeType(value, typeof(T));
		}

		public static Type GetDelegateTypeForMethod(MethodInfo method)
		{
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
			var method = type.GetMethod(methodName);
			if (method == null) throw new Exception("Method not found");

			var newargs = ConvertArgumentList(method, args);
			return method.Invoke(null, newargs);
		}

		public static bool ObjectIsClass(object o, Type type)
		{
			if (o == null || type == null) return false;
			
			if (type.IsAssignableFrom(o.GetType())) {
				return true;
			} else {
				return false;
			}
		}

		public static bool HasOwnProperty(object o, string name)
		{
			if (o == null || o == PlayScript.Undefined._undefined) return false;

			// handle dictionaries
			var dict = o as IDictionary<string, object>;
			if (dict != null) {
				return dict.ContainsKey(name);
			} 

			var dc = o as IDynamicClass;
			if (dc != null) {
				if (dc.__HasDynamicValue(name)) {
					return true;
				}
				// fall through to check all other properties
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
			Type et;
			if (sExtensions.TryGetValue(type, out et)) {
				return et;
			} else {
				return null;
			}
		}
		

	}
}

