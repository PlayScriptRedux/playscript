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

using ActionScript.Expando;

#if !DYNAMIC_SUPPORT

using System;
using System.Reflection;

namespace ActionScript
{
	public class CallSite
	{
		protected Type _delegateType;
		protected CallSiteBinder _binder;

		public class InvokeInfo {
			public WeakReference lastObj;
			public Type[] lastArgTypes;
			public Delegate del;
			public MethodInfo method;
			public object[] args;
			public int generation;

			public bool InvokeMatches(object obj) {
				if (obj is ExpandoObject && ((ExpandoObject)obj).Generation != generation)
					return false;
				return lastObj != null && lastObj.Target == obj;
			}

			public bool InvokeMatches(object obj, object a1) {
				if (obj is ExpandoObject && ((ExpandoObject)obj).Generation != generation)
					return false;
				return lastObj != null && lastObj.Target == obj &&
					((a1 == null && lastArgTypes[0] == null) || (a1.GetType () == lastArgTypes[0]));
			}

			public bool InvokeMatches(object obj, object a1, object a2) {
				if (obj is ExpandoObject && ((ExpandoObject)obj).Generation != generation)
					return false;
				return lastObj != null && lastObj.Target == obj &&
					((a1 == null && lastArgTypes[0] == null) || (a1.GetType () == lastArgTypes[0])) &&
					((a2 == null && lastArgTypes[1] == null) || (a2.GetType () == lastArgTypes[1]));
			}

			public bool InvokeMatches(object obj, object a1, object a2, object a3) {
				if (obj is ExpandoObject && ((ExpandoObject)obj).Generation != generation)
					return false;
				return lastObj != null && lastObj.Target == obj &&
					((a1 == null && lastArgTypes[0] == null) || (a1.GetType () == lastArgTypes[0])) &&
					((a2 == null && lastArgTypes[1] == null) || (a2.GetType () == lastArgTypes[1])) &&
					((a3 == null && lastArgTypes[2] == null) || (a3.GetType () == lastArgTypes[2]));
			}

			public bool InvokeMatches(object obj, object a1, object a2, object a3, object a4) {
				if (obj is ExpandoObject && ((ExpandoObject)obj).Generation != generation)
					return false;
				return lastObj != null && lastObj.Target == obj &&
					((a1 == null && lastArgTypes[0] == null) || (a1.GetType () == lastArgTypes[0])) &&
					((a2 == null && lastArgTypes[1] == null) || (a2.GetType () == lastArgTypes[1])) &&
					((a3 == null && lastArgTypes[2] == null) || (a3.GetType () == lastArgTypes[2])) &&
					((a4 == null && lastArgTypes[3] == null) || (a4.GetType () == lastArgTypes[3]));
			}

			public bool InvokeMatches(object obj, object a1, object a2, object a3, object a4, object a5) {
				if (obj is ExpandoObject && ((ExpandoObject)obj).Generation != generation)
					return false;
				return lastObj != null && lastObj.Target == obj &&
					((a1 == null && lastArgTypes[0] == null) || (a1.GetType () == lastArgTypes[0])) &&
					((a2 == null && lastArgTypes[1] == null) || (a2.GetType () == lastArgTypes[1])) &&
					((a3 == null && lastArgTypes[2] == null) || (a3.GetType () == lastArgTypes[2])) &&
					((a4 == null && lastArgTypes[3] == null) || (a4.GetType () == lastArgTypes[3])) &&
					((a5 == null && lastArgTypes[4] == null) || (a5.GetType () == lastArgTypes[4]));
			}

			public bool InvokeMatches(object obj, object a1, object a2, object a3, object a4, object a5, object a6) {
				if (obj is ExpandoObject && ((ExpandoObject)obj).Generation != generation)
					return false;
				return lastObj != null && lastObj.Target == obj &&
					((a1 == null && lastArgTypes[0] == null) || (a1.GetType () == lastArgTypes[0])) &&
					((a2 == null && lastArgTypes[1] == null) || (a2.GetType () == lastArgTypes[1])) &&
					((a3 == null && lastArgTypes[2] == null) || (a3.GetType () == lastArgTypes[2])) &&
					((a4 == null && lastArgTypes[3] == null) || (a4.GetType () == lastArgTypes[3])) &&
					((a5 == null && lastArgTypes[4] == null) || (a5.GetType () == lastArgTypes[4])) &&
					((a6 == null && lastArgTypes[5] == null) || (a6.GetType () == lastArgTypes[5]));
			}

			public bool InvokeMatches(object obj, object a1, object a2, object a3, object a4, object a5, object a6, object a7) {
				if (obj is ExpandoObject && ((ExpandoObject)obj).Generation != generation)
					return false;
				return lastObj != null && lastObj.Target == obj &&
					((a1 == null && lastArgTypes[0] == null) || (a1.GetType () == lastArgTypes[0])) &&
					((a2 == null && lastArgTypes[1] == null) || (a2.GetType () == lastArgTypes[1])) &&
					((a3 == null && lastArgTypes[2] == null) || (a3.GetType () == lastArgTypes[2])) &&
					((a4 == null && lastArgTypes[3] == null) || (a4.GetType () == lastArgTypes[3])) &&
					((a5 == null && lastArgTypes[4] == null) || (a5.GetType () == lastArgTypes[4])) &&
					((a6 == null && lastArgTypes[5] == null) || (a6.GetType () == lastArgTypes[5])) &&
					((a7 == null && lastArgTypes[6] == null) || (a7.GetType () == lastArgTypes[6]));
			}

			public bool InvokeMatches(object obj, object a1, object a2, object a3, object a4, object a5, object a6, object a7, object a8) {
				if (obj is ExpandoObject && ((ExpandoObject)obj).Generation != generation)
					return false;
				return lastObj != null && lastObj.Target == obj &&
					((a1 == null && lastArgTypes[0] == null) || (a1.GetType () == lastArgTypes[0])) &&
					((a2 == null && lastArgTypes[1] == null) || (a2.GetType () == lastArgTypes[1])) &&
					((a3 == null && lastArgTypes[2] == null) || (a3.GetType () == lastArgTypes[2])) &&
					((a4 == null && lastArgTypes[3] == null) || (a4.GetType () == lastArgTypes[3])) &&
					((a5 == null && lastArgTypes[4] == null) || (a5.GetType () == lastArgTypes[4])) &&
					((a6 == null && lastArgTypes[5] == null) || (a6.GetType () == lastArgTypes[5])) &&
					((a7 == null && lastArgTypes[6] == null) || (a7.GetType () == lastArgTypes[6])) &&
					((a8 == null && lastArgTypes[7] == null) || (a8.GetType () == lastArgTypes[7]));
			}

		}

		public InvokeInfo invokeInfo;

		public CallSite ()
		{
		}

		public Type DelegateType {
			get { return _delegateType; }
		}

		public CallSiteBinder Binder {
			get { return _binder; }
		}
	}

	public class CallSite<T> : CallSite 
	{
		private T _target;

		public CallSite ()
		{
		}

		public static CallSite<T> Create(CallSiteBinder binder) 
		{
			var cs = new CallSite<T>();
			cs._delegateType = typeof(T);
			cs._binder = binder;
			return cs;
		}

		public virtual T Update { 
			get { 
				_target = (T)_binder.Bind(_delegateType);
				return _target; 
			} 
		}

		public virtual T Target {
			get {
				if (_target == null) {
					return Update;
				} else {
					return _target; 
				}
			}
			set { _target = value; }
		}
	}
}

#endif