//
// CSharpSetMemberBinder.cs
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
	class CSharpSetMemberBinder : SetMemberBinder
	{
		readonly CSharpBinderFlags flags;
		IList<CSharpArgumentInfo> argumentInfo;
		Type callingContext;

		public CSharpSetMemberBinder (CSharpBinderFlags flags, string name, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
			: base (name, false)
		{
			this.flags = flags;
			this.callingContext = callingContext;
			this.argumentInfo = argumentInfo.ToReadOnly ();
		}
		
		public override DynamicMetaObject FallbackSetMember (DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
		{
			var ctx = DynamicContext.Create ();
			var source = ctx.CreateCompilerExpression (argumentInfo [1], value);
			var expr = ctx.CreateCompilerExpression (argumentInfo [0], target);

			// Field assignment
			expr = new Compiler.MemberAccess (expr, Name);

			// Compound assignment under dynamic context does not convert result
			// expression but when setting member type we need to do explicit
			// conversion to ensure type match between member type and dynamic
			// expression type
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
using System.Collections.Generic;
using ActionScript.Expando;

namespace ActionScript.RuntimeBinder
{
	class CSharpSetMemberBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> delegates = new Dictionary<Type, object>();
		
		readonly string name;
//		readonly CSharpBinderFlags flags;
//		List<CSharpArgumentInfo> argumentInfo;
//		Type callingContext;

		public static void SetMember<T> (CallSite site, object o, T value)
		{
			var expando = o as ExpandoObject;
			if (expando != null) {
				var binder = (CSharpSetMemberBinder)site.Binder;
				expando[binder.name] = value;
			}
		}

		public CSharpSetMemberBinder (CSharpBinderFlags flags, string name, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			this.name = name;
//			this.flags = flags;
//			this.callingContext = callingContext;
//			this.argumentInfo = new List<CSharpArgumentInfo>(argumentInfo);
		}

		static CSharpSetMemberBinder ()
		{
			delegates.Add (typeof(Action<CallSite, object, int>), (Action<CallSite, object, int>)SetMember<int>);
			delegates.Add (typeof(Action<CallSite, object, uint>), (Action<CallSite, object, uint>)SetMember<uint>);
			delegates.Add (typeof(Action<CallSite, object, double>), (Action<CallSite, object, double>)SetMember<double>);
			delegates.Add (typeof(Action<CallSite, object, bool>), (Action<CallSite, object, bool>)SetMember<bool>);
			delegates.Add (typeof(Action<CallSite, object, string>), (Action<CallSite, object, string>)SetMember<string>);
			delegates.Add (typeof(Action<CallSite, object, object>), (Action<CallSite, object, object>)SetMember<object>);
		}

		public override object Bind (Type delegateType)
		{
			object target;
			if (delegates.TryGetValue (delegateType, out target)) {
				return target;
			}
			throw new Exception("Unable to bind set member for target " + delegateType.FullName);
		}

	}
}

#endif