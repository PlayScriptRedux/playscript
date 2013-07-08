using System;
using System.Reflection;
using System.Collections;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace PlayScript
{
	public static class Dynamic
	{
		public static object[] ConvertArgumentList(MethodInfo methodInfo, IList args)
		{
			ParameterInfo[] paramList = methodInfo.GetParameters();
			
			// build new argument list
			object[] newargs = new object[paramList.Length];
			
			// handle parameters we were given
			int i=0;
			if (args != null) {
				for (; i < args.Count; i++)
				{
					newargs[i] = ConvertValue(args[i], paramList[i].ParameterType);
				}
			}
			
			// add default values
			for (; i < paramList.Length; i++)
			{
				newargs[i] = paramList[i].DefaultValue;
			}
			return newargs;
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

