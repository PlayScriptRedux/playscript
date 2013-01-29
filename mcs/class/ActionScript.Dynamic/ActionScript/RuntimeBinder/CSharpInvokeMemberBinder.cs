//
// CSharpInvokeMemberBinder.cs
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
using SLE = System.Linq.Expressions;

namespace ActionScript.RuntimeBinder
{
	class CSharpInvokeMemberBinder : InvokeMemberBinder
	{
		//
		// A custom runtime invocation is needed to deal with member invocation which
		// is not real member invocation but invocation on invocalble member.
		//
		// An example:
		// class C {
		//		dynamic f;
		//		void Foo ()
		//		{
		//			dynamic d = new C ();
		//			d.f.M ();
		//		}
		//
		// The runtime value of `f' can be a delegate in which case we are invoking result
		// of member invocation, this is already handled by DoResolveDynamic but we need
		// more runtime dependencies which require Microsoft.CSharp assembly reference or
		// a lot of reflection calls
		//
		class Invocation : Compiler.Invocation
		{
			sealed class RuntimeDynamicInvocation : Compiler.ShimExpression
			{
				Invocation invoke;

				public RuntimeDynamicInvocation (Invocation invoke, Compiler.Expression memberExpr)
					: base (memberExpr)
				{
					this.invoke = invoke;
				}

				protected override Compiler.Expression DoResolve (Compiler.ResolveContext rc)
				{
					type = expr.Type;
					eclass = Compiler.ExprClass.Value;
					return this;
				}

				//
				// Creates an invoke call on invocable expression
				//
				public override System.Linq.Expressions.Expression MakeExpression (Compiler.BuilderContext ctx)
				{
					var invokeBinder = invoke.invokeBinder;
					var binder = Binder.Invoke (invokeBinder.flags, invokeBinder.callingContext, invokeBinder.argumentInfo);

					var args = invoke.Arguments;
					var args_expr = new SLE.Expression[invokeBinder.argumentInfo.Count];

					var types = new Type [args_expr.Length + 2];

					// Required by MakeDynamic
					types[0] = typeof (System.Runtime.CompilerServices.CallSite);
					types[1] = expr.Type.GetMetaInfo ();

					args_expr[0] = expr.MakeExpression (ctx);

					for (int i = 0; i < args.Count; ++i) {
						args_expr[i + 1] = args[i].Expr.MakeExpression (ctx);

						int type_index = i + 2;
						types[type_index] = args[i].Type.GetMetaInfo ();
						if (args[i].IsByRef)
							types[type_index] = types[type_index].MakeByRefType ();
					}

					// Return type goes last
					bool void_result = (invokeBinder.flags & CSharpBinderFlags.ResultDiscarded) != 0;
					types[types.Length - 1] = void_result ? typeof (void) : invokeBinder.ReturnType;

					//
					// Much easier to use Expression.Dynamic cannot be used because it ignores ByRef arguments
					// and it always generates either Func or Action and any value type argument is lost
					//
					Type delegateType = SLE.Expression.GetDelegateType (types);
					return SLE.Expression.MakeDynamic (delegateType, binder, args_expr);
				}
			}

			readonly CSharpInvokeMemberBinder invokeBinder;

			public Invocation (Compiler.Expression expr, Compiler.Arguments arguments, CSharpInvokeMemberBinder invokeBinder)
				: base (expr, arguments)
			{
				this.invokeBinder = invokeBinder;
			}

			protected override Compiler.Expression DoResolveDynamic (Compiler.ResolveContext ec, Compiler.Expression memberExpr)
			{
				return new RuntimeDynamicInvocation (this, memberExpr).Resolve (ec);
			}
		}

		readonly CSharpBinderFlags flags;
		IList<CSharpArgumentInfo> argumentInfo;
		IList<Type> typeArguments;
		Type callingContext;
		
		public CSharpInvokeMemberBinder (CSharpBinderFlags flags, string name, Type callingContext, IEnumerable<Type> typeArguments, IEnumerable<CSharpArgumentInfo> argumentInfo)
			: base (name, false, CSharpArgumentInfo.CreateCallInfo (argumentInfo, 1))
		{
			this.flags = flags;
			this.callingContext = callingContext;
			this.argumentInfo = argumentInfo.ToReadOnly ();
			this.typeArguments = typeArguments.ToReadOnly ();
		}
		
