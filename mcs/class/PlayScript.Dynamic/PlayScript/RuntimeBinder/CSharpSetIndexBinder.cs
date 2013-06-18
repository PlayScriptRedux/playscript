//
// CSharpSetIndexBinder.cs
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
	class CSharpSetIndexBinder : SetIndexBinder
	{
		readonly CSharpBinderFlags flags;
		IList<CSharpArgumentInfo> argumentInfo;
		Type callingContext;

		public CSharpSetIndexBinder (CSharpBinderFlags flags, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
			: base (CSharpArgumentInfo.CreateCallInfo (argumentInfo, 2))
		{
			this.flags = flags;
			this.callingContext = callingContext;
			this.argumentInfo = argumentInfo.ToReadOnly ();
		}
		
		public override DynamicMetaObject FallbackSetIndex (DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
		{
			if (argumentInfo.Count != indexes.Length + 2) {
				if (errorSuggestion == null)
					throw new NotImplementedException ();

				return errorSuggestion;
			}

			var ctx = DynamicContext.Create ();
			var expr = ctx.CreateCompilerExpression (argumentInfo [0], target);
			var args = ctx.CreateCompilerArguments (argumentInfo.Skip (1), indexes);
			expr = new Compiler.ElementAccess (expr, args, Compiler.Location.Null);

			var source = ctx.CreateCompilerExpression (argumentInfo [indexes.Length + 1], value);

			// Same conversion as in SetMemberBinder
			if ((flags & CSharpBinderFlags.ValueFromCompoundAssignment) != 0) {
				expr = new Compiler.RuntimeExplicitAssign (expr, source);
			} else {
				expr = new Compiler.SimpleAssign (expr, source);
			}
			expr = new Compiler.Cast (new Compiler.TypeExpression (ctx.ImportType (ReturnType), Compiler.Location.Null), expr, Compiler.Location.Null);

			if ((flags & CSharpBinderFlags.CheckedContext) != 0)
				expr = new Compiler.CheckedExpr (expr, Compiler.Location.Null);

			var binder = new CSharpBinder (this, expr, errorSuggestion);
			binder.AddRestrictions (target);
			binder.AddRestrictions (value);
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
	class CSharpSetIndexBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> delegates = new Dictionary<Type, object>();

//		readonly CSharpBinderFlags flags;
//		List<CSharpArgumentInfo> argumentInfo;
//		Type callingContext;

		private static void SetIndex<T> (CallSite site, object o, int index, T value)
		{
			var l = o as IList<T>;
			if (l != null) {
				l [index] = value;
			} else {
				var a = o as T[];
				if (a != null) {
					a[index] = value;
				} else {
					var l2 = o as IList;
					if (l2 != null) {
						l2 [index] = value;
					} else {
						var d = o as IDictionary<int,T>;
						if (d != null) {
							d[index] = value;
						} else {
							var d2 = o as IDictionary;
							if (d2 != null) {
								d2[index] = value;
							}
						}
					}
				}
			}
		}


		private static void SetKeyStr<T> (CallSite site, object o, string key, T value)
		{
			var d = o as IDictionary<string,T>;
			if (d != null) {
				d[key] = value;
			} else {
				var d2 = o as IDictionary;
				if (d2 != null) {
					d2[key] = value;
				} else {
					Dynamic.SetInstanceMember(o, key, value);
				}
			}
		}

		private static void SetKey<T1,T2> (CallSite site, object o, T1 key, T2 value)
		{
			var d = o as IDictionary<T1,T2>;
			if (d != null) {
				d[key] = value;
			} else {
				var d2 = o as IDictionary;
				if (d2 != null) {
					d2[key] = value;
				} 
			}
		}

		public CSharpSetIndexBinder (CSharpBinderFlags flags, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
//			this.flags = flags;
//			this.callingContext = callingContext;
//			this.argumentInfo = argumentInfo.ToReadOnly ();
		}

		static CSharpSetIndexBinder ()
		{
			delegates.Add (typeof(Action<CallSite, object, int, int>),    (Action<CallSite, object, int, int>)SetIndex<int>);
			delegates.Add (typeof(Action<CallSite, object, int, uint>),   (Action<CallSite, object, int, uint>)SetIndex<uint>);
			delegates.Add (typeof(Action<CallSite, object, int, double>), (Action<CallSite, object, int, double>)SetIndex<double>);
			delegates.Add (typeof(Action<CallSite, object, int, bool>),   (Action<CallSite, object, int, bool>)SetIndex<bool>);
			delegates.Add (typeof(Action<CallSite, object, int, string>), (Action<CallSite, object, int, string>)SetIndex<string>);
			delegates.Add (typeof(Action<CallSite, object, int, object>), (Action<CallSite, object, int, object>)SetIndex<object>);

			delegates.Add (typeof(Action<CallSite, object, uint, int>),    (Action<CallSite, object, uint, int>)SetKey<uint,int>);
			delegates.Add (typeof(Action<CallSite, object, uint, uint>),   (Action<CallSite, object, uint, uint>)SetKey<uint,uint>);
			delegates.Add (typeof(Action<CallSite, object, uint, double>), (Action<CallSite, object, uint, double>)SetKey<uint,double>);
			delegates.Add (typeof(Action<CallSite, object, uint, bool>),   (Action<CallSite, object, uint, bool>)SetKey<uint,bool>);
			delegates.Add (typeof(Action<CallSite, object, uint, string>), (Action<CallSite, object, uint, string>)SetKey<uint,string>);
			delegates.Add (typeof(Action<CallSite, object, uint, object>), (Action<CallSite, object, uint, object>)SetKey<uint,object>);

			delegates.Add (typeof(Action<CallSite, object, double, int>),    (Action<CallSite, object, double, int>)SetKey<double,int>);
			delegates.Add (typeof(Action<CallSite, object, double, uint>),   (Action<CallSite, object, double, uint>)SetKey<double,uint>);
			delegates.Add (typeof(Action<CallSite, object, double, double>), (Action<CallSite, object, double, double>)SetKey<double,double>);
			delegates.Add (typeof(Action<CallSite, object, double, bool>),   (Action<CallSite, object, double, bool>)SetKey<double,bool>);
			delegates.Add (typeof(Action<CallSite, object, double, string>), (Action<CallSite, object, double, string>)SetKey<double,string>);
			delegates.Add (typeof(Action<CallSite, object, double, object>), (Action<CallSite, object, double, object>)SetKey<double,object>);

			delegates.Add (typeof(Action<CallSite, object, string, int>),    (Action<CallSite, object, string, int>)SetKeyStr<int>);
			delegates.Add (typeof(Action<CallSite, object, string, uint>),   (Action<CallSite, object, string, uint>)SetKeyStr<uint>);
			delegates.Add (typeof(Action<CallSite, object, string, double>), (Action<CallSite, object, string, double>)SetKeyStr<double>);
			delegates.Add (typeof(Action<CallSite, object, string, bool>),   (Action<CallSite, object, string, bool>)SetKeyStr<bool>);
			delegates.Add (typeof(Action<CallSite, object, string, string>), (Action<CallSite, object, string, string>)SetKeyStr<string>);
			delegates.Add (typeof(Action<CallSite, object, string, object>), (Action<CallSite, object, string, object>)SetKeyStr<object>);

			delegates.Add (typeof(Action<CallSite, bool, object, int>),    (Action<CallSite, object, bool, int>)SetKey<bool,int>);
			delegates.Add (typeof(Action<CallSite, bool, object, uint>),   (Action<CallSite, object, bool, uint>)SetKey<bool,uint>);
			delegates.Add (typeof(Action<CallSite, bool, object, double>), (Action<CallSite, object, bool, double>)SetKey<bool,double>);
			delegates.Add (typeof(Action<CallSite, bool, object, bool>),   (Action<CallSite, object, bool, bool>)SetKey<bool,bool>);
			delegates.Add (typeof(Action<CallSite, bool, object, string>), (Action<CallSite, object, bool, string>)SetKey<bool,string>);
			delegates.Add (typeof(Action<CallSite, bool, object, object>), (Action<CallSite, object, bool, object>)SetKey<bool,object>);

			delegates.Add (typeof(Action<CallSite, object, object, int>),    (Action<CallSite, object, object, int>)SetKey<object,int>);
			delegates.Add (typeof(Action<CallSite, object, object, uint>),   (Action<CallSite, object, object, uint>)SetKey<object,uint>);
			delegates.Add (typeof(Action<CallSite, object, object, double>), (Action<CallSite, object, object, double>)SetKey<object,double>);
			delegates.Add (typeof(Action<CallSite, object, object, bool>),   (Action<CallSite, object, object, bool>)SetKey<object,bool>);
			delegates.Add (typeof(Action<CallSite, object, object, string>), (Action<CallSite, object, object, string>)SetKey<object,string>);
			delegates.Add (typeof(Action<CallSite, object, object, object>), (Action<CallSite, object, object, object>)SetKey<object,object>);

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