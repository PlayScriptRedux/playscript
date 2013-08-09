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

		enum OverloadState : byte
		{
			/// <summary>
			/// We don't know the overload state, either because this is the first pass through the binder, or we resolved a dynamic value every time.
			/// </summary>
			Unknown,
			/// <summary>
			/// We did a method resolve, and we found only one method. As long as the type is the same, we don't need to resolve again.
			/// </summary>
			NoOverload,
			/// <summary>
			/// We did a method resolve, and we found multiple methods. Everytime we are going to use the passed arguments to figure which method to use.
			/// This is slow but should be rarely used (and it is not used by AS code directly).
			/// </summary>
			HasOverload,
		}

		// information about the current binding
		private string			mName;
		private Type			mType;
		private object[]		mArgs;
		private OverloadState	mOverloadState = OverloadState.Unknown;
		private MethodBinder	mMethod;
		private MethodBinder[]	mMethodList;

		private InvokerBase		mInvoker;

		private InvokerBase SelectMethod(object o)
		{
			// if only one method, then use it
			if (mMethodList.Length == 1) {
				mMethod = mMethodList[0];
				mOverloadState = OverloadState.NoOverload;
				return InvokerBase.UpdateOrCreate(mInvoker, o, mMethod.Method);
			}

			mOverloadState = OverloadState.HasOverload;

			// select method based on simple compatibility
			// TODO: this could change to use a rating system
			foreach (var method in mMethodList) {
				// is this method compatible?
				if (method.CheckArguments(mArgs)) {
					mMethod = method;
					return InvokerBase.UpdateOrCreate(mInvoker, o, mMethod.Method);
				}
			}

			throw new InvalidOperationException("Could not find suitable method to invoke: " + mName + " for type: " + mType);
		}

		private bool UpdateInvoker(object o)
		{
			// We can't cache the delegate related to property (has it could have been changed since last call)
			// We could workaround this limitation if we did have version number in the dynamic object (and detect if it changed - or changed function - since last call)
			var dc = o as IDynamicClass;
			if (dc != null) {
				var func = dc.__GetDynamicValue(mName) as Delegate;
				if (func != null) {
					mInvoker = InvokerBase.UpdateOrCreate(mInvoker, func.Target, func.Method);
					return true;			// Because we found a dynamic value as an invoker, we have to invoke later and not resolve it again (either with the fast path or the slow path)
				}
			}

			if (mInvoker != null)
			{
				// Try to update the invoker with the new target, it might actually be the exact same target, so invoker can be re-used.
				mInvoker = mInvoker.TryUpdate(o);
				return (mInvoker != null);
			}

			// No invoker, or could not re-use it 
			return false;
		}

		/// <summary>
		/// Invokes the method on o (potentially using a previous invoker as a hint).
		/// 
		/// Note that this method works if the parameters haven been boxed, return value is also boxed.
		/// </summary>
		/// <param name="invoker">The previous invoker as a hint, null if we could not get the invoker during the fast path.</param>
		/// <param name="o">The target of the invocation.</param>
		private object ResolveAndInvoke(object o, bool invokeOnly)
		{
			// Property and same target, or same type has already been checked earlier (by the fast path)
			// So here we have a new target type, we have an overloading, or this is the first time
			// Parameters have been boxed and stored in mArgs already.

			// Note that if we are overloading, we still do a full CreateDelegate work, even if we had the same type, target and method...
			// This is slow, however it should pretty much never happen in game code.

			if ((mInvoker == null) && (invokeOnly == false))
			{
				// It means that we did not get the invoker from the fast path (overloaded - rare - or it is the first time - very common).
				var dc = o as IDynamicClass;
				if (dc != null) {
					var func = dc.__GetDynamicValue(mName) as Delegate;
					if (func != null) {
						// Assume that most time, it is due to the first execution, so don't compare with previous version
						mInvoker = ActionCreator.CreateInvoker(func);	// This is going to use an invoker factory (can be registered by user too for more optimal code).
						invokeOnly = true;
					}
				}
			}
			if (invokeOnly)
			{
				Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Slow);

				return mInvoker.SafeInvokeWith(mArgs);
			}

			// If we reached this point, we have to do a new resolve, then invoke

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
				mType = otype;

				// get method list for type and method name
				BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public;
				if (isStatic) {
					flags |= BindingFlags.Static;
				} else {
					flags |= BindingFlags.Instance;
				}

				mMethodList = MethodBinder.LookupMethodList(mType, mName, flags, mArgs.Length);

				// select new method to use, this will try to reuse 
				mInvoker = SelectMethod(o);
			}
			else if (mOverloadState == OverloadState.HasOverload)	
			{
				// if there are overloads we select the method every time
				// we could look into a more optimal way of doing this if it becomes a problem
				mInvoker = SelectMethod(o);
			}
			else
			{
				// Same instance type, no overload, so should be the same method (or none).
				// We might be able to update the invoker if only the target changed
				mInvoker = InvokerBase.UpdateOrCreate(mInvoker, o, mMethod.Method);
			}

			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Slow);

			return mInvoker.SafeInvokeWith(mArgs);
		}

		/// <summary>
		/// Invokes the method on o (potentially using a previous invoker as a hint).
		/// 
		/// Note that this method works if the parameters haven been boxed, return value is also boxed.
		/// </summary>
		/// <param name="invoker">The previous invoker as a hint, null if we could not get the invoker during the fast path.</param>
		/// <param name="o">The target of the invocation.</param>
		/// <typeparam name="TR">The 1st type parameter.</typeparam>
		private TR ResolveAndInvoke<TR>(object o, bool invokeOnly)
		{
			object value = ResolveAndInvoke(o, invokeOnly);
			if (value is TR) {
				return (TR)value;
			} else {
				return Dynamic.ConvertValue<TR>(value);
			}
		}

		public void InvokeAction0 (object o)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = UpdateInvoker(o);
			ICallerA caller = mInvoker as ICallerA;
			if (caller != null)
			{
				Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

				caller.Call();		// Fast path
				return;
			}

			ResolveAndInvoke(o, invokeOnly);
		}

		public void InvokeAction1<A1> (object o, A1 a1)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerA<A1> caller = mInvoker as ICallerA<A1>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					caller.Call(a1);	// Fast path
					return;
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			ResolveAndInvoke(o, invokeOnly);
		}

		public void InvokeAction2<A1,A2> (object o, A1 a1, A2 a2)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerA<A1, A2> caller = mInvoker as ICallerA<A1, A2>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					caller.Call(a1, a2);	// Fast path
					return;
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			args[1] = (object)a2;
			ResolveAndInvoke(o, invokeOnly);
		}

		public void InvokeAction3<A1,A2,A3> (object o, A1 a1, A2 a2, A3 a3)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerA<A1, A2, A3> caller = mInvoker as ICallerA<A1, A2, A3>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					caller.Call(a1, a2, a3);	// Fast path
					return;
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			args[1] = (object)a2;
			args[2] = (object)a3;
			ResolveAndInvoke(o, invokeOnly);
		}

		public void InvokeAction4<A1,A2,A3,A4> (object o, A1 a1, A2 a2, A3 a3, A4 a4)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerA<A1, A2, A3, A4> caller = mInvoker as ICallerA<A1, A2, A3, A4>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					caller.Call(a1, a2, a3, a4);	// Fast path
					return;
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			args[1] = (object)a2;
			args[2] = (object)a3;
			args[3] = (object)a4;
			ResolveAndInvoke(o, invokeOnly);
		}

		public void InvokeAction5<A1,A2,A3,A4,A5>(object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerA<A1, A2, A3, A4, A5> caller = mInvoker as ICallerA<A1, A2, A3, A4, A5>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					caller.Call(a1, a2, a3, a4, a5);	// Fast path
					return;
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			args[1] = (object)a2;
			args[2] = (object)a3;
			args[3] = (object)a4;
			args[4] = (object)a5;
			ResolveAndInvoke(o, invokeOnly);
		}

		public void InvokeAction6<A1,A2,A3,A4,A5,A6> (object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerA<A1, A2, A3, A4, A5, A6> caller = mInvoker as ICallerA<A1, A2, A3, A4, A5, A6>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					caller.Call(a1, a2, a3, a4, a5, a6);	// Fast path
					return;
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			args[1] = (object)a2;
			args[2] = (object)a3;
			args[3] = (object)a4;
			args[4] = (object)a5;
			args[5] = (object)a6;
			ResolveAndInvoke(o, invokeOnly);
		}

		public void InvokeAction7<A1,A2,A3,A4,A5,A6,A7> (object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerA<A1, A2, A3, A4, A5, A6, A7> caller = mInvoker as ICallerA<A1, A2, A3, A4, A5, A6, A7>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					caller.Call(a1, a2, a3, a4, a5, a6, a7);	// Fast path
					return;
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			args[1] = (object)a2;
			args[2] = (object)a3;
			args[3] = (object)a4;
			args[4] = (object)a5;
			args[5] = (object)a6;
			args[6] = (object)a7;
			ResolveAndInvoke(o, invokeOnly);
		}


		public void InvokeAction8<A1,A2,A3,A4,A5,A6,A7,A8> (object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7, A8 a8)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerA<A1, A2, A3, A4, A5, A6, A7, A8> caller = mInvoker as ICallerA<A1, A2, A3, A4, A5, A6, A7, A8>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					caller.Call(a1, a2, a3, a4, a5, a6, a7, a8);	// Fast path
					return;
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			args[1] = (object)a2;
			args[2] = (object)a3;
			args[3] = (object)a4;
			args[4] = (object)a5;
			args[5] = (object)a6;
			args[6] = (object)a7;
			args[7] = (object)a8;
			ResolveAndInvoke(o, invokeOnly);
		}

		public TR InvokeFunc0<TR>(object o)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerF<TR> caller = mInvoker as ICallerF<TR>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					return caller.Call();	// Fast path
				}
			}

			return ResolveAndInvoke<TR>(o, invokeOnly);
		}

		public TR InvokeFunc1<A1,TR>(object o, A1 a1)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerF<A1, TR> caller = mInvoker as ICallerF<A1, TR>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					return caller.Call(a1);	// Fast path
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			return ResolveAndInvoke<TR>(o, invokeOnly);
		}

		public TR InvokeFunc2<A1,A2,TR>(object o, A1 a1, A2 a2)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerF<A1, A2, TR> caller = mInvoker as ICallerF<A1, A2, TR>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					return caller.Call(a1, a2);	// Fast path
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			args[1] = (object)a2;
			return ResolveAndInvoke<TR>(o, invokeOnly);
		}

		public TR InvokeFunc3<A1,A2,A3,TR> (object o, A1 a1, A2 a2, A3 a3)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerF<A1, A2, A3, TR> caller = mInvoker as ICallerF<A1, A2, A3, TR>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					return caller.Call(a1, a2, a3);	// Fast path
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			args[1] = (object)a2;
			args[2] = (object)a3;
			return ResolveAndInvoke<TR>(o, invokeOnly);
		}

		public TR InvokeFunc4<A1,A2,A3,A4,TR> (object o, A1 a1, A2 a2, A3 a3, A4 a4)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerF<A1, A2, A3, A4, TR> caller = mInvoker as ICallerF<A1, A2, A3, A4, TR>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					return caller.Call(a1, a2, a3, a4);	// Fast path
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			args[1] = (object)a2;
			args[2] = (object)a3;
			args[3] = (object)a4;
			return ResolveAndInvoke<TR>(o, invokeOnly);
		}

		public TR InvokeFunc5<A1,A2,A3,A4,A5,TR>(object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerF<A1, A2, A3, A4, A5, TR> caller = mInvoker as ICallerF<A1, A2, A3, A4, A5, TR>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					return caller.Call(a1, a2, a3, a4, a5);	// Fast path
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			args[1] = (object)a2;
			args[2] = (object)a3;
			args[3] = (object)a4;
			args[4] = (object)a5;
			return ResolveAndInvoke<TR>(o, invokeOnly);
		}

		public TR InvokeFunc6<A1,A2,A3,A4,A5,A6,TR> (object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerF<A1, A2, A3, A4, A5, A6, TR> caller = mInvoker as ICallerF<A1, A2, A3, A4, A5, A6, TR>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					return caller.Call(a1, a2, a3, a4, a5, a6);	// Fast path
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			args[1] = (object)a2;
			args[2] = (object)a3;
			args[3] = (object)a4;
			args[4] = (object)a5;
			args[5] = (object)a6;
			return ResolveAndInvoke<TR>(o, invokeOnly);
		}

		public TR InvokeFunc7<A1,A2,A3,A4,A5,A6,A7,TR> (object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerF<A1, A2, A3, A4, A5, A6, A7, TR> caller = mInvoker as ICallerF<A1, A2, A3, A4, A5, A6, A7, TR>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					return caller.Call(a1, a2, a3, a4, a5, a6, a7);	// Fast path
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			args[1] = (object)a2;
			args[2] = (object)a3;
			args[3] = (object)a4;
			args[4] = (object)a5;
			args[5] = (object)a6;
			args[6] = (object)a7;
			return ResolveAndInvoke<TR>(o, invokeOnly);
		}


		public TR InvokeFunc8<A1,A2,A3,A4,A5,A6,A7,A8,TR> (object o, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7, A8 a8)
		{
			Stats.Increment(StatsCounter.InvokeMemberBinderInvoked);

			bool invokeOnly = false;
			if (mOverloadState != OverloadState.HasOverload)
			{
				invokeOnly = UpdateInvoker(o);
				ICallerF<A1, A2, A3, A4, A5, A6, A7, A8, TR> caller = mInvoker as ICallerF<A1, A2, A3, A4, A5, A6, A7, A8, TR>;
				if (caller != null)
				{
					Stats.Increment(StatsCounter.InvokeMemberBinderInvoked_Fast);

					return caller.Call(a1, a2, a3, a4, a5, a6, a7, a8);	// Fast path
				}
			}

			var args   = mArgs;
			args[0] = (object)a1;
			args[1] = (object)a2;
			args[2] = (object)a3;
			args[3] = (object)a4;
			args[4] = (object)a5;
			args[5] = (object)a6;
			args[6] = (object)a7;
			args[7] = (object)a8;
			return ResolveAndInvoke<TR>(o, invokeOnly);
		}
	}
}


#endif
