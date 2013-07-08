//
// Copyright 2013 Zynga Inc.
//	
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//		
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.

#if !DYNAMIC_SUPPORT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using PlayScript.Expando;
using System.Reflection;

namespace PlayScript.RuntimeBinder
{
	class PSInvokeMemberBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> invokeTargets = new Dictionary<Type, object>();
		private static Dictionary<Type, object> hasOwnPropertyInvokeTargets = new Dictionary<Type, object>();

//		readonly CSharpBinderFlags flags;
//		readonly IList<CSharpArgumentInfo> argumentInfo;
//		readonly IList<Type> typeArguments;
//		readonly Type callingContext;

		public const int MAX_ARGS = 8;

		readonly string name;
		public PSInvokeMemberBinder (CSharpBinderFlags flags, string name, Type callingContext, IEnumerable<Type> typeArguments, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			this.name = name;

			var argList = argumentInfo as System.Collections.IList;
			if (argList != null)
			{
				// allocate argument list
				this.mArgs = new object[argList.Count];
			}
			else
			{
				this.mArgs = new object[MAX_ARGS];
			}

//			this.flags = flags;
//			this.callingContext = callingContext;
//			this.argumentInfo = new List<CSharpArgumentInfo>(argumentInfo);
//			if (typeArguments != null) this.typeArguments = new List<Type>(typeArguments);
		}

		[Flags]
		enum InvokeFlags
		{
			None = 0,
			IsOverloaded = (1 << 0),
			HasDefaults  = (1 << 1),
			IsVariadic  = (1 << 2),
			HasConversions  = (1 << 3),
			ExtensionMethod = (1 << 4)
		};

		
		class MethodInvokeInfo
		{
			public readonly MethodInfo  		method;
			public readonly InvokeFlags 		flags;
			public readonly Type[] 				paramTypes;
			public readonly int        			paramCount;
			public readonly object[]    		defaults;
			public readonly int         		defaultsIndex;
			public readonly int         		variadicIndex;
			public MethodInvokeInfo 			next;		// linked list
			
			public MethodInvokeInfo(MethodInfo method, bool isExtensionMethod)
			{
				this.method = method;

				if (isExtensionMethod)
				{
					this.flags |= InvokeFlags.ExtensionMethod;
				}

				var ps = method.GetParameters();
				paramCount = ps.Length;
				paramTypes = new Type[paramCount];

				for (int i=0 ; i < ps.Length; i++)
				{
					if (i == (ps.Length -1))
					{
					// determine variadic state of this method
						var paramArrayAttribute = ps[i].GetCustomAttributes(typeof(ParamArrayAttribute), true);
						if ((paramArrayAttribute != null) && (paramArrayAttribute.Length != 0))
						{
							flags |= InvokeFlags.IsVariadic;
							variadicIndex = i;
						}
					}

					// build type array
					paramTypes[i] = ps[i].ParameterType;

					// dont convert things that target system object
					if (paramTypes[i] == typeof(System.Object)) {
						paramTypes[i] = null;
					}

					// handle default parameters
					if (ps[i].IsOptional)
					{
						if (defaults == null) {
							defaults = new object[paramTypes.Length];
							defaultsIndex = i;
						}

						flags |= InvokeFlags.HasDefaults;
						defaults[i]   = ps[i].DefaultValue;
					}
				}

			}

			public bool ConvertArguments(object thisObj, object[] args, int argCount, object[] outArgs)
			{
				if ((flags & InvokeFlags.ExtensionMethod) != 0) {
					throw new NotImplementedException("Extension methods not supported yet");
				}

				if ((flags & InvokeFlags.IsVariadic) != 0) {
					throw new NotImplementedException("Variadic not supported yet");
				}

				// convert arguments
				int i=0;
				for (; i < argCount; i++)
				{
					var targetType = paramTypes[i];
					object arg = args[i];
					outArgs[i] = arg;
					if (arg != null)
					{
						Type argType = arg.GetType();
						if (targetType != null && targetType != argType) {
							if (!targetType.IsAssignableFrom(argType)) {
								outArgs[i] = System.Convert.ChangeType(arg, targetType);
							}
						} 
					}
				}

				// set defaults
				if (defaults != null) {
					if (i < defaultsIndex) {
						throw new NotImplementedException("not enough default parameters! TODO");
					}

					// set default values
					for (; i < defaults.Length; i++)
					{
						outArgs[i] = defaults[i];
					}
				}
				return true;

			}
		}

