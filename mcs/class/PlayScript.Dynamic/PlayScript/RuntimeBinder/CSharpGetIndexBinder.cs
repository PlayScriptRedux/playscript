//
// CSharpGetIndexBinder.cs
//
// Authors:
//	Marek Safar  <marek.safar@gmail.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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

#if DYNAMIC_SUPPORT

using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Linq;
using Compiler = Mono.CSharp;

namespace PlayScript.RuntimeBinder
{
	class CSharpGetIndexBinder : GetIndexBinder
	{
		IList<CSharpArgumentInfo> argumentInfo;
		Type callingContext;
		
		public CSharpGetIndexBinder (Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
			: base (CSharpArgumentInfo.CreateCallInfo (argumentInfo, 1))
		{
			this.callingContext = callingContext;
			this.argumentInfo = argumentInfo.ToReadOnly ();
		}
			
		public override DynamicMetaObject FallbackGetIndex (DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
		{
			if (argumentInfo.Count != indexes.Length + 1) {
				if (errorSuggestion == null)
					throw new NotImplementedException ();

				return errorSuggestion;
			}

			var ctx = DynamicContext.Create ();
			var expr = ctx.CreateCompilerExpression (argumentInfo [0], target);
			var args = ctx.CreateCompilerArguments (argumentInfo.Skip (1), indexes);
			expr = new Compiler.ElementAccess (expr, args, Compiler.Location.Null);
			expr = new Compiler.Cast (new Compiler.TypeExpression (ctx.ImportType (ReturnType), Compiler.Location.Null), expr, Compiler.Location.Null);

			var binder = new CSharpBinder (this, expr, errorSuggestion);
			binder.AddRestrictions (target);
			binder.AddRestrictions (indexes);

			return binder.Bind (ctx, callingContext);
		}
	}
}

#else

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

using System;
using System.Collections;
using System.Collections.Generic;

namespace PlayScript.RuntimeBinder
{
	class CSharpGetIndexBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> delegates = new Dictionary<Type, object>();

//		IList<CSharpArgumentInfo> argumentInfo;
//		Type callingContext;

		private static void ThrowCantIndexError (object o)
		{
			throw new Exception("Unable to get indexer on type " + o.GetType ().FullName);
		}

		private static T GetIndex<T> (CallSite site, object o, int index)
		{
			Stats.Increment(StatsCounter.GetIndexBinderInvoked);
			Stats.Increment(StatsCounter.GetIndexBinder_Int_Invoked);

			var l = o as IList<T>;
			if (l != null) {
				return l [index];
			}

			var a = o as T[];
			if (a != null) {
				return a [index];
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

			ThrowCantIndexError (o);
			return default(T);
		}


		private static T GetKeyStr<T> (CallSite site, object o, string key)
		{
			Stats.Increment(StatsCounter.GetIndexBinderInvoked);
//			Stats.Increment(StatsCounter.GetIndexBinder_KeyStr_Invoked);

			var d = o as IDictionary<string,T>;
			if (d != null) {
				return d[key];
			}
			var d2 = o as IDictionary;
			if (d2 != null) {
				var ro = d2[key];
				if (ro is T) {
					return (T)ro;
				} else {
					return (T)Convert.ChangeType(ro, typeof(T));
				}
			}

			object value;
			if (o is System.Type)
			{
				// try to get a static member
				if (Dynamic.GetStaticMember((System.Type)o, key, out value)) {
					return (T)value;
				}
			}
			else
			{
				// get instance member
				if (Dynamic.GetInstanceMember(o, key, out value)) {
					return (T)value;
				}
			}

			ThrowCantIndexError (o);
			return default(T);
		}
		
		private static T2 GetKey<T1,T2> (CallSite site, object o, T1 key)
		{

			Stats.Increment(StatsCounter.GetIndexBinderInvoked);
			Stats.Increment(StatsCounter.GetIndexBinder_Key_Invoked);

			var d = o as IDictionary<T1,T2>;
			if (d != null) {
				return d[key];
			}

			var d2 = o as IDictionary;
			if (d2 != null) {
				var ro = d2[key];
				if (ro is T2) {
					return (T2)ro;
				} else {
					return (T2)Convert.ChangeType(ro, typeof(T2));
				}
			}

			ThrowCantIndexError (o);
			return default(T2);
		}

		public CSharpGetIndexBinder (Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
//			this.callingContext = callingContext;
//			this.argumentInfo = argumentInfo.ToReadOnly ();
		}

		static CSharpGetIndexBinder ()
		{
			delegates.Add (typeof(Func<CallSite, object, int, int>),    (Func<CallSite, object, int, int>)GetIndex<int>);
			delegates.Add (typeof(Func<CallSite, object, int, uint>),   (Func<CallSite, object, int, uint>)GetIndex<uint>);
			delegates.Add (typeof(Func<CallSite, object, int, double>), (Func<CallSite, object, int, double>)GetIndex<double>);
			delegates.Add (typeof(Func<CallSite, object, int, bool>),   (Func<CallSite, object, int, bool>)GetIndex<bool>);
			delegates.Add (typeof(Func<CallSite, object, int, string>), (Func<CallSite, object, int, string>)GetIndex<string>);
			delegates.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite, object, int, object>)GetIndex<object>);
			
