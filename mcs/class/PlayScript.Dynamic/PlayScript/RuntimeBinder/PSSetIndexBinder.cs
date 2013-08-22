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
using System.Collections;
using System.Collections.Generic;
using PlayScript;

namespace PlayScript.RuntimeBinder
{
	class PSSetIndexBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> delegates = new Dictionary<Type, object>();

		private CallSite 	      mSetMemberCallSite;

		private static void SetIndex<T> (CallSite site, object o, int index, T value)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.SetIndexBinderInvoked);
			Stats.Increment(StatsCounter.SetIndexBinder_Int_Invoked);
#endif
			var l = o as IList<T>;
			if (l != null) {
				l [index] = value;
				return;
			} 


			var l2 = o as IList;
			if (l2 != null) {
				l2 [index] = value;
				return;
			} 

			var d = o as IDictionary<int,T>;
			if (d != null) {
				d[index] = value;
				return;
			} 

			var d2 = o as IDictionary;
			if (d2 != null) {
				d2[index] = value;
				return;
			}
		}

		private static void SetIndexUnsigned<T> (CallSite site, object o, uint index, T value)
		{
			SetIndex<T>(site, o, (int)index, value);
		}

		private static void SetIndexDouble<T> (CallSite site, object o, double index, T value)
		{
			SetIndex<T>(site, o, (int)index, value);
		}

		private static void SetKeyStr<T> (CallSite site, object o, string key, T value)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.SetIndexBinderInvoked);
			Stats.Increment(StatsCounter.SetIndexBinder_Key_Invoked);
#endif
			// handle dictionaries
			var dict = o as IDictionary;
			if (dict != null) {
#if BINDERS_RUNTIME_STATS
				Stats.Increment(StatsCounter.SetIndexBinder_Key_Dictionary_Invoked);
#endif
				dict[key] = (object)value;
				return;
			} 

			// fallback on setmemberbinder to do the hard work 
			var binder = site.Binder as PSSetIndexBinder;

#if BINDERS_RUNTIME_STATS
				Stats.Increment(StatsCounter.SetIndexBinder_Key_Property_Invoked);
