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

namespace PlayScript.DynamicRuntime
{
	public class PSInvokeMember
	{
		public const int MAX_ARGS = 8;

		public PSInvokeMember(string name, int argCount)
		{
			mName   = name;
			mArgs   = new object[argCount];
		}

		// information about the current binding
		private string 			  mName;
		private Type              mType;
		private object[]   		  mArgs;
		private object[]   		  mConvertedArgs;
		private bool 			  mIsOverloaded;
		private MethodBinder      mMethod;
		private MethodBinder[]    mMethodList;
		private Delegate   	      mDelegate;


		private void SelectMethod(object o)
		{
			mDelegate = null;

			// if only one method, then use it
			if (mMethodList.Length == 1) {
				mMethod       = mMethodList[0];
				mIsOverloaded = false;
				return;
			}

			mIsOverloaded = true;

			// select method based on simple compatibility
			// TODO: this could change to use a rating system
			foreach (var method in mMethodList) {
				// is this method compatible?
				if (method.CheckArguments(mArgs)) {
					mMethod = method;
					return;
				}
			}

			// no methods compatible?

			// try to get method from dynamic class
			var dc = o as IDynamicClass;
			if (dc != null) {
				var func = dc.__GetDynamicValue(mName) as Delegate;
				if (func != null) {
					// use function
					mDelegate = func;
					return;
				}
			}

			throw new InvalidOperationException("Could not find suitable method to invoke: " + mName + " for type: " + mType);
		}

		private object ResolveAndInvoke(object o)
		{
			// determine object type
			Type otype;
			bool isStatic;
			if (o is Type) {
				// this is a static method invocation where o is the class
				otype = (Type)o;
				isStatic = true;
			} else {
				// this is a instance method invocation
				otype = o.GetType();
				isStatic = false;
			}

			// see if type has changed
			if (otype != mType)
			{
				// re-resolve method list if type has changed
				mType           = otype;

				// get method list for type and method name
				BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public;
				if (isStatic) {
					flags|=BindingFlags.Static;
				} else {
					flags|=BindingFlags.Instance;
				}

				mMethodList     = MethodBinder.LookupMethodList(mType, mName, flags, mArgs.Length);

				// select new method to use
				SelectMethod(o);
			}
			else if (mIsOverloaded)	
			{
				// if there are overloads we select the method every time
				// we could look into a more optimal way of doing this if it becomes a problem
				SelectMethod(o);
			}

			if (mDelegate == null)
			{
				// invoke as method
				// convert arguments for method
				if (mMethod.ConvertArguments(o, mArgs, mArgs.Length, ref mConvertedArgs)) {
					// invoke method with converted arguments
					return mMethod.Method.Invoke(o, mConvertedArgs);
				}
			}
			else
			{
				// invoke as delegate
				// convert arguments for delegate and invoke
				object[] newargs = PlayScript.Dynamic.ConvertArgumentList(mDelegate.Method, mArgs);
				return mDelegate.DynamicInvoke(newargs);
			}

			throw new InvalidOperationException("Could not find suitable method to invoke: " + mName + " for type: " + mType);
		}

		private TR ResolveAndInvokeAndConvert<TR>(object o)
		{
			object value = ResolveAndInvoke(o);
			if (value is TR) {
				return (TR)value;
			} else {
				return Dynamic.ConvertValue<TR>(value);
			}
		}


	public void InvokeAction0 (object o)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		ResolveAndInvoke(o);
	}

