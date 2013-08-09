using System;
using System.Reflection;
using System.Collections.Generic;

namespace PlayScript
{
	public class MethodBinder
	{
		public readonly MethodInfo  		Method;
		public readonly ParameterInfo[] 	Parameters;
		public readonly int 				MinArgumentCount;
		public readonly int 				MaxArgumentCount;
		public readonly int 				ParameterCount;
		public readonly bool 				IsExtensionMethod;
		public readonly bool 				IsVariadic;

		public MethodBinder(MethodInfo method, bool isExtensionMethod)
		{
			Method = method;
			IsExtensionMethod = isExtensionMethod;

			// get method parameters
			Parameters = method.GetParameters();
			ParameterCount = Parameters.Length;

			// see if this method is variadic
			if (Parameters.Length > 0)
			{
				var lastParameter = Parameters[Parameters.Length - 1];
				// determine variadic state of this method
				var paramArrayAttribute = lastParameter.GetCustomAttributes(typeof(ParamArrayAttribute), true);
				if ((paramArrayAttribute != null) && (paramArrayAttribute.Length != 0))
				{
					IsVariadic = true;
					// we have one less parameter since we are variadic
					ParameterCount--;
				}
			}

			// determine required argument count
			MinArgumentCount = 0;
			for (int i=0; i < ParameterCount; i++) {
				if (Parameters[i].IsOptional) {
					break;
				}
				MinArgumentCount++;
			}
			MaxArgumentCount = !IsVariadic ? ParameterCount : int.MaxValue;

			if (IsExtensionMethod) {
				MinArgumentCount--;
				MaxArgumentCount--;
			}
		}

		public bool CheckArguments(object[] args) {
			int startParameter = IsExtensionMethod ? 1 : 0;
			// check required arguments
			for (int i=0; i < MinArgumentCount; i++)
			{
				object arg = args[i];
				if (arg != null)
				{
					Type argType = arg.GetType();
					Type paramType = Parameters[startParameter + i].ParameterType;
					if (!paramType.IsAssignableFrom(argType)) {
						// not compatible
						return false;
					}
				}
			}
			// all arguments are compatible
			return true;
		}

		public bool CheckArgumentCount(int argCount) {
			// ensure argument count is within range
			return (argCount >= MinArgumentCount) && (argCount <= MaxArgumentCount);
		}

		public static bool ConvertArguments(MethodInfo methodInfo, object thisObj, object[] args, int argCount, ref object[] outArgs)
		{
			MethodBinder methodBinder;
			if (sMethodInfoCache.TryGetValue(methodInfo, out methodBinder) == false)
			{
				methodBinder = new MethodBinder(methodInfo, false);		// We assume that if we reached by this code path, it is not an extension method
																		// TODO: This assumption might need more care
				sMethodInfoCache.Add(methodInfo, methodBinder);
			}
			return methodBinder.ConvertArguments(thisObj, args, argCount, ref outArgs);
		}