#endif

			// create a set member binder here to set
			if (binder.mSetMemberCallSite == null) {
				var setMemberBinder   = new PSSetMemberBinder(0, key, null, null);
				binder.mSetMemberCallSite = CallSite< Action<CallSite, object, T> >.Create(setMemberBinder);
			}

			// set member value
			PSSetMemberBinder.SetValue<T>(binder.mSetMemberCallSite, o, key, value);			
		}



		private static void SetKeyObject<T> (CallSite site, object o, object key, T value)
		{
			if (key is int) {
				SetIndex<T>(site, o, (int)key, value);
			} else if (key is string) {
				SetKeyStr<T>(site, o, (string)key, value);
			} else  if (key is uint) {
				SetIndexUnsigned<T>(site, o, (uint)key, value);
			} else  if (key is double) {
				SetIndexDouble<T>(site, o, (double)key, value);
			} else {
				throw new InvalidOperationException("Cannot index object with key of type: " + key.GetType());
			}
		}

		public PSSetIndexBinder (CSharpBinderFlags flags, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
		}

		static PSSetIndexBinder ()
		{
			delegates.Add (typeof(Action<CallSite, object, int, int>),    (Action<CallSite, object, int, int>)SetIndex<int>);
			delegates.Add (typeof(Action<CallSite, object, int, uint>),   (Action<CallSite, object, int, uint>)SetIndex<uint>);
			delegates.Add (typeof(Action<CallSite, object, int, double>), (Action<CallSite, object, int, double>)SetIndex<double>);
			delegates.Add (typeof(Action<CallSite, object, int, bool>),   (Action<CallSite, object, int, bool>)SetIndex<bool>);
			delegates.Add (typeof(Action<CallSite, object, int, string>), (Action<CallSite, object, int, string>)SetIndex<string>);
			delegates.Add (typeof(Action<CallSite, object, int, object>), (Action<CallSite, object, int, object>)SetIndex<object>);

			delegates.Add (typeof(Action<CallSite, object, uint, int>),    (Action<CallSite, object, uint, int>)SetIndexUnsigned<int>);
			delegates.Add (typeof(Action<CallSite, object, uint, uint>),   (Action<CallSite, object, uint, uint>)SetIndexUnsigned<uint>);
			delegates.Add (typeof(Action<CallSite, object, uint, double>), (Action<CallSite, object, uint, double>)SetIndexUnsigned<double>);
			delegates.Add (typeof(Action<CallSite, object, uint, bool>),   (Action<CallSite, object, uint, bool>)SetIndexUnsigned<bool>);
			delegates.Add (typeof(Action<CallSite, object, uint, string>), (Action<CallSite, object, uint, string>)SetIndexUnsigned<string>);
			delegates.Add (typeof(Action<CallSite, object, uint, object>), (Action<CallSite, object, uint, object>)SetIndexUnsigned<object>);

			delegates.Add (typeof(Action<CallSite, object, double, int>),    (Action<CallSite, object, double, int>)SetIndexDouble<int>);
			delegates.Add (typeof(Action<CallSite, object, double, uint>),   (Action<CallSite, object, double, uint>)SetIndexDouble<uint>);
			delegates.Add (typeof(Action<CallSite, object, double, double>), (Action<CallSite, object, double, double>)SetIndexDouble<double>);
			delegates.Add (typeof(Action<CallSite, object, double, bool>),   (Action<CallSite, object, double, bool>)SetIndexDouble<bool>);
			delegates.Add (typeof(Action<CallSite, object, double, string>), (Action<CallSite, object, double, string>)SetIndexDouble<string>);
			delegates.Add (typeof(Action<CallSite, object, double, object>), (Action<CallSite, object, double, object>)SetIndexDouble<object>);

			delegates.Add (typeof(Action<CallSite, object, string, int>),    (Action<CallSite, object, string, int>)SetKeyStr<int>);
			delegates.Add (typeof(Action<CallSite, object, string, uint>),   (Action<CallSite, object, string, uint>)SetKeyStr<uint>);
			delegates.Add (typeof(Action<CallSite, object, string, double>), (Action<CallSite, object, string, double>)SetKeyStr<double>);
			delegates.Add (typeof(Action<CallSite, object, string, bool>),   (Action<CallSite, object, string, bool>)SetKeyStr<bool>);
			delegates.Add (typeof(Action<CallSite, object, string, string>), (Action<CallSite, object, string, string>)SetKeyStr<string>);
			delegates.Add (typeof(Action<CallSite, object, string, object>), (Action<CallSite, object, string, object>)SetKeyStr<object>);

			delegates.Add (typeof(Action<CallSite, object, object, int>),    (Action<CallSite, object, object, int>)SetKeyObject<int>);
			delegates.Add (typeof(Action<CallSite, object, object, uint>),   (Action<CallSite, object, object, uint>)SetKeyObject<uint>);
			delegates.Add (typeof(Action<CallSite, object, object, double>), (Action<CallSite, object, object, double>)SetKeyObject<double>);
			delegates.Add (typeof(Action<CallSite, object, object, bool>),   (Action<CallSite, object, object, bool>)SetKeyObject<bool>);
			delegates.Add (typeof(Action<CallSite, object, object, string>), (Action<CallSite, object, object, string>)SetKeyObject<string>);
			delegates.Add (typeof(Action<CallSite, object, object, object>), (Action<CallSite, object, object, object>)SetKeyObject<object>);

		}
		
		public override object Bind (Type delegateType)
		{
			object target;
			if (delegates.TryGetValue (delegateType, out target)) {
				return target;
			}
			throw new Exception("Unable to bind set index for target " + delegateType.Name);
		}

	}
}
#endif

