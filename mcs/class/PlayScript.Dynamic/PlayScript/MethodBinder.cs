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
			this.Method = method;
			this.IsExtensionMethod = isExtensionMethod;
			
			// get method parameters
			this.Parameters = method.GetParameters();
			this.ParameterCount = this.Parameters.Length;

			// see if this method is variadic
			if (this.Parameters.Length > 0)
			{
				var lastParameter = this.Parameters[this.Parameters.Length - 1];
				// determine variadic state of this method
				var paramArrayAttribute = lastParameter.GetCustomAttributes(typeof(ParamArrayAttribute), true);
				if ((paramArrayAttribute != null) && (paramArrayAttribute.Length != 0))
				{
					IsVariadic = true;
					// we have one less parameter since we are variadic
					this.ParameterCount--;
				}
			}

			// determine required argument count
			this.MinArgumentCount = 0;
			for (int i=0; i < this.ParameterCount; i++) {
				if (this.Parameters[i].IsOptional) {
					break;
				}
				MinArgumentCount++;
			}
			if (this.IsExtensionMethod) {
				MinArgumentCount--;
			}

			this.MaxArgumentCount = !this.IsVariadic ? this.ParameterCount : int.MaxValue;
		}

		public bool CheckArguments(object[] args) {
			int startParameter = this.IsExtensionMethod ? 1 : 0;
			// check required arguments
			for (int i=0; i < this.MinArgumentCount; i++)
			{
				object arg = args[i];
				if (arg != null)
				{
					Type argType = arg.GetType();
					Type paramType = this.Parameters[startParameter + i].ParameterType;
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
			return (argCount >= this.MinArgumentCount) && (argCount <= this.MaxArgumentCount);
		}
		
		public bool ConvertArguments(object thisObj, object[] args, int argCount, ref object[] outArgs)
		{
			// index in parameters (outArgs)
			int i = 0;
			// index in arguments (args)
			int argIndex = 0;

			// resize converted argument array if necessary
			if (outArgs == null || outArgs.Length != this.Parameters.Length) {
				outArgs = new object[this.Parameters.Length];
			}
			
			if (this.IsExtensionMethod) {
				// write 'this' as first argument
				outArgs[i++] = thisObj;
			}
			
			// process all parameters
			for (; i < this.ParameterCount; i++)
			{
				if (argIndex < argCount)
				{
					// write argument to output array (with conversion)
					outArgs[i] = PlayScript.Dynamic.ConvertValue(args[argIndex], this.Parameters[i].ParameterType);
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

			if (this.IsVariadic)
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
//				if (extraArgCount > 0)
//				{
//					// we have too many arguments and this function is not variadic
//					return false;
//				}
			}
			
			return true;
		}
		
		
		public static MethodBinder[] BuildMethodList(Type type, string name, BindingFlags flags, int argCount)
		{
			var list = new List<MethodBinder>();

			// get methods from main type
			if (type != null) {
				var methods = type.GetMethods(flags);
				foreach (var method in methods)	{
					if (method.Name == name) {
						var newInfo = new MethodBinder(method, false);
						if (newInfo.CheckArgumentCount(argCount)) {
							list.Add(newInfo);
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
										list.Add(newInfo);
									}
								}
							}
						}
					}
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
	}
}