			delegates.Add (typeof(Func<CallSite, object, uint, int>),    (Func<CallSite, object, uint, int>)GetKey<uint,int>);
			delegates.Add (typeof(Func<CallSite, object, uint, uint>),   (Func<CallSite, object, uint, uint>)GetKey<uint,uint>);
			delegates.Add (typeof(Func<CallSite, object, uint, double>), (Func<CallSite, object, uint, double>)GetKey<uint,double>);
			delegates.Add (typeof(Func<CallSite, object, uint, bool>),   (Func<CallSite, object, uint, bool>)GetKey<uint,bool>);
			delegates.Add (typeof(Func<CallSite, object, uint, string>), (Func<CallSite, object, uint, string>)GetKey<uint,string>);
			delegates.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite, object, uint, object>)GetKey<uint,object>);
			
			delegates.Add (typeof(Func<CallSite, object, double, int>),    (Func<CallSite, object, double, int>)GetKey<double,int>);
			delegates.Add (typeof(Func<CallSite, object, double, uint>),   (Func<CallSite, object, double, uint>)GetKey<double,uint>);
			delegates.Add (typeof(Func<CallSite, object, double, double>), (Func<CallSite, object, double, double>)GetKey<double,double>);
			delegates.Add (typeof(Func<CallSite, object, double, bool>),   (Func<CallSite, object, double, bool>)GetKey<double,bool>);
			delegates.Add (typeof(Func<CallSite, object, double, string>), (Func<CallSite, object, double, string>)GetKey<double,string>);
			delegates.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite, object, double, object>)GetKey<double,object>);
			
			delegates.Add (typeof(Func<CallSite, object, string, int>),    (Func<CallSite, object, string, int>)GetKeyStr<int>);
			delegates.Add (typeof(Func<CallSite, object, string, uint>),   (Func<CallSite, object, string, uint>)GetKeyStr<uint>);
			delegates.Add (typeof(Func<CallSite, object, string, double>), (Func<CallSite, object, string, double>)GetKeyStr<double>);
			delegates.Add (typeof(Func<CallSite, object, string, bool>),   (Func<CallSite, object, string, bool>)GetKeyStr<bool>);
			delegates.Add (typeof(Func<CallSite, object, string, string>), (Func<CallSite, object, string, string>)GetKeyStr<string>);
			delegates.Add (typeof(Func<CallSite, object, string, object>), (Func<CallSite, object, string, object>)GetKeyStr<object>);
			
			delegates.Add (typeof(Func<CallSite, bool, object, int>),    (Func<CallSite, object, bool, int>)GetKey<bool,int>);
			delegates.Add (typeof(Func<CallSite, bool, object, uint>),   (Func<CallSite, object, bool, uint>)GetKey<bool,uint>);
			delegates.Add (typeof(Func<CallSite, bool, object, double>), (Func<CallSite, object, bool, double>)GetKey<bool,double>);
			delegates.Add (typeof(Func<CallSite, bool, object, bool>),   (Func<CallSite, object, bool, bool>)GetKey<bool,bool>);
			delegates.Add (typeof(Func<CallSite, bool, object, string>), (Func<CallSite, object, bool, string>)GetKey<bool,string>);
			delegates.Add (typeof(Func<CallSite, bool, object, object>), (Func<CallSite, object, bool, object>)GetKey<bool,object>);
			
			delegates.Add (typeof(Func<CallSite, object, object, int>),    (Func<CallSite, object, object, int>)GetKey<object,int>);
			delegates.Add (typeof(Func<CallSite, object, object, uint>),   (Func<CallSite, object, object, uint>)GetKey<object,uint>);
			delegates.Add (typeof(Func<CallSite, object, object, double>), (Func<CallSite, object, object, double>)GetKey<object,double>);
			delegates.Add (typeof(Func<CallSite, object, object, bool>),   (Func<CallSite, object, object, bool>)GetKey<object,bool>);
			delegates.Add (typeof(Func<CallSite, object, object, string>), (Func<CallSite, object, object, string>)GetKey<object,string>);
			delegates.Add (typeof(Func<CallSite, object, object, object>), (Func<CallSite, object, object, object>)GetKey<object,object>);
			
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