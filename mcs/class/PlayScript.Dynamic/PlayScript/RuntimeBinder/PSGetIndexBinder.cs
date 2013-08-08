//
// CSharpGetIndexBinder.cs
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
using System.Collections;
using System.Collections.Generic;
using PlayScript;

namespace PlayScript.RuntimeBinder
{
	class PSGetIndexBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> delegates = new Dictionary<Type, object>();

		private CallSite 			  mGetMemberCallSite;

		private static T GetIndex<T> (CallSite site, object o, int index)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.GetIndexBinderInvoked);
			Stats.Increment(StatsCounter.GetIndexBinder_Int_Invoked);
#endif
			var l = o as IList<T>;
			if (l != null) {
				return l [index];
			}

			var l2 = o as IList;
			if (l2 != null) {
				var ro = l2 [index];
				if (ro is T) {
					return (T)ro;
				} else {
					return (T)Convert.ChangeType(ro, typeof(T));
				}
			}

			var d = o as IDictionary<int,T>;
			if (d != null) {
				var ro = d[index];
				if (ro is T) {
					return (T)ro;
				} else {
					return (T)Convert.ChangeType(ro, typeof(T));
				}
			}

			var d2 = o as IDictionary;
			if (d2 != null) {
				var ro = d2[index];
				if (ro is T) {
					return (T)ro;
				} else {
					return (T)Convert.ChangeType(ro, typeof(T));
				}
			}

			return default(T);
		}

		private static T GetIndexUnsigned<T> (CallSite site, object o, uint index)
		{
			return GetIndex<T>(site, o, (int)index);
		}

		private static T GetIndexDouble<T> (CallSite site, object o, double index)
		{
			return GetIndex<T>(site, o, (int)index);
		}

		private static T GetKeyStr<T> (CallSite site, object o, string key)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.GetIndexBinderInvoked);
			Stats.Increment(StatsCounter.GetIndexBinder_Key_Invoked);
#endif
			// handle dictionaries
			var dict = o as IDictionary;
			if (dict != null) {
#if BINDERS_RUNTIME_STATS
				Stats.Increment(StatsCounter.GetIndexBinder_Key_Dictionary_Invoked);
#endif
				var ro = dict[key];
				if (ro is T) {
					return (T)ro;
				} else {
					return (T)Convert.ChangeType(ro, typeof(T));
				}
			} 

			// fallback on getmemberbinder to do the hard work 
			var binder = site.Binder as PSGetIndexBinder;