		// information about the current binding
		private Type              mType;
		private object[]   		  mArgs;
		private object[]   		  mConvertedArgs;
		private bool 			  mIsStatic;
		private bool 			  mIsOverloaded;
		private MethodInvokeInfo  mMethod;
		private MethodInvokeInfo  mMethodList;

		private static MethodInvokeInfo BuildMethodList(System.Type otype, string name)
		{
			MethodInvokeInfo head = null;
			MethodInvokeInfo tail = null;

			var types = new Type[2] {otype, PlayScript.Dynamic.GetExtensionClassForType(otype)};
			// add all methods from types
			foreach (Type type in types) {
				if (type != null) {
					var methods = type.GetMethods();
					foreach (var method in methods)
					{
						if (method.Name == name)
						{
							var newInfo = new MethodInvokeInfo(method, (type!=otype));
							if (tail != null) tail.next = newInfo;
							if (head == null) head = newInfo;
						}
					}
				}
			}

			// return head of list
			return head;
		}

		private static MethodInvokeInfo LookupMethodList(System.Type otype, string name)
		{
			// TODO: use a cache!
			return BuildMethodList(otype, name);
		}

		private static MethodInvokeInfo FindBestMethod(MethodInvokeInfo list, object thisObj, object[] args, int argCount)
		{
			// no overloads!
			throw new NotImplementedException("Overloads are currently not supported");
		}

		private void ResolveMethod(CallSite site, object o, int argCount)
		{
			if (o is System.Type) {
				// this is a static method invocation where o is the class
				mIsStatic = true;
				mType = (System.Type)o;
			} else {
				// this is a non-static method invocation
				mIsStatic = false;
				mType = o.GetType();
			}

			// lookup methods for type
			mMethodList = LookupMethodList(mType, this.name);
			if (mMethodList == null)
			{
				if (o is IDynamicClass) {
					var func = ((IDynamicClass)o).__GetDynamicValue(name);
					if (func != null) {
						throw new NotImplementedException("Functions stored in dynamic objects not supported yet");
					}
				}

				throw new Exception("Method not found with name: " + this.name + " for type: " + mType.ToString());
			} else if (mMethodList.next != null) {
				this.mIsOverloaded = true;
				this.mMethod = FindBestMethod(mMethodList, o, mArgs, argCount);
			} else {
				// use first method
				this.mIsOverloaded = false;
				this.mMethod = this.mMethodList;
			}

			// resize coverted argument array if necessary
			if (this.mConvertedArgs == null || this.mConvertedArgs.Length != mMethod.paramCount) {
				this.mConvertedArgs = new object[mMethod.paramCount];
			}
		}

		private object ResolveAndInvoke(CallSite site, object o, int argCount)
		{
			// get type for binding comparison
			System.Type otype;
			if (mIsStatic)  {
				otype = o as Type;
			} else { 
				otype = o.GetType();
			}

			// see if type has changed
			if (otype != this.mType)
			{
				// re-resolve method
				ResolveMethod(site, o, argCount);
			}

			// convert arguments
			if (!mMethod.ConvertArguments(o, mArgs, argCount, mConvertedArgs)) {
				if (mIsOverloaded) {
					// uh oh, have to overload? or resolve again?
					throw new NotImplementedException("Overloads not supported right now");
				} else {
					throw new NotImplementedException("Could not convert argumetns");
				}
			}

			// invoke method with converted arguments
			return mMethod.method.Invoke(o, this.mConvertedArgs);
		}

