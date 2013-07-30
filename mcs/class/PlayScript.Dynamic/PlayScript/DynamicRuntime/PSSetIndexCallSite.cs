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
	public class PSSetIndexCallSite
	{
		private PSSetMemberCallSite mSetMemberCallSite;

		public void SetIndexAs<T> (object o, int index, T value)
		{
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

		public void SetIndexAs<T> (object o, uint index, T value)
		{
			SetIndexAs<T>(o, (int)index, value);
		}

		public void SetIndexAs<T> (object o, double index, T value)
		{
			SetIndexAs<T>(o, (int)index, value);
		}

		public void SetIndexAs<T> (object o, string key, T value)
		{
			// handle dictionaries
			var dict = o as IDictionary;
			if (dict != null) {
				dict[key] = (object)value;
				return;
			} 

			// fallback on setmemberbinder to do the hard work 
			// create a set member binder here to set
			if (mSetMemberCallSite == null) {
				mSetMemberCallSite   = new PSSetMemberCallSite(key);
			}

			// set member value
			mSetMemberCallSite.SetNamedMember(o, key, value);			
		}

		public void SetIndexAs<T> (CallSite site, object o, object key, T value)
		{
			if (key is int) {
				SetIndexAs<T>(site, o, (int)key, value);
			} else if (key is string) {
				SetIndexAs<T>(site, o, (string)key, value);
			} else  if (key is uint) {
				SetIndexAs<T>(site, o, (uint)key, value);
			} else  if (key is double) {
				SetIndexAs<T>(site, o, (double)key, value);
			} else {
				throw new InvalidOperationException("Cannot index object with key of type: " + key.GetType());
			}
		}

		public PSSetIndexCallSite ()
		{
		}
	}
}
#endif