#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.GetIndexBinder_Key_Property_Invoked);
#endif

			// create a get member binder here
			if (binder.mGetMemberCallSite == null) {
				var getMemberBinder   = new PSGetMemberBinder(key, null, null);
				binder.mGetMemberCallSite = CallSite< Func<CallSite, object, T> >.Create(getMemberBinder);
			}
			
			// get member value
			return PSGetMemberBinder.GetValue<T>(binder.mGetMemberCallSite, o, key);			
		}

		
		private static T GetKeyObject<T> (CallSite site, object o, object key)
		{
			if (key is int) {
				return GetIndex<T>(site, o, (int)key);
			} else if (key is string) {
				return GetKeyStr<T>(site, o, (string)key);
			}  else if (key is uint) {
				return GetIndexUnsigned<T>(site, o, (uint)key);
			}  else if (key is double) {
				return GetIndexDouble<T>(site, o, (double)key);
			} else {
				throw new InvalidOperationException("Cannot index object with key of type: " + key.GetType());
			}
		}
		

		public PSGetIndexBinder (Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
		}

		static PSGetIndexBinder ()
		{
			delegates.Add (typeof(Func<CallSite, object, int, int>),    (Func<CallSite, object, int, int>)GetIndex<int>);
			delegates.Add (typeof(Func<CallSite, object, int, uint>),   (Func<CallSite, object, int, uint>)GetIndex<uint>);
			delegates.Add (typeof(Func<CallSite, object, int, double>), (Func<CallSite, object, int, double>)GetIndex<double>);
			delegates.Add (typeof(Func<CallSite, object, int, bool>),   (Func<CallSite, object, int, bool>)GetIndex<bool>);
			delegates.Add (typeof(Func<CallSite, object, int, string>), (Func<CallSite, object, int, string>)GetIndex<string>);
			delegates.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite, object, int, object>)GetIndex<object>);
			
			delegates.Add (typeof(Func<CallSite, object, uint, int>),    (Func<CallSite, object, uint, int>)GetIndexUnsigned<int>);
			delegates.Add (typeof(Func<CallSite, object, uint, uint>),   (Func<CallSite, object, uint, uint>)GetIndexUnsigned<uint>);
			delegates.Add (typeof(Func<CallSite, object, uint, double>), (Func<CallSite, object, uint, double>)GetIndexUnsigned<double>);
			delegates.Add (typeof(Func<CallSite, object, uint, bool>),   (Func<CallSite, object, uint, bool>)GetIndexUnsigned<bool>);
			delegates.Add (typeof(Func<CallSite, object, uint, string>), (Func<CallSite, object, uint, string>)GetIndexUnsigned<string>);
			delegates.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite, object, uint, object>)GetIndexUnsigned<object>);
			
			delegates.Add (typeof(Func<CallSite, object, double, int>),    (Func<CallSite, object, double, int>)GetIndexDouble<int>);
			delegates.Add (typeof(Func<CallSite, object, double, uint>),   (Func<CallSite, object, double, uint>)GetIndexDouble<uint>);
			delegates.Add (typeof(Func<CallSite, object, double, double>), (Func<CallSite, object, double, double>)GetIndexDouble<double>);
			delegates.Add (typeof(Func<CallSite, object, double, bool>),   (Func<CallSite, object, double, bool>)GetIndexDouble<bool>);
			delegates.Add (typeof(Func<CallSite, object, double, string>), (Func<CallSite, object, double, string>)GetIndexDouble<string>);
			delegates.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite, object, double, object>)GetIndexDouble<object>);
			
			delegates.Add (typeof(Func<CallSite, object, string, int>),    (Func<CallSite, object, string, int>)GetKeyStr<int>);
			delegates.Add (typeof(Func<CallSite, object, string, uint>),   (Func<CallSite, object, string, uint>)GetKeyStr<uint>);
			delegates.Add (typeof(Func<CallSite, object, string, double>), (Func<CallSite, object, string, double>)GetKeyStr<double>);
			delegates.Add (typeof(Func<CallSite, object, string, bool>),   (Func<CallSite, object, string, bool>)GetKeyStr<bool>);
			delegates.Add (typeof(Func<CallSite, object, string, string>), (Func<CallSite, object, string, string>)GetKeyStr<string>);
			delegates.Add (typeof(Func<CallSite, object, string, object>), (Func<CallSite, object, string, object>)GetKeyStr<object>);
			
			delegates.Add (typeof(Func<CallSite, object, object, int>),    (Func<CallSite, object, object, int>)GetKeyObject<int>);
			delegates.Add (typeof(Func<CallSite, object, object, uint>),   (Func<CallSite, object, object, uint>)GetKeyObject<uint>);
			delegates.Add (typeof(Func<CallSite, object, object, double>), (Func<CallSite, object, object, double>)GetKeyObject<double>);
			delegates.Add (typeof(Func<CallSite, object, object, bool>),   (Func<CallSite, object, object, bool>)GetKeyObject<bool>);
			delegates.Add (typeof(Func<CallSite, object, object, string>), (Func<CallSite, object, object, string>)GetKeyObject<string>);
			delegates.Add (typeof(Func<CallSite, object, object, object>), (Func<CallSite, object, object, object>)GetKeyObject<object>);
			
		}
		
		public override object Bind (Type delegateType)
		{
			object target;
			if (delegates.TryGetValue (delegateType, out target)) {
				return target;
			}
			throw new Exception("Unable to bind get index for target " + delegateType.Name);
		}

	}
}
#endif