		public override DynamicMetaObject FallbackInvoke (DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
		{
			var b = new CSharpInvokeBinder (flags, callingContext, argumentInfo);
			
			// TODO: Is errorSuggestion ever used?
			return b.Defer (target, args);
		}
		
		public override DynamicMetaObject FallbackInvokeMember (DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
		{
			var ctx = DynamicContext.Create ();
			var c_args = ctx.CreateCompilerArguments (argumentInfo.Skip (1), args);
			var t_args = typeArguments == null ?
				null :
				new Compiler.TypeArguments (typeArguments.Select (l => new Compiler.TypeExpression (ctx.ImportType (l), Compiler.Location.Null)).ToArray ());

			var expr = ctx.CreateCompilerExpression (argumentInfo[0], target);

			//
			// Simple name invocation is actually member access invocation
 			// to capture original this argument. This  brings problem when
			// simple name is resolved as a static invocation and member access
			// has to be reduced back to simple name without reporting an error
			//
			if ((flags & CSharpBinderFlags.InvokeSimpleName) != 0) {
				var value = expr as Compiler.RuntimeValueExpression;
				if (value != null)
					value.IsSuggestionOnly = true;
			}

			expr = new Compiler.MemberAccess (expr, Name, t_args, Compiler.Location.Null);
			expr = new Invocation (expr, c_args, this);

			if ((flags & CSharpBinderFlags.ResultDiscarded) == 0)
				expr = new Compiler.Cast (new Compiler.TypeExpression (ctx.ImportType (ReturnType), Compiler.Location.Null), expr, Compiler.Location.Null);
			else
				expr = new Compiler.DynamicResultCast (ctx.ImportType (ReturnType), expr);

			var binder = new CSharpBinder (this, expr, errorSuggestion);
			binder.AddRestrictions (target);
			binder.AddRestrictions (args);

			if ((flags & CSharpBinderFlags.InvokeSpecialName) != 0)
				binder.ResolveOptions |= Compiler.ResolveContext.Options.InvokeSpecialName;

			return binder.Bind (ctx, callingContext);
		}
	}
}

#else

using System;
using System.Collections.Generic;
using ActionScript.Expando;
using System.Reflection;

namespace ActionScript.RuntimeBinder
{
	class CSharpInvokeMemberBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> invokeTargets = new Dictionary<Type, object>();

		readonly string name;
//		readonly CSharpBinderFlags flags;
//		List<CSharpArgumentInfo> argumentInfo;
//		List<Type> typeArguments;
//		Type callingContext;
		
		public CSharpInvokeMemberBinder (CSharpBinderFlags flags, string name, Type callingContext, IEnumerable<Type> typeArguments, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			this.name = name;
//			this.flags = flags;
//			this.callingContext = callingContext;
//			this.argumentInfo = new List<CSharpArgumentInfo>(argumentInfo);
//			this.typeArguments = new List<Type>(typeArguments);
		}

		public void FindMethod (CallSite site, object o)
		{
			if (o == null) {
				throw new NullReferenceException ();
			}

			CallSite.InvokeInfo info = site.invokeInfo;
			if (info == null) {
				info = new CallSite.InvokeInfo();
				info.lastObj = new WeakReference (o);
				site.invokeInfo = info;
			} else {
				site.invokeInfo.lastObj.Target = o;
				object lastObj = site.invokeInfo.lastObj.Target;
				if (!(lastObj is ExpandoObject) && lastObj.GetType () == o.GetType ()) {
					return;
				}
			}

			if (o is ExpandoObject) {
				object delObj;
				var expando = (ExpandoObject)o;
				expando.TryGetValue(name, out delObj);
				Delegate del = delObj as Delegate;
				if (del == null) {
					throw new Exception ("No delegate found with the name '" + name + "'");
				}
				info.method = null;
				info.del = del;
				info.generation = expando.Generation;
			} else {
				MethodInfo method = null;
				var methods = o.GetType().GetMethods();
				var len = methods.Length;
				for (var mi = 0; mi < len; mi++) {
					var testMeth = methods[mi];
					if (testMeth.IsPublic && !testMeth.IsStatic && testMeth.Name == name) {
						method = testMeth;
						break;
					}
				}
				if (method == null) {
					throw new Exception("No method found with the name '" + name + "'"); 
				}
				info.method = method;
				info.del = null;
				info.generation = 0;
			}

		}

		private static void InvokeAction (CallSite site, object o)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				info.method.Invoke (o, null);
			else 
				info.del.DynamicInvoke(null);
		}

		private static void InvokeAction1 (CallSite site, object o, object a1)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				info.method.Invoke (o, new [] { a1 });
			else 
				info.del.DynamicInvoke(new [] { a1 });
		}