		private static void InvokeAction0 (CallSite site, object o)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			binder.ResolveAndInvoke(site, o, 0);
		}

		private static void InvokeAction1 (CallSite site, object o, object a1)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			binder.ResolveAndInvoke(site, o, 1);
		}

		private static void InvokeAction2 (CallSite site, object o, object a1, object a2)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			binder.ResolveAndInvoke(site, o, 2);
		}

		private static void InvokeAction3 (CallSite site, object o, object a1, object a2, object a3)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			binder.ResolveAndInvoke(site, o, 3);
		}

		private static void InvokeAction4 (CallSite site, object o, object a1, object a2, object a3, object a4)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			binder.ResolveAndInvoke(site, o, 4);
		}

		private static void InvokeAction5 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			binder.ResolveAndInvoke(site, o, 5);
		}

		private static void InvokeAction6 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5, object a6)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			args[5] = a6;
			binder.ResolveAndInvoke(site, o, 6);
		}

		private static void InvokeAction7 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5, object a6, object a7)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			args[5] = a6;
			args[6] = a7;
			binder.ResolveAndInvoke(site, o, 7);
		}

		
		private static void InvokeAction8 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5, object a6, object a7, object a8)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			args[5] = a6;
			args[6] = a7;
			args[7] = a8;
			binder.ResolveAndInvoke(site, o, 8);
		}

		private static object InvokeFunc0 (CallSite site, object o)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			return binder.ResolveAndInvoke(site, o, 0);
		}
		
		private static object InvokeFunc1 (CallSite site, object o, object a1)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			return binder.ResolveAndInvoke(site, o, 1);
		}
		
		private static object InvokeFunc2 (CallSite site, object o, object a1, object a2)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			return binder.ResolveAndInvoke(site, o, 2);
		}
		
		private static object InvokeFunc3 (CallSite site, object o, object a1, object a2, object a3)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			return binder.ResolveAndInvoke(site, o, 3);
		}
		
		private static object InvokeFunc4 (CallSite site, object o, object a1, object a2, object a3, object a4)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			return binder.ResolveAndInvoke(site, o, 4);
		}

		
		private static object InvokeFunc5 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			return binder.ResolveAndInvoke(site, o, 5);
		}
		
		private static object InvokeFunc6 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5, object a6)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			args[5] = a6;
			return binder.ResolveAndInvoke(site, o, 6);
		}
		
		private static object InvokeFunc7 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5, object a6, object a7)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			args[5] = a6;
			args[6] = a7;
			return binder.ResolveAndInvoke(site, o, 7);
		}
		
		private static object InvokeFunc8 (CallSite site, object o, object a1, object a2, object a3, object a4, object a5, object a6, object a7, object a8)
		{
			var binder = (PSInvokeMemberBinder)site.Binder;
			var args   = binder.mArgs;
			args[0] = a1;
			args[1] = a2;
			args[2] = a3;
			args[3] = a4;
			args[4] = a5;
			args[5] = a6;
			args[6] = a7;
			args[7] = a8;
			return binder.ResolveAndInvoke(site, o, 8);
		}


		// minor optimization for has own propety
		private static object InvokeFunc1_hasOwnProperty(CallSite site, object o, object a1)
		{
			if (o == null || a1 == null) return false;

			var name = a1 as string;
			if (name != null) {
				return PlayScript.Dynamic.HasOwnProperty(o, name);
			}

			// fallback
			return InvokeFunc1(site, o, a1);
		}

		
		static PSInvokeMemberBinder ()
		{
			invokeTargets.Add (typeof(Action<CallSite,object>), 
			                   (Action<CallSite,object>)InvokeAction0);
			invokeTargets.Add (typeof(Action<CallSite,object,object>), 
			                   (Action<CallSite,object,object>)InvokeAction1);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object>), 
			                   (Action<CallSite,object,object,object>)InvokeAction2);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object>)InvokeAction3);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object,object>)InvokeAction4);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object,object,object>)InvokeAction5);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object,object,object,object>)InvokeAction6);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object,object,object,object,object>)InvokeAction7);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object,object,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object,object,object,object,object,object>)InvokeAction8);
			invokeTargets.Add (typeof(Func<CallSite,object,object>), 
			                   (Func<CallSite,object,object>)InvokeFunc0);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object>), 
			                   (Func<CallSite,object,object,object>)InvokeFunc1);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object>)InvokeFunc2);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object>)InvokeFunc3);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object,object>)InvokeFunc4);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object,object,object>),
			                   (Func<CallSite,object,object,object,object,object,object,object>)InvokeFunc5);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object,object,object,object>)InvokeFunc6);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object,object,object,object,object>)InvokeFunc7);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object,object,object,object,object,object>)InvokeFunc8);

			hasOwnPropertyInvokeTargets.Add (typeof(Func<CallSite,object,object,object>), 
			                   (Func<CallSite,object,object,object>)InvokeFunc1_hasOwnProperty);

		}

		public override object Bind (Type delegateType)
		{
			object target;
			// special case optimization here
			if (this.name == "hasOwnProperty") {
				if (hasOwnPropertyInvokeTargets.TryGetValue(delegateType, out target))
				{
					return target;
				}
			} 

			invokeTargets.TryGetValue(delegateType, out target);
			return target;
		}
	}
}


#endif