		public bool ConvertArguments(object thisObj, object[] args, int argCount, ref object[] outArgs)
		{
			// index in parameters (outArgs)
			int i = 0;
			// index in arguments (args)
			int argIndex = 0;

			// resize converted argument array if necessary
			if (outArgs == null || outArgs.Length != Parameters.Length) {
				outArgs = new object[Parameters.Length];
			}
			
			if (IsExtensionMethod) {
				// write 'this' as first argument
				outArgs[i++] = thisObj;
			}
			
			// process all parameters
			for (; i < ParameterCount; i++)
			{
				if (argIndex < argCount)
				{
					// write argument to output array (with conversion)
					outArgs[i] = PlayScript.Dynamic.ConvertValue(args[argIndex], Parameters[i].ParameterType);
					argIndex++;
				}
				else
				{
					// no more arguments left? use defaults
					if (Parameters[i].IsOptional) {
						outArgs[i] = Parameters[i].DefaultValue;
					} else {
						// error, not enough arguments supplied for method
						return false;
					}
				}
			}

			// compute remaining arguments
			int extraArgCount = (argCount - argIndex);

			if (IsVariadic)
			{
				// setup variadic arguments
				if (extraArgCount > 0)
				{
					// reallocate variadic array as needed
					object[] extraArgs = outArgs[i] as object[];
					if ((extraArgs == null) || (extraArgs.Length != extraArgCount))
					{
						outArgs[i] = extraArgs = new object[extraArgCount];
					}
					
					// copy variadic arguments
					Array.Copy(args, argIndex, extraArgs, 0, extraArgCount);
				}
				else
				{
					// use empty array
					outArgs[i] = sEmptyArray;
				}
			}
			else
			{
				if (extraArgCount > 0)
				{
					// we have too many arguments and this function is not variadic
					return false;
				}
			}
			
			return true;
		}
		
		
		public static MethodBinder[] BuildMethodList(Type type, string name, BindingFlags flags, int argCount)
		{
			var list = new List<MethodBinder>();
			var variadicList = new List<MethodBinder>();

			// rename toString to ToString for non-playscript objects
			if (name == "toString" && argCount == 0) {
				name = "ToString";
			}

			// get methods from main type
			if (type != null) {
				var methods = type.GetMethods(flags);
				foreach (var method in methods)	{
					if (method.Name == name) {
						var newInfo = new MethodBinder(method, false);
						if (newInfo.CheckArgumentCount(argCount)) {
							if (!newInfo.IsVariadic) {
								list.Add(newInfo);
							} else {
								variadicList.Add(newInfo);
							}
							sMethodInfoCache[method] = newInfo;
						}
					}
				}
			}

			if ((flags & BindingFlags.Static)==0) {
				// get extension methods
				var extType = PlayScript.Dynamic.GetExtensionClassForType(type);
				if (extType != null) {
					var methods = extType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
					foreach (var method in methods)	{
						if (method.Name == name) {
							// determine if this is a valid extension method for this object type
							var parameters = method.GetParameters();
							if (parameters.Length > 0){
								var thisType = parameters[0].ParameterType;
								if (thisType.IsAssignableFrom(type)) {
									var newInfo = new MethodBinder(method, true);
									if (newInfo.CheckArgumentCount(argCount)) {
										if (!newInfo.IsVariadic) {
											list.Add(newInfo);
										} else {
											variadicList.Add(newInfo);
										}
										sMethodInfoCache[method] = newInfo;
									}
								}
							}
						}
					}
				}
			}

			// see which variadic methods we want to keep
			// this is necessary in the case of overloads that have the same arguments but one is variadic and one is not
			// for example:
			//       push(o:Object);		
			//       push(o:Object, ...);
			// in this case we select the first method when only one argument is supplied
			foreach (var variadic in variadicList) {
				bool keepVariadic = true;

				// look at each non-variadic method
				// if any one matches this variadic method then dont keep it 
				foreach (var method in list) {
					if (!method.IsVariadic) {
						bool sameSignature = true;
						for (int i=0; i < argCount; i++) {
							if (variadic.Parameters[i].ParameterType != method.Parameters[i].ParameterType) {
								sameSignature = false;
							}
						}
						if (sameSignature) {
							// dont keep this variadic
							keepVariadic = false;
							break;
						}
					}
				}

				if (keepVariadic) {
					// add variadic to main list
					list.Add(variadic);
				}
			}

			// return list
			return list.ToArray();
		}


		public static MethodBinder[] LookupMethodList(Type type, string name, BindingFlags flags, int argCount)
		{
			MethodBinder[] list;

			// we cache based on the combination of type, name, flags and argcount
			var key = Tuple.Create<Type, string, BindingFlags, int>(type, name, flags, argCount);

			// try to get from cache
			if (!sMethodCache.TryGetValue(key, out list)) {
				list = BuildMethodList(type, name, flags, argCount);
				sMethodCache.Add(key, list);
			}
			return list;
		}

		private static readonly Dictionary< Tuple<Type, string, BindingFlags, int>, MethodBinder[] > sMethodCache = new Dictionary< Tuple<Type, string, BindingFlags, int>, MethodBinder[] >();
		private static readonly object[] 	   sEmptyArray = new object[0];

		// ActionCreator has already a MethodInfo cache, and most time they will match with these
		// We probably should move this to the ActionCreator and have one shared cache.
		private static Dictionary<MethodInfo, MethodBinder> sMethodInfoCache = new Dictionary<MethodInfo, MethodBinder>();
	}
}

