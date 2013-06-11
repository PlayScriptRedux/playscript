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

namespace PlayScript.RuntimeBinder
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
using PlayScript.Expando;
using System.Reflection;

namespace PlayScript.RuntimeBinder
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

		public void FindMethod (CallSite site, object o, object[] args)
		{
			if (o == null) {
				throw new NullReferenceException ();
			}

			CallSite.InvokeInfo info = site.invokeInfo;
			if (info == null) {
				info = new CallSite.InvokeInfo();
				info.lastObj = new WeakReference (o);
				info.lastArgTypes = new Type[args.Length];
				site.invokeInfo = info;
			} else {
				site.invokeInfo.lastObj.Target = o;
			}

			var arg_len = args.Length;
			for (var i = 0; i < arg_len; i++) {
				info.lastArgTypes[i] = (args != null && args[i]!=null) ? args[i].GetType () : null;
			}

			if (o is ExpandoObject) {
				var expando = (ExpandoObject)o;
				// special case .hasOwnProperty here
				if (name == "hasOwnProperty")
				{
					info.method = o.GetType().GetMethod("hasOwnProperty");
					info.args = args;
					info.del = null;
					info.generation = 0;
				}
				else
				{
					object delObj;
					expando.TryGetValue(name, out delObj);
					Delegate del = delObj as Delegate;
					if (del == null) {
						throw new Exception ("No delegate found with the name '" + name + "'");
					}
					info.method = null;
					info.del = del;
					info.generation = expando.Generation;
				}
			} else {
				MethodInfo method = null;
				bool isStatic;
				System.Type otype;
				if (o is System.Type) {
					// this is a static method invocation where o is the class
					isStatic = true;
					otype = (System.Type)o;
				} else {
					// this is a non-static method invocation
					isStatic = false;
					otype = o.GetType();
				}

				MethodInfo[] methods = otype.GetMethods();
				var len = methods.Length;
				for (var mi = 0; mi < len; mi++) {
					var m = methods[mi];
					if ((m.IsStatic == isStatic) && m.Name == name) {
						bool matches = true;
						bool has_defaults = false;
						var parameters = m.GetParameters();
						var par_len = parameters.Length;
						if (par_len >= arg_len) {
							for (var i = 0; i < par_len; i++) {
								var p = parameters[i];
								if (i >= args.Length) {
									if ((p.Attributes & ParameterAttributes.HasDefault) != 0) {
										has_defaults = true;
										continue;
									} else {
										matches = false;
										break;
									}
								} else {
									var ptype = p.ParameterType;
									if (args[i] != null) {
										if (!ptype.IsAssignableFrom(args[i].GetType ())) {
											matches = false;
											break;
										}
									} else if (!ptype.IsClass || ptype == typeof(string)) {
										matches = false;
										break;
									}
								}
							}
						}
						if (matches) {
							if (has_defaults) {
								var new_args = new object[par_len];
								for (var j = 0; j < par_len; j++) {
									if (j < args.Length)
										new_args[j] = args[j];
									else
										new_args[j] = parameters[j].DefaultValue;
								}
								args = new_args;
							}
							method = m;
							break;
						}
					}
				}
				if (method == null) {
					throw new Exception("No matching method found with the name '" + name + "'"); 
				}
				info.method = method;
				info.args = args;
				info.del = null;
				info.generation = 0;
			}

		}

		private static void InvokeAction (CallSite site, object o)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches(o)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new object [] {});
				info = site.invokeInfo;
			}
			args = info.args;
			if (info.method != null) 
				info.method.Invoke (o, null);
			else 
				info.del.DynamicInvoke(null);
		}

		private static void InvokeAction1 (CallSite site, object o, object a1)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches (o, a1)) {
				var new_args = new [] { a1 };
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1;
			}
			if (info.method != null) 
				info.method.Invoke (o, args);
			else 
				info.del.DynamicInvoke(args);
			args[0] = null;
		}

		private static void InvokeAction2 (CallSite site, object o, object a1, object a2)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches(o, a1, a2)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1, a2 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1; args[1] = a2;
			}
			if (info.method != null) 
				info.method.Invoke (o, args);
			else 
				info.del.DynamicInvoke(null, args);
			args[0] = args[1] = null;
		}

		private static void InvokeAction3 (CallSite site, object o, object a1, object a2, object a3)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches(o, a1, a2, a3)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1, a2, a3 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1; args[1] = a2; args[2] = a3;
			}
			if (info.method != null) 
				info.method.Invoke (o, args);
			else 
				info.del.DynamicInvoke(null, args);
			args[0] = args[1] = args[2] = null;
		}

		private static void InvokeAction4 (CallSite site, object o, object a1, object a2, object a3, object a4)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches(o, a1, a2, a3, a4)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1, a2, a3, a4 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1; args[1] = a2; args[2] = a3; args[3] = a4;
			}
			if (info.method != null) 
				info.method.Invoke (o, args);
			else 
				info.del.DynamicInvoke(null, args);
			args[0] = args[1] = args[2] = args[3] = null;
		}

		private static void InvokeAction5 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                            object a5)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches(o, a1, a2, a3, a4, a5)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1, a2, a3, a4, a5 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1; args[1] = a2; args[2] = a3; args[3] = a4; args[4] = a5;
			}
			if (info.method != null) 
				info.method.Invoke (o, args);
			else 
				info.del.DynamicInvoke(null, args);
			args[0] = args[1] = args[2] = args[3] = args[4] = null;
		}
		
		private static void InvokeAction6 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                            object a5, object a6)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches(o, a1, a2, a3, a4, a5, a6)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1, a2, a3, a4, a5, a6 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1; args[1] = a2; args[2] = a3; args[3] = a4; args[4] = a5; args[5] = a6;
			}
			if (info.method != null) 
				info.method.Invoke (o, args);
			else 
				info.del.DynamicInvoke(null, args);
			args[0] = args[1] = args[2] = args[3] = args[4] = args[5] = null;
		}
		
		private static void InvokeAction7 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                            object a5, object a6, object a7)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches(o, a1, a2, a3, a4, a5, a6, a7)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1, a2, a3, a4, a5, a6, a7 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1; args[1] = a2; args[2] = a3; args[3] = a4; args[4] = a5; args[5] = a6; args[6] = a7;
			}
			if (info.method != null) 
				info.method.Invoke (o, args);
			else 
				info.del.DynamicInvoke(null, args);
			args[0] = args[1] = args[2] = args[3] = args[4] = args[5] = args[6] = null;
		}
		
		private static void InvokeAction8 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                            object a5, object a6, object a7, object a8)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches(o, a1, a2, a3, a4, a5, a6, a7, a8)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1, a2, a3, a4, a5, a6, a7, a8 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1; args[1] = a2; args[2] = a3; args[3] = a4; args[4] = a5; args[5] = a6; args[6] = a7; args[7] = a8;
			}
			if (info.method != null) 
				info.method.Invoke (o, args);
			else 
				info.del.DynamicInvoke(null, args);
			args[0] = args[1] = args[2] = args[3] = args[4] = args[5] = args[6] = args[7] = null;
		}

		private static object InvokeFunc (CallSite site, object o)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches (o)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new object[] {});
				info = site.invokeInfo;
			}
			args = info.args;
			if (info.method != null) 
				return info.method.Invoke (o, null);
			else 
				return info.del.DynamicInvoke(null);
		}

		private static object InvokeFunc1 (CallSite site, object o, object a1)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches (o, a1)) {
				var new_args = new [] { a1 };
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1;
			}
			object ret;
			if (info.method != null) 
				ret = info.method.Invoke (o, args);
			else 
				ret = info.del.DynamicInvoke(args);
			args[0] = null;
			return ret;
		}
		
		private static object InvokeFunc2 (CallSite site, object o, object a1, object a2)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches(o, a1, a2)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1, a2 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1; args[1] = a2;
			}
			object ret;
			if (info.method != null) 
				ret = info.method.Invoke (o, args);
			else 
				ret = info.del.DynamicInvoke(null, args);
			args[0] = args[1] = null;
			return ret;
		}
		
		private static object InvokeFunc3 (CallSite site, object o, object a1, object a2, object a3)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches(o, a1, a2, a3)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1, a2, a3 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1; args[1] = a2; args[2] = a3;
			}
			object ret;
			if (info.method != null) 
				ret = info.method.Invoke (o, args);
			else 
				ret = info.del.DynamicInvoke(null, args);
			args[0] = args[1] = args[2] = null;
			return ret;
		}
		
		private static object InvokeFunc4 (CallSite site, object o, object a1, object a2, object a3, object a4)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches(o, a1, a2, a3, a4)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1, a2, a3, a4 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1; args[1] = a2; args[2] = a3; args[3] = a4;
			}
			object ret;
			if (info.method != null) 
				ret = info.method.Invoke (o, args);
			else 
				ret = info.del.DynamicInvoke(null, args);
			args[0] = args[1] = args[2] = args[3] = null;
			return ret;
		}
		
		private static object InvokeFunc5 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                                   object a5)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches(o, a1, a2, a3, a4, a5)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1, a2, a3, a4, a5 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1; args[1] = a2; args[2] = a3; args[3] = a4; args[4] = a5;
			}
			object ret;
			if (info.method != null) 
				ret = info.method.Invoke (o, args);
			else 
				ret = info.del.DynamicInvoke(null, args);
			args[0] = args[1] = args[2] = args[3] = args[4] = null;
			return ret;
		}
		
		private static object InvokeFunc6 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                                   object a5, object a6)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches(o, a1, a2, a3, a4, a5, a6)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1, a2, a3, a4, a5, a6 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1; args[1] = a2; args[2] = a3; args[3] = a4; args[4] = a5; args[5] = a6;
			}
			object ret;
			if (info.method != null) 
				ret = info.method.Invoke (o, args);
			else 
				ret = info.del.DynamicInvoke(null, args);
			args[0] = args[1] = args[2] = args[3] = args[4] = args[5] = null;
			return ret;
		}
		
		private static object InvokeFunc7 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                                   object a5, object a6, object a7)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches(o, a1, a2, a3, a4, a5, a6, a7)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1, a2, a3, a4, a5, a6, a7 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1; args[1] = a2; args[2] = a3; args[3] = a4; args[4] = a5; args[5] = a6; args[6] = a7;
			}
			object ret;
			if (info.method != null) 
				ret = info.method.Invoke (o, args);
			else 
				ret = info.del.DynamicInvoke(null, args);
			args[0] = args[1] = args[2] = args[3] = args[4] = args[5] = args[6] = null;
			return ret;
		}
		
		private static object InvokeFunc8 (CallSite site, object o, object a1, object a2, object a3, object a4,
		                                   object a5, object a6, object a7, object a8)
		{
			var info = site.invokeInfo;
			object[] args;
			if (info == null || !info.InvokeMatches(o, a1, a2, a3, a4, a5, a6, a7, a8)) {
				((CSharpInvokeMemberBinder)site.Binder).FindMethod (site, o, new [] { a1, a2, a3, a4, a5, a6, a7, a8 });
				info = site.invokeInfo;
				args = info.args;
			} else {
				args = info.args;
				args[0] = a1; args[1] = a2; args[2] = a3; args[3] = a4; args[4] = a5; args[5] = a6; args[6] = a7; args[7] = a8;
			}			
			object ret;
			if (info.method != null) 
				ret = info.method.Invoke (o, args);
			else 
				ret = info.del.DynamicInvoke(null, args);
			args[0] = args[1] = args[2] = args[3] = args[4] = args[5] = args[6] = args[7] = null;
			return ret;
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