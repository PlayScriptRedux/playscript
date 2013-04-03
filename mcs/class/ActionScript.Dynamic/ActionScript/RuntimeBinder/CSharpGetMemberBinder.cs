//
// CSharpGetMemberBinder.cs
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

namespace ActionScript.RuntimeBinder
{
	class CSharpGetMemberBinder : GetMemberBinder
	{
		IList<CSharpArgumentInfo> argumentInfo;
		Type callingContext;
		
		public CSharpGetMemberBinder (string name, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
			: base (name, false)
		{
			this.callingContext = callingContext;
			this.argumentInfo = argumentInfo.ToReadOnly ();
		}
		
		public override DynamicMetaObject FallbackGetMember (DynamicMetaObject target, DynamicMetaObject errorSuggestion)
		{
			var ctx = DynamicContext.Create ();

			var expr = ctx.CreateCompilerExpression (argumentInfo [0], target);
			expr = new Compiler.MemberAccess (expr, Name);
			expr = new Compiler.Cast (new Compiler.TypeExpression (ctx.ImportType (ReturnType), Compiler.Location.Null), expr, Compiler.Location.Null);

			var binder = new CSharpBinder (this, expr, errorSuggestion);
			binder.AddRestrictions (target);

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
using ActionScript.Expando;

namespace ActionScript.RuntimeBinder
{
	class CSharpGetMemberBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> delegates = new Dictionary<Type, object>();

		readonly string name;
//		IList<CSharpArgumentInfo> argumentInfo;
//		Type callingContext;
		
		public CSharpGetMemberBinder (string name, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			this.name = name;
//			this.callingContext = callingContext;
//			this.argumentInfo = argumentInfo != null ? new List<CSharpArgumentInfo>(argumentInfo) : null;
		}

		public static T GetMember<T> (CallSite site, object o)
		{
			var name = ((CSharpGetMemberBinder)site.Binder).name;
			T value = default(T);

			// Try Dictionary<T>
			var d = o as IDictionary<string,T>;
			if (d != null) {
				if (d.TryGetValue (name, out value)) {
					return value;
				}
			}

			// Try IDictionary
			var d2 = o as IDictionary;
			if (d2 != null) {
				object vo = d2[name];
				if (vo != null) {
					if (vo.GetType () == typeof(T)) {
						value = (T)vo;
					} else {
						value = (T)Convert.ChangeType(vo, typeof(T));
					}
					return value;
				}
			}

			// Try property
			var props = o.GetType ().GetProperties();
			var len = props.Length;
			for (var pi = 0; pi < len; pi++) {
				var prop = props[pi];
				var propType = prop.PropertyType;
				var getter = prop.GetGetMethod();
				if (getter != null && getter.IsPublic && !getter.IsStatic && prop.Name == name) {
					if (typeof(T) == typeof(object) || typeof(T) == propType) {
						value = (T)getter.Invoke (o, null);
					} else {
						value = (T)Convert.ChangeType(getter.Invoke(o, null), typeof(T));
					}
					return value;
				}
			}

			throw new Exception("Unable to find member " + name);
		}

		static CSharpGetMemberBinder ()
		{
			delegates.Add (typeof(Func<CallSite, object, int>), (Func<CallSite, object, int>)GetMember<int>);
			delegates.Add (typeof(Func<CallSite, object, uint>), (Func<CallSite, object, uint>)GetMember<uint>);
			delegates.Add (typeof(Func<CallSite, object, double>), (Func<CallSite, object, double>)GetMember<double>);
			delegates.Add (typeof(Func<CallSite, object, bool>), (Func<CallSite, object, bool>)GetMember<bool>);
			delegates.Add (typeof(Func<CallSite, object, string>), (Func<CallSite, object, string>)GetMember<string>);
			delegates.Add (typeof(Func<CallSite, object, object>), (Func<CallSite, object, object>)GetMember<object>);
		}
		
		public override object Bind (Type delegateType)
		{
			object target;
			if (delegates.TryGetValue (delegateType, out target)) {
				return target;
			}
			throw new Exception("Unable to bind get member for target " + delegateType.FullName);
		}

	}
}

#endif