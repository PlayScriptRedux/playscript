//
// CSharpUnaryOperationBinder.cs
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
using System.Linq.Expressions;
using Compiler = Mono.CSharp;

namespace PlayScript.RuntimeBinder
{
	class CSharpUnaryOperationBinder : UnaryOperationBinder
	{
		IList<CSharpArgumentInfo> argumentInfo;
		readonly CSharpBinderFlags flags;
		readonly Type context;
		
		public CSharpUnaryOperationBinder (ExpressionType operation, CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
			: base (operation)
		{
			this.argumentInfo = argumentInfo.ToReadOnly ();
			if (this.argumentInfo.Count != 1)
				throw new ArgumentException ("Unary operation requires 1 argument");

			this.flags = flags;
			this.context = context;
		}
	

		Compiler.Unary.Operator GetOperator ()
		{
			switch (Operation) {
			case ExpressionType.Negate:
				return Compiler.Unary.Operator.UnaryNegation;
			case ExpressionType.Not:
				return Compiler.Unary.Operator.LogicalNot;
			case ExpressionType.OnesComplement:
				return Compiler.Unary.Operator.OnesComplement;
			case ExpressionType.UnaryPlus:
				return Compiler.Unary.Operator.UnaryPlus;
			default:
				throw new NotImplementedException (Operation.ToString ());
			}
		}
		
		public override DynamicMetaObject FallbackUnaryOperation (DynamicMetaObject target, DynamicMetaObject errorSuggestion)
		{
			var ctx = DynamicContext.Create ();
			var expr = ctx.CreateCompilerExpression (argumentInfo [0], target);

			if (Operation == ExpressionType.IsTrue) {
				expr = new Compiler.BooleanExpression (expr);
			} else if (Operation == ExpressionType.IsFalse) {
				expr = new Compiler.BooleanExpressionFalse (expr);
			} else {
				if (Operation == ExpressionType.Increment)
					expr = new Compiler.UnaryMutator (Compiler.UnaryMutator.Mode.PreIncrement, expr, Compiler.Location.Null);
				else if (Operation == ExpressionType.Decrement)
					expr = new Compiler.UnaryMutator (Compiler.UnaryMutator.Mode.PreDecrement, expr, Compiler.Location.Null);
				else
					expr = new Compiler.Unary (GetOperator (), expr, Compiler.Location.Null);

				expr = new Compiler.Cast (new Compiler.TypeExpression (ctx.ImportType (ReturnType), Compiler.Location.Null), expr, Compiler.Location.Null);

				if ((flags & CSharpBinderFlags.CheckedContext) != 0)
					expr = new Compiler.CheckedExpr (expr, Compiler.Location.Null);
			}

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
	class CSharpUnaryOperationBinder : CallSiteBinder
	{
		private static Dictionary<ExpressionType, object> delegates = new Dictionary<ExpressionType, object>();

		ExpressionType operation;
		List<CSharpArgumentInfo> argumentInfo;
//		readonly CSharpBinderFlags flags;
//		readonly Type context;
		
		private static void ThrowOnInvalidOp (object o, string op)
		{
			throw new Exception ("Invalid " + op + " operation with type " + o.GetType ().Name);
		}
		
		public static object NegateObject (CallSite site, object a)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.UnaryOperationBinderInvoked);
#endif
			if (a is int) {
				return -(int)a;
			} else if (a is double) {
				return -(double)a;
			} else if (a is uint) {
				return -(uint)a;
			} else {
				ThrowOnInvalidOp(a, "negate");
				return null;
			}
		}

		public static object IncrementObject (CallSite site, object a)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.UnaryOperationBinderInvoked);
#endif
			if (a is int) {
				return (int)a + 1;
			} else if (a is double) {
				return (double)a + 1.0;
			} else if (a is uint) {
				return (uint)a + 1;
			} else {
				ThrowOnInvalidOp(a, "increment");
				return null;

			}
		}

		public static object DecrementObject (CallSite site, object a)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.UnaryOperationBinderInvoked);
#endif
			if (a is int) {
				return (int)a - 1;
			} else if (a is double) {
				return (double)a - 1.0;
			} else if (a is uint) {
				return (uint)a - 1;
			} else {
				ThrowOnInvalidOp(a, "decrement");
				return null;
			}
		}

		public static object LogicalNotObject (CallSite site, object a)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.UnaryOperationBinderInvoked);
#endif
			if (a is bool) {
				return !(bool)a;
			} if (a is int) {
				return (int)a == 0;
			} else if (a is double) {
				return (double)a == 0.0;
			} else if (a is uint) {
				return (uint)a == 0;
			} else {
				return a == null;
			}
		}

		public static object BitwiseNotObject (CallSite site, object a)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.UnaryOperationBinderInvoked);
#endif
			if (a is bool) {
				return (bool)a ? 0 : 1;
			} if (a is int) {
				return ~((int)a);
			} else if (a is double) {
				return (double)(~(int)a);
			} else if (a is uint) {
				return ~(uint)a;
			} else {
				ThrowOnInvalidOp(a, "decrement");
				return null;
			}
		}
		public static bool IsTrueObject (CallSite site, object a)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.UnaryOperationBinderInvoked);
#endif
			return Dynamic.CastObjectToBool(a) == true;
		}

		public static bool IsFalseObject (CallSite site, object a)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.UnaryOperationBinderInvoked);
#endif
			return Dynamic.CastObjectToBool(a) == false;
		}


		static CSharpUnaryOperationBinder ()
		{
			delegates.Add (ExpressionType.Negate, (Func<CallSite, object, object>)NegateObject);
			delegates.Add (ExpressionType.NegateChecked, (Func<CallSite, object, object>)NegateObject);
			
			delegates.Add (ExpressionType.Increment, (Func<CallSite, object, object>)IncrementObject);

			delegates.Add (ExpressionType.Decrement, (Func<CallSite, object, object>)DecrementObject);

			delegates.Add (ExpressionType.Not, (Func<CallSite, object, object>)LogicalNotObject);

			delegates.Add (ExpressionType.OnesComplement, (Func<CallSite, object, object>)BitwiseNotObject);

			delegates.Add (ExpressionType.IsFalse, (Func<CallSite, object, bool>)IsFalseObject);
			delegates.Add (ExpressionType.IsTrue, (Func<CallSite, object, bool>)IsTrueObject);
		}

		public CSharpUnaryOperationBinder (ExpressionType operation, CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			this.operation = operation;
			this.argumentInfo = new List<CSharpArgumentInfo>(argumentInfo);
			if (this.argumentInfo.Count != 1)
				throw new ArgumentException ("Unary operation requires 1 argument");
			
//			this.flags = flags;
//			this.context = context;
		}

		public override object Bind (Type delegateType)
		{
			object target;
			if (delegates.TryGetValue (operation, out target)) {
				return target;
			}
			throw new Exception("Unable to bind binary operation " + 
			                    Enum.GetName (typeof(ExpressionType), operation) + 
			                    " for target " + delegateType.FullName);
		}
	}
}

#endif