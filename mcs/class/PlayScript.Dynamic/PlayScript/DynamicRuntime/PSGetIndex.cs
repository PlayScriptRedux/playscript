//
// PSGetIndex.cs
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

namespace PlayScript.DynamicRuntime
{
	public class PSGetIndex
	{
		private PSGetMember			  mGetMember;

		public dynamic GetIndexAsObject(object o, object index)
		{
			var result = GetIndexAs<object>(o, index);
			// Need to check for undefined if we're not returning AsUntyped
			if (Dynamic.IsUndefined (result))
				result = null;
			return result;
		}

		public dynamic GetIndexAsObject(object o, int index)
		{
			var result = GetIndexAs<object>(o, index);
			// Need to check for undefined if we're not returning AsUntyped
			if (Dynamic.IsUndefined (result))
				result = null;
			return result;
		}

		public dynamic GetIndexAsObject(object o, uint index)
		{
			var result = GetIndexAs<object>(o, index);
			// Need to check for undefined if we're not returning AsUntyped
			if (Dynamic.IsUndefined (result))
				result = null;
			return result;
		}

		public dynamic GetIndexAsObject(object o, long index)
		{
			var result = GetIndexAs<object>(o, index);
			// Need to check for undefined if we're not returning AsUntyped
			if (Dynamic.IsUndefined (result))
				result = null;
			return result;
		}

		public dynamic GetIndexAsObject(object o, float index)
		{
			var result = GetIndexAs<object>(o, index);
			// Need to check for undefined if we're not returning AsUntyped
			if (Dynamic.IsUndefined (result))
				result = null;
			return result;
		}

		public dynamic GetIndexAsObject(object o, double index)
		{
			var result = GetIndexAs<object>(o, index);
			// Need to check for undefined if we're not returning AsUntyped
			if (Dynamic.IsUndefined (result))
				result = null;
			return result;
		}

		public dynamic GetIndexAsObject(object o, string index)
		{
			var result = GetIndexAs<object>(o, index);
			// Need to check for undefined if we're not returning AsUntyped
			if (Dynamic.IsUndefined (result))
				result = null;
			return result;
		}

		[return: AsUntyped]
		public dynamic GetIndexAsUntyped(object o, object index)
		{
			// get untyped accessor
			var accessor = o as IDynamicAccessorUntyped;
			if (accessor != null) {
				return accessor.GetIndex(index);
			}

			return GetIndexAs<object>(o, index);
		}

		[return: AsUntyped]
		public dynamic GetIndexAsUntyped(object o, int index)
		{
			return GetIndexAs<object>(o, index);
		}

		[return: AsUntyped]
		public dynamic GetIndexAsUntyped(object o, uint index)
		{
			return GetIndexAs<object>(o, index);
		}

		[return: AsUntyped]
		public dynamic GetIndexAsUntyped(object o, long index)
		{
			return GetIndexAs<object>(o, index);
		}

		[return: AsUntyped]
		public dynamic GetIndexAsUntyped(object o, float index)
		{
			return GetIndexAs<object>(o, index);
		}

		[return: AsUntyped]
		public dynamic GetIndexAsUntyped(object o, double index)
		{
			return GetIndexAs<object>(o, index);
		}

		[return: AsUntyped]
		public dynamic GetIndexAsUntyped(object o, string index)
		{
			return GetIndexAs<object>(o, index);
		}

		public T GetIndexAs<T> (object o, int index)
		{
			Stats.Increment(StatsCounter.GetIndexBinderInvoked);
			Stats.Increment(StatsCounter.GetIndexBinder_Int_Invoked);

			// get accessor for value type T
			var accessor = o as IDynamicAccessor<T>;
			if (accessor != null) {
				return accessor.GetIndex(index);
			}

			// fallback on object accessor and cast it to T
			var untypedAccessor = o as IDynamicAccessorUntyped;
			if (untypedAccessor != null) {
				object value = untypedAccessor.GetIndex(index);
				// convert value to T
				if (value == null) {
					return default(T);
				} else if (value is T) {
					return (T)value;
				} else if (Dynamic.IsUndefined(value)) {
					return Dynamic.GetUndefinedValue<T>();
				} else {
					return PlayScript.Dynamic.ConvertValue<T>(value);
				}
			}

			var l = o as IList<T>;
			if (l != null) {
				return l [index];
			}

			var l2 = o as IList;
			if (l2 != null) {
				if (index >= l2.Count)
					return default(T);
				var ro = l2 [index];
				if (ro is T) {
					return (T)ro;
				} else {
					return Dynamic.ConvertValue<T>(ro);
				}
			}

			var d = o as IDictionary<int,T>;
			if (d != null) {
				var ro = d[index];
				if (ro is T) {
					return (T)ro;
				} else {
					return Dynamic.ConvertValue<T>(ro);
				}
			}

			var d2 = o as IDictionary;
			if (d2 != null) {
				var ro = d2[index];
				if (ro is T) {
					return (T)ro;
				} else {
					return Dynamic.ConvertValue<T>(ro);
				}
			}

			return Dynamic.GetUndefinedValue<T>();
		}

		public T GetIndexAs<T> (object o, uint index)
		{
			return GetIndexAs<T>(o, (int)index);
		}

		public T GetIndexAs<T> (object o, double index)
		{
			return GetIndexAs<T>(o, (int)index);
		}

		public T GetIndexAs<T> (object o, float index)
		{
			return GetIndexAs<T>(o, (int)index);
		}

		public T GetIndexAs<T> (object o, string key)
		{
			Stats.Increment(StatsCounter.GetIndexBinderInvoked);
			Stats.Increment(StatsCounter.GetIndexBinder_Key_Invoked);

			// get accessor for value type T
			var accessor = o as IDynamicAccessor<T>;
			if (accessor != null) {
				return accessor.GetIndex(key);
			}

			// fallback on object accessor and cast it to T
			var untypedAccessor = o as IDynamicAccessorUntyped;
			if (untypedAccessor != null) {
				object value = untypedAccessor.GetIndex(key);
				if (value == null) return default(T);
				if (value is T) {
					return (T)value;
				} else {
					return PlayScript.Dynamic.ConvertValue<T>(value);
				}
			}

			// handle dictionaries
			var dict = o as IDictionary;
			if (dict != null) {
				Stats.Increment(StatsCounter.GetIndexBinder_Key_Dictionary_Invoked);

				var ro = dict[key];
				if (ro is T) {
					return (T)ro;
				} else {
					return Dynamic.ConvertValue<T>(ro);
				}
			} 

			// fallback on getmemberbinder to do the hard work 
			Stats.Increment(StatsCounter.GetIndexBinder_Key_Property_Invoked);

			// create a get member binder here
			if (mGetMember == null) {
				mGetMember = new PSGetMember(key);
			}
			
			// get member value
			return mGetMember.GetNamedMember<T>(o, key);			
		}
		
		public T GetIndexAs<T> (object o, object key)
		{
			key = PlayScript.Dynamic.FormatKeyForAs (key);
			if (key is int) {
				return GetIndexAs<T>(o, (int)key);
			} else if (key is string) {
				return GetIndexAs<T>(o, (string)key);
			}  else if (key is uint) {
				return GetIndexAs<T>(o, (uint)key);
			}  else if (key is double) {
				return GetIndexAs<T>(o, (double)key);
			}  else if (key is float) {
				return GetIndexAs<T>(o, (float)key);
			} else {
				throw new InvalidOperationException("Cannot index object with key of type: " + key.GetType());
			}
		}

		public PSGetIndex()
		{
		}
	}
}
#endif