	public void InvokeAction1<A1> (object o, A1 a1)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		ResolveAndInvoke(o);
	}

	public void InvokeAction2<A1,A2> (object o, A1 a1, A2 a2)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		args[1] = (object)a2;
		ResolveAndInvoke(o);
	}

	public void InvokeAction3<A1,A2,A3> (object o, A1 a1, A2 a2, A3 a3)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		args[1] = (object)a2;
		args[2] = (object)a3;
		ResolveAndInvoke(o);
	}

	public void InvokeAction4<A1,A2,A3,A4> (object o, A1 a1, A2 a2, A3 a3, A4 a4)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		args[1] = (object)a2;
		args[2] = (object)a3;
		args[3] = (object)a4;
		ResolveAndInvoke(o);
	}

	public void InvokeAction5<A1,A2,A3,A4,A5>(object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		args[1] = (object)a2;
		args[2] = (object)a3;
		args[3] = (object)a4;
		args[4] = (object)a5;
		ResolveAndInvoke(o);
	}

	public void InvokeAction6<A1,A2,A3,A4,A5,A6> (object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		args[1] = (object)a2;
		args[2] = (object)a3;
		args[3] = (object)a4;
		args[4] = (object)a5;
		args[5] = (object)a6;
		ResolveAndInvoke(o);
	}

	public void InvokeAction7<A1,A2,A3,A4,A5,A6,A7> (object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		args[1] = (object)a2;
		args[2] = (object)a3;
		args[3] = (object)a4;
		args[4] = (object)a5;
		args[5] = (object)a6;
		args[6] = (object)a7;
		ResolveAndInvoke(o);
	}


	public void InvokeAction8<A1,A2,A3,A4,A5,A6,A7,A8> (object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7, A8 a8)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		args[1] = (object)a2;
		args[2] = (object)a3;
		args[3] = (object)a4;
		args[4] = (object)a5;
		args[5] = (object)a6;
		args[6] = (object)a7;
		args[7] = (object)a8;
		ResolveAndInvoke(o);
	}

	public TR InvokeFunc0<TR>(object o)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		return ResolveAndInvokeAndConvert<TR>(o);
	}

	public TR InvokeFunc1<A1,TR>(object o, A1 a1)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		return ResolveAndInvokeAndConvert<TR>(o);
	}

	public TR InvokeFunc2<A1,A2,TR>(object o, A1 a1, A2 a2)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		args[1] = (object)a2;
		return ResolveAndInvokeAndConvert<TR>(o);
	}

	public TR InvokeFunc3<A1,A2,A3,TR> (object o, A1 a1, A2 a2, A3 a3)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		args[1] = (object)a2;
		args[2] = (object)a3;
		return ResolveAndInvokeAndConvert<TR>(o);
	}

	public TR InvokeFunc4<A1,A2,A3,A4,TR> (object o, A1 a1, A2 a2, A3 a3, A4 a4)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		args[1] = (object)a2;
		args[2] = (object)a3;
		args[3] = (object)a4;
		return ResolveAndInvokeAndConvert<TR>(o);
	}

	public TR InvokeFunc5<A1,A2,A3,A4,A5,TR>(object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		args[1] = (object)a2;
		args[2] = (object)a3;
		args[3] = (object)a4;
		args[4] = (object)a5;
		return ResolveAndInvokeAndConvert<TR>(o);
	}

	public TR InvokeFunc6<A1,A2,A3,A4,A5,A6,TR> (object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		args[1] = (object)a2;
		args[2] = (object)a3;
		args[3] = (object)a4;
		args[4] = (object)a5;
		args[5] = (object)a6;
		return ResolveAndInvokeAndConvert<TR>(o);
	}

	public TR InvokeFunc7<A1,A2,A3,A4,A5,A6,A7,TR> (object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		args[1] = (object)a2;
		args[2] = (object)a3;
		args[3] = (object)a4;
		args[4] = (object)a5;
		args[5] = (object)a6;
		args[6] = (object)a7;
		return ResolveAndInvokeAndConvert<TR>(o);
	}


	public TR InvokeFunc8<A1,A2,A3,A4,A5,A6,A7,A8,TR> (object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7, A8 a8)
	{
		#if BINDERS_RUNTIME_STATS
		++Stats.CurrentInstance.InvokeMemberBinderInvoked;
		#endif
		var args   = mArgs;
		args[0] = (object)a1;
		args[1] = (object)a2;
		args[2] = (object)a3;
		args[3] = (object)a4;
		args[4] = (object)a5;
		args[5] = (object)a6;
		args[6] = (object)a7;
		args[7] = (object)a8;
		return ResolveAndInvokeAndConvert<TR>(o);
	}
}

}


#endif