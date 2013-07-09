using System;
using System.Reflection;
using System.Collections.Generic;

namespace PlayScript
{
	public class MethodBinder
	{
		public readonly MethodInfo  		Method;
		public readonly ParameterInfo[] 	Parameters;
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
		}

		public bool IsCompatible(object thisObj, object[] args, int argCount) {

			int i = 0;
			int argIndex = 0;

			if (this.IsExtensionMethod) {
				// write 'this' as first argument
				Type paramType = this.Parameters[i].ParameterType;
				if (!paramType.IsAssignableFrom(thisObj.GetType())) {
					return false;
				}
				i++;
			}
			
			
			// process all parameters
			for (; i < this.ParameterCount; i++)
			{
				if (argIndex < argCount)
				{
					object arg = args[argIndex++];
					if (arg != null)
					{
						Type argType = arg.GetType();
						Type paramType = this.Parameters[i].ParameterType;
						if (!paramType.IsAssignableFrom(argType)) {
							// not compatible
							return false;
						}
					}
				}
				else
				{
					// no more arguments left? use defaults
					if (!Parameters[i].IsOptional) {
						// not enough arguments supplied for method
						return false;
					}
				}
			}

			// compute leftover arguments
			int extraArgCount = (argCount - argIndex);
			if ((extraArgCount > 0) && !this.IsVariadic)
			{
				// its not okay to have extra arguments if we're not variadic
				return false;
			}
			
			// compatible!
			return true;
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
		
		
		public static MethodBinder[] BuildList(System.Type otype, string name, bool isStatic)
		{
			List<MethodBinder> list = null;

			// get methods from main type
			if (otype != null) {
				var methods = otype.GetMethods();
				foreach (var method in methods)
				{
					if ((method.Name == name) && (method.IsStatic == isStatic))
					{
						var newInfo = new MethodBinder(method, false);
						if (list == null) {
							list = new List<MethodBinder>();
						}
						list.Add(newInfo);
					}
				}
			}

			if (!isStatic) {
				// get extension methods
				var extType = PlayScript.Dynamic.GetExtensionClassForType(otype);
				if (extType != null) {
					var methods = extType.GetMethods();
					foreach (var method in methods)
					{
						if (method.Name == name && method.IsStatic)
						{
							// determine if this is a valid extension method for this object type
							var parameters = method.GetParameters();
							if (parameters.Length > 1)
							{
								var thisType = parameters[0].ParameterType;
								if (thisType.IsAssignableFrom(otype)) {
									var newInfo = new MethodBinder(method, true);
									if (list == null) {
										list = new List<MethodBinder>();
									}
									list.Add(newInfo);
								}
							}
						}
					}
				}
			}

			// return list
			return (list!=null) ? list.ToArray() : sEmptyList;
		}
		
		public static MethodBinder[] LookupList(System.Type otype, string name, bool isStatic)
		{
			// TODO: use a cache!
			return BuildList(otype, name, isStatic);
		}

		private static readonly object[] 	   sEmptyArray = new object[0];
		private static readonly MethodBinder[] sEmptyList = new MethodBinder[0];
		
	}
}