		private static void InvokeAction2 (CallSite site, object o, object a1, object a2)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				info.method.Invoke (o, new [] { a1, a2 });
			else 
				info.del.DynamicInvoke(null, new [] { a1, a2 });
		}

		private static void InvokeAction3 (CallSite site, object o, object a1, object a2, object a3)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				info.method.Invoke (o, new [] { a1, a2, a3 });
			else 
				info.del.DynamicInvoke(null, new [] { a1, a2, a3 });
		}

		private static void InvokeAction4 (CallSite site, object o, object a1, object a2, object a3, object a4)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				info.method.Invoke (o, new [] { a1, a2, a3, a4 });
			else 
				info.del.DynamicInvoke(null, new [] { a1, a2, a3, a4 });
		}

		private static void InvokeAction5 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                            object a5)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				info.method.Invoke (o, new [] { a1, a2, a3, a4, a5 });
			else 
				info.del.DynamicInvoke(null, new [] { a1, a2, a3, a4, a5 });
		}
		
		private static void InvokeAction6 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                            object a5, object a6)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				info.method.Invoke (o, new [] { a1, a2, a3, a4, a5, a6 });
			else 
				info.del.DynamicInvoke(null, new [] { a1, a2, a3, a4, a5, a6 });
		}
		
		private static void InvokeAction7 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                            object a5, object a6, object a7)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				info.method.Invoke (o, new [] { a1, a2, a3, a4, a5, a6, a7 });
			else 
				info.del.DynamicInvoke(null, new [] { a1, a2, a3, a4, a5, a6, a7 });
		}
		
		private static void InvokeAction8 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                            object a5, object a6, object a7, object a8)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				info.method.Invoke (o, new [] { a1, a2, a3, a4, a5, a6, a7, a8 });
			else 
				info.del.DynamicInvoke(null, new [] { a1, a2, a3, a4, a5, a6, a7, a8 });
		}

		private static object InvokeFunc (CallSite site, object o)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				return info.method.Invoke (o, null);
			else 
				return info.del.DynamicInvoke(null);
		}
		
		private static object InvokeFunc1 (CallSite site, object o, object a1)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				return info.method.Invoke (o, new [] { a1 });
			else 
				return info.del.DynamicInvoke(new [] { a1 });
		}
		
		private static object InvokeFunc2 (CallSite site, object o, object a1, object a2)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				return info.method.Invoke (o, new [] { a1, a2 });
			else 
				return info.del.DynamicInvoke(null, new [] { a1, a2 });
		}
		
		private static object InvokeFunc3 (CallSite site, object o, object a1, object a2, object a3)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				return info.method.Invoke (o, new [] { a1, a2, a3 });
			else 
				return info.del.DynamicInvoke(null, new [] { a1, a2, a3 });
		}
		
		private static object InvokeFunc4 (CallSite site, object o, object a1, object a2, object a3, object a4)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				return info.method.Invoke (o, new [] { a1, a2, a3, a4 });
			else 
				return info.del.DynamicInvoke(null, new [] { a1, a2, a3, a4 });
		}
		
		private static object InvokeFunc5 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                     object a5)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				return info.method.Invoke (o, new [] { a1, a2, a3, a4, a5 });
			else 
				return info.del.DynamicInvoke(null, new [] { a1, a2, a3, a4, a5 });
		}
		
		private static object InvokeFunc6 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                     object a5, object a6)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				return info.method.Invoke (o, new [] { a1, a2, a3, a4, a5, a6 });
			else 
				return info.del.DynamicInvoke(null, new [] { a1, a2, a3, a4, a5, a6 });
		}
		
		private static object InvokeFunc7 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                     object a5, object a6, object a7)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				return info.method.Invoke (o, new [] { a1, a2, a3, a4, a5, a6, a7 });
			else 
				return info.del.DynamicInvoke(null, new [] { a1, a2, a3, a4, a5, a6, a7 });
		}
		
		private static object InvokeFunc8 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                     object a5, object a6, object a7, object a8)
		{
			var info = site.invokeInfo;
			if (info == null || info.lastObj.Target != o || 
			    (o is ExpandoObject && ((ExpandoObject)o).Generation != info.generation)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o);
				info = site.invokeInfo;
			}
			if (info.method != null) 
				return info.method.Invoke (o, new [] { a1, a2, a3, a4, a5, a6, a7, a8 });
			else 
				return info.del.DynamicInvoke(null, new [] { a1, a2, a3, a4, a5, a6, a7, a8 });
		}

		static CSharpInvokeMemberBinder ()
		{
			invokeTargets.Add (typeof(Action<CallSite,object>), 
			                   (Action<CallSite,object>)InvokeAction);
			invokeTargets.Add (typeof(Action<CallSite,object,object>), 
			                   (Action<CallSite,object,object>)InvokeAction1);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object>), 
			                   (Action<CallSite,object,object,object>)InvokeAction2);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object>)InvokeAction3);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object,object>)InvokeAction4);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object,object,object>)InvokeAction5);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object,object,object,object>)InvokeAction6);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object,object,object,object,object>)InvokeAction7);
			invokeTargets.Add (typeof(Action<CallSite,object,object,object,object,object,object,object,object,object>), 
			                   (Action<CallSite,object,object,object,object,object,object,object,object,object>)InvokeAction8);
			invokeTargets.Add (typeof(Func<CallSite,object,object>), 
			                   (Func<CallSite,object,object>)InvokeFunc);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object>), 
			                   (Func<CallSite,object,object,object>)InvokeFunc1);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object>)InvokeFunc2);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object>)InvokeFunc3);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object,object>)InvokeFunc4);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object,object,object>)InvokeFunc5);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object,object,object,object>)InvokeFunc6);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object,object,object,object,object>)InvokeFunc7);
			invokeTargets.Add (typeof(Func<CallSite,object,object,object,object,object,object,object,object,object,object>), 
			                   (Func<CallSite,object,object,object,object,object,object,object,object,object,object>)InvokeFunc8);
		}

		public override object Bind (Type delegateType)
		{
			object target;
			invokeTargets.TryGetValue(delegateType, out target);
			return target;
		}
	}
}


#endif