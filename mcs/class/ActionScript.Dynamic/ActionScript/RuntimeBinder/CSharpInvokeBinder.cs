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

namespace ActionScript.RuntimeBinder
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

using System;
using ActionScript;
using System.Collections.Generic;

namespace ActionScript.RuntimeBinder
{
	class CSharpInvokeBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> delegates = new Dictionary<Type, object>();

//		readonly CSharpBinderFlags flags;
//		List<CSharpArgumentInfo> argumentInfo;
//		Type callingContext;

		public static void Action1 (CallSite site, object o1)
		{
			((Delegate)o1).DynamicInvoke(null);
		}
		
		public static void Action2 (CallSite site, object o1, object o2)
		{
			((Delegate)o1).DynamicInvoke(new [] { o2 });
		}
		
		public static void Action3 (CallSite site, object o1, object o2, object o3)
		{
			((Delegate)o1).DynamicInvoke(new [] { o2, o3 });
		}
		
		public static void Action4 (CallSite site, object o1, object o2, object o3, object o4)
		{
			((Delegate)o1).DynamicInvoke(new [] { o2, o3, o4 });
		}
		
		public static void Action5 (CallSite site, object o1, object o2, object o3, object o4, object o5)
		{
			((Delegate)o1).DynamicInvoke(new [] { o2, o3, o4, o5 });
		}
		
		public static void Action6 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6)
		{
			((Delegate)o1).DynamicInvoke(new [] { o2, o3, o4, o5, o6 });
		}
		
		public static void Action7 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7)
		{
			((Delegate)o1).DynamicInvoke(new [] { o2, o3, o4, o5, o6, o7 });
		}
		
		public static void Action8 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8)
		{
			((Delegate)o1).DynamicInvoke(new [] { o2, o3, o4, o5, o6, o7, o8 });
		}
		
		public static void Action9 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8, object o9)
		{
			((Delegate)o1).DynamicInvoke(new [] { o2, o3, o4, o5, o6, o7, o8, o9 });
		}
		
		public static void Action10 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8, object o9, object o10)
		{
			((Delegate)o1).DynamicInvoke(new [] { o2, o3, o4, o5, o6, o7, o8, o10 });
		}

		public static object Func1 (CallSite site, object o1)
		{
			return ((Delegate)o1).DynamicInvoke(null);
		}

		public static object Func2 (CallSite site, object o1, object o2)
		{
			return ((Delegate)o1).DynamicInvoke(new [] { o2 });
		}
		
		public static object Func3 (CallSite site, object o1, object o2, object o3)
		{
			return ((Delegate)o1).DynamicInvoke(new [] { o2, o3 });
		}
		
		public static object Func4 (CallSite site, object o1, object o2, object o3, object o4)
		{
			return ((Delegate)o1).DynamicInvoke(new [] { o2, o3, o4 });
		}
		
		public static object Func5 (CallSite site, object o1, object o2, object o3, object o4, object o5)
		{
			return ((Delegate)o1).DynamicInvoke(new [] { o2, o3, o4, o5 });
		}
		
		public static object Func6 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6)
		{
			return ((Delegate)o1).DynamicInvoke(new [] { o2, o3, o4, o5, o6 });
		}
		
		public static object Func7 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7)
		{
			return ((Delegate)o1).DynamicInvoke(new [] { o2, o3, o4, o5, o6, o7 });
		}
		
		public static object Func8 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8)
		{
			return ((Delegate)o1).DynamicInvoke(new [] { o2, o3, o4, o5, o6, o7, o8});
		}
		
		public static object Func9 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8, object o9)
		{
			return ((Delegate)o1).DynamicInvoke(new [] { o2, o3, o4, o5, o6, o7, o8, o9 });
		}
		
		public static object Func10 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8, object o9, object o10)
		{
			return ((Delegate)o1).DynamicInvoke(new [] { o2, o3, o4, o5, o6, o7, o8, o9, o10 });
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
