//
// CallSite.cs
//
// Authors:
//	Ben Cooley <bcooley@zynga.com>
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
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