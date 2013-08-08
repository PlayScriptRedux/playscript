//
// CSharpInvokeBinder.cs
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
	class CSharpInvokeBinder : InvokeBinder
	{
		readonly CSharpBinderFlags flags;
		IList<CSharpArgumentInfo> argumentInfo;
		Type callingContext;
		
		public CSharpInvokeBinder (CSharpBinderFlags flags, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
			: base (CSharpArgumentInfo.CreateCallInfo (argumentInfo, 1))
		{
			this.flags = flags;
			this.callingContext = callingContext;
			this.argumentInfo = argumentInfo.ToReadOnly ();
		}
		
		public override DynamicMetaObject FallbackInvoke (DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
		{
			var ctx = DynamicContext.Create ();
			var expr = ctx.CreateCompilerExpression (argumentInfo [0], target);
			var c_args = ctx.CreateCompilerArguments (argumentInfo.Skip (1), args);
			expr = new Compiler.Invocation (expr, c_args);

			if ((flags & CSharpBinderFlags.ResultDiscarded) == 0)
				expr = new Compiler.Cast (new Compiler.TypeExpression (ctx.ImportType (ReturnType), Compiler.Location.Null), expr, Compiler.Location.Null);
			else
				expr = new Compiler.DynamicResultCast (ctx.ImportType (ReturnType), expr);

			var binder = new CSharpBinder (this, expr, errorSuggestion);
			binder.AddRestrictions (target);
			binder.AddRestrictions (args);

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
using System.Reflection;
using PlayScript;
using System.Collections.Generic;
using System.Diagnostics;

namespace PlayScript.RuntimeBinder
{
	class CSharpInvokeBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> delegates = new Dictionary<Type, object>();

		private Delegate _d;
		private object[] _args;
		private object[] _params;
		private object[][] _targetArray;
		private int[] _targetIndex;

//		readonly CSharpBinderFlags flags;
//		List<CSharpArgumentInfo> argumentInfo;
//		Type callingContext;

		public static void Action1 (CallSite site, object o1)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 1);
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			b._d.DynamicInvoke (outArgs);
		}
		
		public static void Action2 (CallSite site, object o1, object o2)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 2);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
			} else {
				b._args [0] = o2;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			b._d.DynamicInvoke (outArgs);
		}
		
		public static void Action3 (CallSite site, object o1, object o2, object o3)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 3);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;

			} else {
				b._args [0] = o2;
				b._args [1] = o3;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			b._d.DynamicInvoke (outArgs);
		}
		
		public static void Action4 (CallSite site, object o1, object o2, object o3, object o4)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 4);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;
				b._targetArray [2] [b._targetIndex[2]] = o4;

			} else {
				b._args [0] = o2;
				b._args [1] = o3;
				b._args [2] = o4;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			b._d.DynamicInvoke (outArgs);
		}
		
		public static void Action5 (CallSite site, object o1, object o2, object o3, object o4, object o5)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 5);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;
				b._targetArray [2] [b._targetIndex[2]] = o4;
				b._targetArray [3] [b._targetIndex[3]] = o5;
			} else {
				b._args [0] = o2;
				b._args [1] = o3;
				b._args [2] = o4;
				b._args [3] = o5;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			b._d.DynamicInvoke (outArgs);
		}
		
		public static void Action6 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 6);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;
				b._targetArray [2] [b._targetIndex[2]] = o4;
				b._targetArray [3] [b._targetIndex[3]] = o5;
				b._targetArray [4] [b._targetIndex[4]] = o6;
			} else {
				b._args [0] = o2;
				b._args [1] = o3;
				b._args [2] = o4;
				b._args [3] = o5;
				b._args [4] = o6;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			b._d.DynamicInvoke (outArgs);
		}
		
		public static void Action7 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 7);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;
				b._targetArray [2] [b._targetIndex[2]] = o4;
				b._targetArray [3] [b._targetIndex[3]] = o5;
				b._targetArray [4] [b._targetIndex[4]] = o6;
				b._targetArray [5] [b._targetIndex[5]] = o7;
			} else {
				b._args [0] = o2;
				b._args [1] = o3;
				b._args [2] = o4;
				b._args [3] = o5;
				b._args [4] = o6;
				b._args [5] = o7;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			b._d.DynamicInvoke (outArgs);
		}
		
		public static void Action8 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 8);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;
				b._targetArray [2] [b._targetIndex[2]] = o4;
				b._targetArray [3] [b._targetIndex[3]] = o5;
				b._targetArray [4] [b._targetIndex[4]] = o6;
				b._targetArray [5] [b._targetIndex[5]] = o7;
				b._targetArray [6] [b._targetIndex[6]] = o8;
			} else {
				b._args [0] = o2;
				b._args [1] = o3;
				b._args [2] = o4;
				b._args [3] = o5;
				b._args [4] = o6;
				b._args [5] = o7;
				b._args [6] = o8;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			b._d.DynamicInvoke (outArgs);
		}
		
		public static void Action9 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8, object o9)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 9);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;
				b._targetArray [2] [b._targetIndex[2]] = o4;
				b._targetArray [3] [b._targetIndex[3]] = o5;
				b._targetArray [4] [b._targetIndex[4]] = o6;
				b._targetArray [5] [b._targetIndex[5]] = o7;
				b._targetArray [6] [b._targetIndex[6]] = o8;
				b._targetArray [7] [b._targetIndex[7]] = o9;
			} else {
				b._args [0] = o2;
				b._args [1] = o3;
				b._args [2] = o4;
				b._args [3] = o5;
				b._args [4] = o6;
				b._args [5] = o7;
				b._args [6] = o8;
				b._args [7] = o9;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			b._d.DynamicInvoke (outArgs);
		}
		
		public static void Action10 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8, object o9, object o10)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 10);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;
				b._targetArray [2] [b._targetIndex[2]] = o4;
				b._targetArray [3] [b._targetIndex[3]] = o5;
				b._targetArray [4] [b._targetIndex[4]] = o6;
				b._targetArray [5] [b._targetIndex[5]] = o7;
				b._targetArray [6] [b._targetIndex[6]] = o8;
				b._targetArray [7] [b._targetIndex[7]] = o9;
				b._targetArray [8] [b._targetIndex[7]] = o10;
			} else {
				b._args [0] = o2;
				b._args [1] = o3;
				b._args [2] = o4;
				b._args [3] = o5;
				b._args [4] = o6;
				b._args [5] = o7;
				b._args [6] = o8;
				b._args [7] = o9;
				b._args [8] = o10;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			b._d.DynamicInvoke (outArgs);
		}

		public static object Func1 (CallSite site, object o1)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 1);
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return b._d.DynamicInvoke (outArgs);
		}

		public static object Func2 (CallSite site, object o1, object o2)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 2);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
			} else {
				b._args [0] = o2;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return b._d.DynamicInvoke (outArgs);
		}
		
		public static object Func3 (CallSite site, object o1, object o2, object o3)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 3);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;
			} else {
				b._args [0] = o2;
				b._args [1] = o3;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return b._d.DynamicInvoke (outArgs);
		}
		
		public static object Func4 (CallSite site, object o1, object o2, object o3, object o4)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 4);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;
				b._targetArray [2] [b._targetIndex[2]] = o4;
			} else {
				b._args [0] = o2;
				b._args [1] = o3;
				b._args [2] = o4;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return b._d.DynamicInvoke (outArgs);
		}
		
		public static object Func5 (CallSite site, object o1, object o2, object o3, object o4, object o5)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 5);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;
				b._targetArray [2] [b._targetIndex[2]] = o4;
				b._targetArray [3] [b._targetIndex[3]] = o5;
			} else {
				b._args [0] = o2;
				b._args [1] = o3;
				b._args [2] = o4;
				b._args [3] = o5;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return b._d.DynamicInvoke (outArgs);
		}
		
		public static object Func6 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 6);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;
				b._targetArray [2] [b._targetIndex[2]] = o4;
				b._targetArray [3] [b._targetIndex[3]] = o5;
				b._targetArray [4] [b._targetIndex[4]] = o6;
			} else {
				b._args [0] = o2;
				b._args [1] = o3;
				b._args [2] = o4;
				b._args [3] = o5;
				b._args [4] = o6;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return b._d.DynamicInvoke (outArgs);
		}
		
		public static object Func7 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 7);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;
				b._targetArray [2] [b._targetIndex[2]] = o4;
				b._targetArray [3] [b._targetIndex[3]] = o5;
				b._targetArray [4] [b._targetIndex[4]] = o6;
				b._targetArray [5] [b._targetIndex[5]] = o7;
			} else {
				b._args [0] = o2;
				b._args [1] = o3;
				b._args [2] = o4;
				b._args [3] = o5;
				b._args [4] = o6;
				b._args [5] = o7;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return b._d.DynamicInvoke (outArgs);
		}
		
		public static object Func8 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 8);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;
				b._targetArray [2] [b._targetIndex[2]] = o4;
				b._targetArray [3] [b._targetIndex[3]] = o5;
				b._targetArray [4] [b._targetIndex[4]] = o6;
				b._targetArray [5] [b._targetIndex[5]] = o7;
				b._targetArray [6] [b._targetIndex[6]] = o8;
			} else {
				b._args [0] = o2;
				b._args [1] = o3;
				b._args [2] = o4;
				b._args [3] = o5;
				b._args [4] = o6;
				b._args [5] = o7;
				b._args [6] = o8;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return b._d.DynamicInvoke (outArgs);
		}
		
		public static object Func9 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8, object o9)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 9);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;
				b._targetArray [2] [b._targetIndex[2]] = o4;
				b._targetArray [3] [b._targetIndex[3]] = o5;
				b._targetArray [4] [b._targetIndex[4]] = o6;
				b._targetArray [5] [b._targetIndex[5]] = o7;
				b._targetArray [6] [b._targetIndex[6]] = o8;
				b._targetArray [7] [b._targetIndex[7]] = o9;
			} else {
				b._args [0] = o2;
				b._args [1] = o3;
				b._args [2] = o4;
				b._args [3] = o5;
				b._args [4] = o6;
				b._args [5] = o7;
				b._args [6] = o8;
				b._args [7] = o9;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return b._d.DynamicInvoke (outArgs);
		}
		
		public static object Func10 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8, object o9, object o10)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			CSharpInvokeBinder b = (CSharpInvokeBinder)site.Binder;
			if ((Delegate)o1 != b._d) {
				b.UpdateInvokeInfo ((Delegate)o1, 10);
			}
			if (b._params != null) {
				b._targetArray [0] [b._targetIndex[0]] = o2;
				b._targetArray [1] [b._targetIndex[1]] = o3;
				b._targetArray [2] [b._targetIndex[2]] = o4;
				b._targetArray [3] [b._targetIndex[3]] = o5;
				b._targetArray [4] [b._targetIndex[4]] = o6;
				b._targetArray [5] [b._targetIndex[5]] = o7;
				b._targetArray [6] [b._targetIndex[6]] = o8;
				b._targetArray [7] [b._targetIndex[7]] = o9;
				b._targetArray [8] [b._targetIndex[8]] = o10;
			} else {
				b._args [0] = o2;
				b._args [1] = o3;
				b._args [2] = o4;
				b._args [3] = o5;
				b._args [4] = o6;
				b._args [5] = o7;
				b._args [6] = o8;
				b._args [7] = o9;
				b._args [8] = o10;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(b._d.Method, b._args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return b._d.DynamicInvoke (outArgs);
		}

		public void UpdateInvokeInfo(Delegate d, int callArgs)
		{
			int args = callArgs - 1;
			var t = d.GetType ();

			_d = d;

			// Set up args arrays
			if (t.Namespace == "PlayScript") {
				bool isActionP = t.Name.StartsWith ("ActionP");
				bool isFuncP = t.Name.StartsWith ("FuncP");
				int argsP = 1;
				if (isActionP || isFuncP) { 
					if (t.IsGenericType) {
						argsP += t.GetGenericArguments ().Length;
					}
					if (isFuncP) {
						argsP--;
					}
					_args = new object[argsP];
					_params = new object[args - (argsP - 1)];
					_args [argsP - 1] = _params;
					_targetArray = new object[args][];
					_targetIndex = new int[args];
					for (int i = 0; i < args; i++) {
						if (i < argsP - 1) {
							_targetArray [i] = _args;
							_targetIndex [i] = i;
						} else {
							_targetArray [i] = _params;
							_targetIndex [i] = i - (argsP - 1);
						}
					}
				}
			} else {
				_args = new object[args];
				_params = null;
			}
		}

		public CSharpInvokeBinder (CSharpBinderFlags flags, Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
//			this.flags = flags;
//			this.callingContext = callingContext;
//			this.argumentInfo = new List<CSharpArgumentInfo>(argumentInfo);
		}

		static CSharpInvokeBinder ()
		{
			delegates.Add (typeof(Action<CallSite, object>), (Action<CallSite, object>)Action1);
			delegates.Add (typeof(Action<CallSite, object, object>), (Action<CallSite, object, object>)Action2);
			delegates.Add (typeof(Action<CallSite, object, object, object>), (Action<CallSite, object, object, object>)Action3);
			delegates.Add (typeof(Action<CallSite, object, object, object, object>),   (Action<CallSite, object, object, object, object>)Action4);
			delegates.Add (typeof(Action<CallSite, object, object, object, object, object>), (Action<CallSite, object, object, object, object, object>)Action5);
			delegates.Add (typeof(Action<CallSite, object, object, object, object, object, object>), (Action<CallSite, object, object, object, object, object, object>)Action6);
			delegates.Add (typeof(Action<CallSite, object, object, object, object, object, object, object>), (Action<CallSite, object, object, object, object, object, object, object>)Action7);
			delegates.Add (typeof(Action<CallSite, object, object, object, object, object, object, object, object>), (Action<CallSite, object, object, object, object, object, object, object, object>)Action8);
			delegates.Add (typeof(Action<CallSite, object, object, object, object, object, object, object, object, object>), (Action<CallSite, object, object, object, object, object, object, object, object, object>)Action9);
			delegates.Add (typeof(Action<CallSite, object, object, object, object, object, object, object, object, object, object>), (Action<CallSite, object, object, object, object, object, object, object, object, object, object>)Action10);

			delegates.Add (typeof(Func<CallSite, object, object>), (Func<CallSite, object, object>)Func1);
			delegates.Add (typeof(Func<CallSite, object, object, object>), (Func<CallSite, object, object, object>)Func2);
			delegates.Add (typeof(Func<CallSite, object, object, object, object>),   (Func<CallSite, object, object, object, object>)Func3);
			delegates.Add (typeof(Func<CallSite, object, object, object, object, object>), (Func<CallSite, object, object, object, object, object>)Func4);
			delegates.Add (typeof(Func<CallSite, object, object, object, object, object, object>), (Func<CallSite, object, object, object, object, object, object>)Func5);
			delegates.Add (typeof(Func<CallSite, object, object, object, object, object, object, object>), (Func<CallSite, object, object, object, object, object, object, object>)Func6);
			delegates.Add (typeof(Func<CallSite, object, object, object, object, object, object, object, object>), (Func<CallSite, object, object, object, object, object, object, object, object>)Func7);
			delegates.Add (typeof(Func<CallSite, object, object, object, object, object, object, object, object, object>), (Func<CallSite, object, object, object, object, object, object, object, object, object>)Func8);
			delegates.Add (typeof(Func<CallSite, object, object, object, object, object, object, object, object, object, object>), (Func<CallSite, object, object, object, object, object, object, object, object, object, object>)Func9);
			delegates.Add (typeof(Func<CallSite, object, object, object, object, object, object, object, object, object, object, object>), (Func<CallSite, object, object, object, object, object, object, object, object, object, object, object>)Func10);
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
