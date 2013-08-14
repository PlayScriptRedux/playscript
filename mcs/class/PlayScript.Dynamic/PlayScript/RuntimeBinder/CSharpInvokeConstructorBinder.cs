//
// CSharpInvokeConstructorBinder.cs
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
using System.Reflection;

#if DYNAMIC_SUPPORT

using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Linq;
using Compiler = Mono.CSharp;

namespace PlayScript.RuntimeBinder
{
	class CSharpInvokeConstructorBinder : DynamicMetaObjectBinder
	{
		IList<CSharpArgumentInfo> argumentInfo;
		Type callingContext;
		Type target_return_type;

		public CSharpInvokeConstructorBinder (Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			this.callingContext = callingContext;
			this.argumentInfo = argumentInfo.ToReadOnly ();
		}

		public override DynamicMetaObject Bind (DynamicMetaObject target, DynamicMetaObject[] args)
		{
			var ctx = DynamicContext.Create ();

			var type = ctx.CreateCompilerExpression (argumentInfo [0], target);
			target_return_type = type.Type.GetMetaInfo ();

			var c_args = ctx.CreateCompilerArguments (argumentInfo.Skip (1), args);

			var binder = new CSharpBinder (
				this, new Compiler.New (type, c_args, Compiler.Location.Null), null);

			binder.AddRestrictions (target);
			binder.AddRestrictions (args);

			return binder.Bind (ctx, callingContext);
		}

		public override Type ReturnType {
			get {
				return target_return_type;
			}
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

namespace PlayScript.RuntimeBinder
{
	class CSharpInvokeConstructorBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> delegates = new Dictionary<Type, object>();

		// IList<CSharpArgumentInfo> argumentInfo;
		// Type callingContext;
		// Type target_return_type;

		private static object InvokeConstructor(Type objType, object[] args)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeConstructorBinderInvoked);
#endif

			var constructors = objType.GetConstructors();

			// Handle Embed loaders..
			if (args.Length == 0 &&
			    objType.BaseType.Name == "EmbedLoader" && objType.BaseType.Namespace == "PlayScript") {
				var loaderObj = objType.GetConstructor (Type.EmptyTypes).Invoke (args);
				return loaderObj.GetType ().GetMethod ("Load").Invoke (loaderObj, null);
			}

			foreach (var c in constructors) {
				object[] outArgs;
				if (PlayScript.Dynamic.ConvertMethodParameters(c, args, out outArgs)) {
					return c.Invoke(outArgs);
				}
			}

			throw new InvalidOperationException("Unable to find matching constructor.");
		}

		public static object Func1 (CallSite site, object o1)
		{
			return InvokeConstructor((Type)o1, new object[] {});
		}
		
		public static object Func2 (CallSite site, object o1, object o2)
		{
			return InvokeConstructor((Type)o1, new [] { o2 });
		}
		
		public static object Func3 (CallSite site, object o1, object o2, object o3)
		{
			return InvokeConstructor((Type)o1, new [] { o2, o3 });
		}
		
		public static object Func4 (CallSite site, object o1, object o2, object o3, object o4)
		{
			return InvokeConstructor((Type)o1, new [] { o2, o3, o4 });
		}
		
		public static object Func5 (CallSite site, object o1, object o2, object o3, object o4, object o5)
		{
			return InvokeConstructor((Type)o1, new [] { o2, o3, o4, o5 });
		}
		
		public static object Func6 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6)
		{
			return InvokeConstructor((Type)o1, new [] { o2, o3, o4, o5, o6 });
		}
		
		public static object Func7 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7)
		{
			return InvokeConstructor((Type)o1, new [] { o2, o3, o4, o5, o6, o7 });
		}
		
		public static object Func8 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8)
		{
			return InvokeConstructor((Type)o1, new [] { o2, o3, o4, o5, o6, o7, o8});
		}
		
		public static object Func9 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8, object o9)
		{
			return InvokeConstructor((Type)o1, new [] { o2, o3, o4, o5, o6, o7, o8, o9 });
		}
		
		public static object Func10 (CallSite site, object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8, object o9, object o10)
		{
			return InvokeConstructor((Type)o1, new [] { o2, o3, o4, o5, o6, o7, o8, o9, o10 });
		}

		public CSharpInvokeConstructorBinder (Type callingContext, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
//			this.callingContext = callingContext;
//			this.argumentInfo = argumentInfo.ToReadOnly ();
		}

		static CSharpInvokeConstructorBinder ()
		{
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
