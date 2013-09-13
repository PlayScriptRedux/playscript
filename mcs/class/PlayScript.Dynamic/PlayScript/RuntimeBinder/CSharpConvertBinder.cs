//
// CSharpConvertBinder.cs
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
	class CSharpConvertBinder : ConvertBinder
	{
		readonly CSharpBinderFlags flags;
		readonly Type context;

		public CSharpConvertBinder (Type type, Type context, CSharpBinderFlags flags)
			: base (type, (flags & CSharpBinderFlags.ConvertExplicit) != 0)
		{
			this.flags = flags;
			this.context = context;
		}

		public override DynamicMetaObject FallbackConvert (DynamicMetaObject target, DynamicMetaObject errorSuggestion)
		{
			var ctx = DynamicContext.Create ();
			var expr = ctx.CreateCompilerExpression (null, target);

			if (Explicit)
				expr = new Compiler.Cast (new Compiler.TypeExpression (ctx.ImportType (Type), Compiler.Location.Null), expr, Compiler.Location.Null);
			else
				expr = new Compiler.ImplicitCast (expr, ctx.ImportType (Type), (flags & CSharpBinderFlags.ConvertArrayIndex) != 0);

			if ((flags & CSharpBinderFlags.CheckedContext) != 0)
				expr = new Compiler.CheckedExpr (expr, Compiler.Location.Null);

			var binder = new CSharpBinder (this, expr, errorSuggestion);
			binder.AddRestrictions (target);

			return binder.Bind (ctx, context);
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
using PlayScript;

namespace PlayScript.RuntimeBinder
{

	class CSharpConvertBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> delegates = new Dictionary<Type, object>();

	//	readonly Type type;
	//	readonly CSharpBinderFlags flags;
	//	readonly Type context;
		
		public CSharpConvertBinder (Type type, Type context, CSharpBinderFlags flags)
		{
	//		this.type = type;
	//		this.flags = flags;
	//		this.context = context;
		}

		public static int ConvertToInt (CallSite site, object o)
		{
			if (o == null)
			{
				return 0;
			}
			var typeCode = Type.GetTypeCode (o.GetType ());
			switch (typeCode) {
			case TypeCode.Int32:
				return (int)o;
			case TypeCode.Double:
				return (int)((double)o);
			case TypeCode.Boolean:
				return (bool)o ? 1 : 0;
			case TypeCode.UInt32:
				return (int)((uint)o);
			case TypeCode.Single:
				return (int)((float)o);
			case TypeCode.String:
				return int.Parse((String)o);
			default:
				throw new Exception ("Invalid cast to int");
			}
		}

		public static uint ConvertToUInt (CallSite site, object o)
		{
			if (o == null)
			{
				return 0;
			}
			var typeCode = Type.GetTypeCode (o.GetType ());
			switch (typeCode) {
			case TypeCode.Int32:
				return (uint)((int)o);
			case TypeCode.Double:
				return (uint)((double)o);
			case TypeCode.Boolean:
				return (bool)o ? (uint)1 : (uint)0;
			case TypeCode.UInt32:
				return (uint)o;
			case TypeCode.Single:
				return (uint)((float)o);
			case TypeCode.String:
				return uint.Parse((String)o);
			default:
				throw new Exception ("Invalid cast to int");
			}
		}

		public static double ConvertToDouble (CallSite site, object o)
		{
			if (o == null)
			{
				return 0;
			}
			var typeCode = Type.GetTypeCode (o.GetType ());
			switch (typeCode) {
			case TypeCode.Int32:
				return (int)o;
			case TypeCode.Double:
				return (double)o;
			case TypeCode.Boolean:
				return (bool)o ? 1 : 0;
			case TypeCode.UInt32:
				return (uint)o;
			case TypeCode.Single:
				return (float)o;
			case TypeCode.String:
				return double.Parse((String)o);
			default:
				throw new Exception ("Invalid cast to int");
			}
		}

		public static bool ConvertToBool (CallSite site, object o)
		{
			if (o == null)
			{
				return false;
			}
			var typeCode = Type.GetTypeCode (o.GetType ());
			switch (typeCode) {
			case TypeCode.Int32:
				return (int)o != 0;
			case TypeCode.Double:
				return (double)o != 0.0;
			case TypeCode.Boolean:
				return (bool)o;
			case TypeCode.UInt32:
				return (uint)o != 0;
			case TypeCode.Single:
				return (float)o != 0.0f;
			default:
				throw new Exception ("Invalid cast to int");
			}
		}

		public static string ConvertToString (CallSite site, object o)
		{
			if (o == null || o == PlayScript.Undefined._undefined) {
				return null;
			} else if  (o is string) {
				return (string)o;
			} else {
				return o.ToString ();
			}
		}

		public static object ConvertToObj (CallSite site, object o)
		{
			return o;
		}

		static CSharpConvertBinder ()
		{
			delegates.Add (typeof(Func<CallSite, object, int>), (Func<CallSite, object, int>)ConvertToInt);
			delegates.Add (typeof(Func<CallSite, object, uint>), (Func<CallSite, object, uint>)ConvertToUInt);
			delegates.Add (typeof(Func<CallSite, object, double>), (Func<CallSite, object, double>)ConvertToDouble);
			delegates.Add (typeof(Func<CallSite, object, bool>), (Func<CallSite, object, bool>)ConvertToBool);
			delegates.Add (typeof(Func<CallSite, object, string>), (Func<CallSite, object, string>)ConvertToString);
			delegates.Add (typeof(Func<CallSite, object, object>), (Func<CallSite, object, object>)ConvertToObj);
		}
		
		public override object Bind (Type delegateType)
		{
			object target;
			if (delegates.TryGetValue (delegateType, out target)) {
				return target;
			}
			throw new Exception("Unable to bind convert for target " + delegateType.FullName);
		}

	}

}

#endif