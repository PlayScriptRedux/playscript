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


		// information about the current binding
		private Type              mType;
		private object[]   		  mArgs;
		private object[]   		  mConvertedArgs;
		private bool 			  mIsStatic;
		private bool 			  mIsOverloaded;
		private MethodBinder      mMethod;
		private MethodBinder[]    mMethodList;


		private MethodBinder SelectCompatibleMethod(MethodBinder[] list, object thisObj, object[] args, int argCount)
		{
			// select method based on simple compatibility
			// TODO: this could change to use a rating system
			foreach (var method in list) {
				// is this method compatible?
				if (method.IsCompatible(thisObj, args, argCount)) {
					return method;
				}
			}

			// no methods compatible?
			var dc = thisObj as IDynamicClass;
			if (dc != null) {
				var func = (object)dc.__GetDynamicValue(this.name);
				if (func != null) {
					throw new NotImplementedException("Delegates cant be invoked from dynamic properties (yet)");
				}
			}

			throw new InvalidOperationException("Could not find suitable method to invoke: " + this.name + " for type: " + mType);
		}

		private object ResolveAndInvoke(CallSite site, object o, int argCount)
		{
			// determine object type
			Type otype;
			if (o is Type) {
				// this is a static method invocation where o is the class
				otype = (Type)o;
			} else {
				// this is a instance method invocation
				otype = o.GetType();
			}

			// see if type has changed
			if (otype != this.mType)
			{
				// re-resolve method list if type has changed
				mType           = otype;
				mIsStatic       = (o is Type);
				// get method list for type and method name
				mMethodList     = MethodBinder.LookupList(mType, this.name, mIsStatic);
				if (mMethodList.Length == 0)
				{
					throw new InvalidOperationException("Could not find suitable method to invoke: " + this.name + " for type: " + mType);
				}

				if (mMethodList.Length == 1)
				{
					// no overloading
					mMethod       = mMethodList[0];
					mIsOverloaded = false;
				}
				else
				{
					// more than one method
					mIsOverloaded = true;
				}
			}

			// if there are overloads we resolve the method every time
			// we could look into a more optimal way of doing this if it becomes a problem
			if (mIsOverloaded)	{
				mMethod = SelectCompatibleMethod(mMethodList, o, mArgs, argCount);
			}

			// convert arguments for method
			if (mMethod.ConvertArguments(o, mArgs, argCount, ref mConvertedArgs)) {
				// invoke method with converted arguments
				return mMethod.Method.Invoke(o, mConvertedArgs);
			}

			throw new InvalidOperationException("Could not find suitable method to invoke: " + this.name + " for type: " + mType);
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