//
// CSharpBinaryOperationBinder.cs
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
using System.Runtime.CompilerServices;
using Compiler = Mono.CSharp;

namespace PlayScript.RuntimeBinder
{
	class CSharpBinaryOperationBinder2 : BinaryOperationBinder
	{
		IList<CSharpArgumentInfo> argumentInfo;
		readonly CSharpBinderFlags flags;
		readonly Type context;
		
		public CSharpBinaryOperationBinder (ExpressionType operation, CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
			: base (operation)
		{
			this.argumentInfo = new ReadOnlyCollectionBuilder<CSharpArgumentInfo> (argumentInfo);
			if (this.argumentInfo.Count != 2)
				throw new ArgumentException ("Binary operation requires 2 arguments");

			this.flags = flags;
			this.context = context;
		}

		Compiler.Binary.Operator GetOperator (out bool isCompound)
		{
			isCompound = false;
			switch (Operation) {
			case ExpressionType.Add:
				return Compiler.Binary.Operator.Addition;
			case ExpressionType.AddAssign:
				isCompound = true;
				return Compiler.Binary.Operator.Addition;
			case ExpressionType.And:
				return (flags & CSharpBinderFlags.BinaryOperationLogical) != 0 ?
					Compiler.Binary.Operator.LogicalAnd : Compiler.Binary.Operator.BitwiseAnd;
			case ExpressionType.AndAssign:
				isCompound = true;
				return Compiler.Binary.Operator.BitwiseAnd;
			case ExpressionType.Divide:
				return Compiler.Binary.Operator.Division;
			case ExpressionType.DivideAssign:
				isCompound = true;
				return Compiler.Binary.Operator.Division;
			case ExpressionType.Equal:
				return Compiler.Binary.Operator.Equality;
			case ExpressionType.ExclusiveOr:
				return Compiler.Binary.Operator.ExclusiveOr;
			case ExpressionType.ExclusiveOrAssign:
				isCompound = true;
				return Compiler.Binary.Operator.ExclusiveOr;
			case ExpressionType.GreaterThan:
				return Compiler.Binary.Operator.GreaterThan;
			case ExpressionType.GreaterThanOrEqual:
				return Compiler.Binary.Operator.GreaterThanOrEqual;
			case ExpressionType.LeftShift:
				return Compiler.Binary.Operator.LeftShift;
			case ExpressionType.LeftShiftAssign:
				isCompound = true;
				return Compiler.Binary.Operator.LeftShift;
			case ExpressionType.LessThan:
				return Compiler.Binary.Operator.LessThan;
			case ExpressionType.LessThanOrEqual:
				return Compiler.Binary.Operator.LessThanOrEqual;
			case ExpressionType.Modulo:
				return Compiler.Binary.Operator.Modulus;
			case ExpressionType.ModuloAssign:
				isCompound = true;
				return Compiler.Binary.Operator.Modulus;
			case ExpressionType.Multiply:
				return Compiler.Binary.Operator.Multiply;
			case ExpressionType.MultiplyAssign:
				isCompound = true;
				return Compiler.Binary.Operator.Multiply;
			case ExpressionType.NotEqual:
				return Compiler.Binary.Operator.Inequality;
			case ExpressionType.Or:
				return (flags & CSharpBinderFlags.BinaryOperationLogical) != 0 ?
					Compiler.Binary.Operator.LogicalOr : Compiler.Binary.Operator.BitwiseOr;
			case ExpressionType.OrAssign:
				isCompound = true;
				return Compiler.Binary.Operator.BitwiseOr;
			case ExpressionType.OrElse:
				return Compiler.Binary.Operator.LogicalOr;
			case ExpressionType.RightShift:
				return Compiler.Binary.Operator.RightShift;
			case ExpressionType.RightShiftAssign:
				isCompound = true;
				return Compiler.Binary.Operator.RightShift;
			case ExpressionType.Subtract:
				return Compiler.Binary.Operator.Subtraction;
			case ExpressionType.SubtractAssign:
				isCompound = true;
				return Compiler.Binary.Operator.Subtraction;
			default:
				throw new NotImplementedException (Operation.ToString ());
			}
		}
		
		public override DynamicMetaObject FallbackBinaryOperation (DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
		{
			var ctx = DynamicContext.Create ();
			var left = ctx.CreateCompilerExpression (argumentInfo [0], target);
			var right = ctx.CreateCompilerExpression (argumentInfo [1], arg);
			
			bool is_compound;
			var oper = GetOperator (out is_compound);
			Compiler.Expression expr;

			if (is_compound) {
				var target_expr = new Compiler.RuntimeValueExpression (target, ctx.ImportType (target.LimitType));
				expr = new Compiler.CompoundAssign (oper, target_expr, right, left);
			} else {
				expr = new Compiler.Binary (oper, left, right);
			}

			expr = new Compiler.Cast (new Compiler.TypeExpression (ctx.ImportType (ReturnType), Compiler.Location.Null), expr, Compiler.Location.Null);
			
			if ((flags & CSharpBinderFlags.CheckedContext) != 0)
				expr = new Compiler.CheckedExpr (expr, Compiler.Location.Null);

			var binder = new CSharpBinder (this, expr, errorSuggestion);
			binder.AddRestrictions (target);
			binder.AddRestrictions (arg);

			return binder.Bind (ctx, context);
		}
	}
}

#else 

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace PlayScript.RuntimeBinder
{
	class CSharpBinaryOperationBinder2 : CallSiteBinder
	{
		private static Dictionary<ExpressionType, Dictionary<Type, object>> delegates = new Dictionary<ExpressionType, Dictionary<Type, object>>();

		private static string ADD = "add";
		private static string SUB = "sub";
		private static string MUL = "mul";
		private static string DIV = "div";
		private static string MOD = "mod";
		private static string SHL = "shl";
		private static string SHR = "shr";
		private static string LT = "lt";
		private static string LTE = "lte";
		private static string GT = "gt";
		private static string GTE = "gte";
		private static string EQ = "eq";
		private static string NEQ = "ne";
//		private static string AND = "and";
//		private static string OR = "or";
		private static string XOR = "xor";

		readonly ExpressionType operation;
		readonly List<CSharpArgumentInfo> argumentInfo;
//		readonly CSharpBinderFlags flags;
//		readonly Type context;

		private static void ThrowOnInvalidOp (object o, string op)
		{
			throw new Exception ("Invalid " + op + " operation with type " + o.GetType ().Name);
		}

		// Arithmetic operations are using the following rules:
		//	A. If the object is of type of the other operand, cast the operand to the common type, and do the operation directly.
		//	B. If the object is of different type, we whole operation is done in double precision.
		// This keeps fast performance if the user code keeps all types the same, and allows for other cases to keep maximum precision.
		// There is a bit of performance loss in some cases (like when mixing int with uint), but it should not be common with AS.

		// We also have to test for null and undefined (before calling Convert.ToXYZ().
		// Here are the checks these operators have to do:
		//	1. If of expected, type direct cast and apply the operation.
		//	2. If null and undefined, assume the value is 0 and apply the operation.
		//	3. Otherwise convert to Double both operands and apply the operation.

		public static object AddObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int)
			{
				return (int)a + b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToDouble(a) + (double)b;
		}

		public static object AddIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is int)
			{
				return a + (int)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (double)a + Convert.ToDouble(b);
		}

		public static object AddObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is uint)
			{
				return (uint)a + b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToDouble(a) + (double)b;
		}
		
		public static object AddUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is uint)
			{
				return a + (uint)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (double)a + Convert.ToDouble(b);
		}

		public static object AddObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToDouble(a) + b;
		}

		public static object AddDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a + Convert.ToDouble(b);
		}

		public static object AddStringObj (CallSite site, string a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			return a + b;
		}

		public static object AddObjString (CallSite site, object a, string b)
		{
			return a.ToString() + b;
		}

		public static object AddObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int) {
				return AddIntObj (site, (int)a, b);
			} else if (a is double) {
				return AddDoubleObj (site, (double)a, b);
			} else if (a is float) {
				return AddDoubleObj (site, (float)a, b);
			} else if (a is String) {
				return AddStringObj (site, (string)a, b);
			} else if (a is uint) {
				return AddUIntObj (site, (uint)a, b);
			} else {
				ThrowOnInvalidOp (a, ADD);
				return null;
			}
		}

		public static object SubObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int)
			{
				return (int)a - b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return -b;
			}
			return Convert.ToDouble(a) - (double)b;
		}
		
		public static object SubIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is int)
			{
				return a - (int)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (double)a - Convert.ToDouble(b);
		}
		
		public static object SubObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is uint)
			{
				return (uint)a - b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return -b;
			}
			return Convert.ToDouble(a) - (double)b;
		}
		
		public static object SubUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is uint)
			{
				return a - (uint)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (double)a - Convert.ToDouble(b);
		}

		public static object SubObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return -b;
			}
			return Convert.ToDouble(a) - b;
		}
		
		public static object SubDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a - Convert.ToDouble(b);
		}

		public static object SubObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int) {
				return SubIntObj (site, (int)a, b);
			} else if (a is double) {
				return SubDoubleObj (site, (double)a, b);
			} else if (a is float) {
				return SubDoubleObj (site, (float)a, b);
			} else if (a is uint) {
				return SubUIntObj (site, (uint)a, b);
			} else {
				ThrowOnInvalidOp (a, SUB);
				return null;
			}
		}

		public static object MulObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int)
			{
				return (int)a * b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return Convert.ToDouble(a) * (double)b;
		}
		
		public static object MulIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is int)
			{
				return a * (int)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return (double)a * Convert.ToDouble(b);
		}
		
		public static object MulObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is uint)
			{
				return (uint)a * b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return Convert.ToDouble(a) * (double)b;
		}
		
		public static object MulUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is uint)
			{
				return a * (uint)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return (double)a * Convert.ToDouble(b);
		}
		
		public static object MulObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (double)0;
			}
			return Convert.ToDouble(a) * b;
		}
		
		public static object MulDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return (double)0;
			}
			return a * Convert.ToDouble(b);
		}

		public static object MulObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int) {
				return MulIntObj (site, (int)a, b);
			} else if (a is double) {
				return MulDoubleObj (site, (double)a, b);
			} else if (a is float) {
				return MulDoubleObj (site, (float)a, b);
			} else if (a is uint) {
				return MulUIntObj (site, (uint)a, b);
			} else {
				ThrowOnInvalidOp (a, MUL);
				return null;
			}
		}

		public static object DivObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int)
			{
				return (int)a / b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return Convert.ToDouble(a) / (double)b;
		}
		
		public static object DivIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is int)
			{
				return a / (int)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return Double.NaN;		// Should probably also use Positive and Negative Infinity
			}
			return (double)a / Convert.ToDouble(b);
		}
		
		public static object DivObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is uint)
			{
				return (uint)a / b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return Convert.ToDouble(a) / (double)b;
		}
		
		public static object DivUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is uint)
			{
				return a / (uint)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return Double.NaN;		// Should probably also use Positive and Negative Infinity
			}
			return (double)a / Convert.ToDouble(b);
		}
		
		public static object DivObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (double)0;
			}
			return Convert.ToDouble(a) / b;
		}
		
		public static object DivDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return Double.NaN;		// Should probably also use Positive and Negative Infinity
			}
			return a / Convert.ToDouble(b);
		}

		public static object DivObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int) {
				return DivIntObj (site, (int)a, b);
			} else if (a is double) {
				return DivDoubleObj (site, (double)a, b);
			} else if (a is float) {
				return DivDoubleObj (site, (float)a, b);
			} else if (a is uint) {
				return DivUIntObj (site, (uint)a, b);
			} else {
				ThrowOnInvalidOp (a, DIV);
				return null;
			}
		}

		public static object ModObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int)
			{
				return (int)a % b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return Convert.ToDouble(a) % (double)b;
		}
		
		public static object ModIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is int)
			{
				return a / (int)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return Double.NaN;		// Should probably also use Positive and Negative Infinity
			}
			return (double)a % Convert.ToDouble(b);
		}
		
		public static object ModObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is uint)
			{
				return (uint)a % b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return Convert.ToDouble(b) % (double)b;
		}
		
		public static object ModUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is uint)
			{
				return a / (uint)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return Double.NaN;		// Should probably also use Positive and Negative Infinity
			}
			return (double)a % Convert.ToDouble(b);
		}
		
		public static object ModObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return Convert.ToDouble(a) % b;
		}
		
		public static object ModDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return Double.NaN;		// Should probably also use Positive and Negative Infinity
			}
			return a % Convert.ToDouble(b);
		}

		public static object ModObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int) {
				return ModIntObj (site, (int)a, b);
			} else if (a is double) {
				return ModDoubleObj (site, (double)a, b);
			} else if (a is float) {
				return ModDoubleObj (site, (float)a, b);
			} else if (a is uint) {
				return ModUIntObj (site, (uint)a, b);
			} else {
				ThrowOnInvalidOp (a, MOD);
				return null;
			}
		}

		// Shift operations are like logical operations, integer only

		public static object ShiftLeftObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return Convert.ToInt32(a) << b;
		}
		
		public static object ShiftLeftIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a << Convert.ToInt32(b);
		}

		public static object ShiftLeftObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return Convert.ToInt32(a) << (int)b;
		}

		public static object ShiftLeftUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (int)a << Convert.ToInt32(b);
		}

		public static object ShiftLeftObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return Convert.ToInt32(a) << (int)b;
		}

		public static object ShiftLeftDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (int)a << Convert.ToInt32(b);
		}
		
		public static object ShiftLeftObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int) {
				return ShiftLeftIntObj (site, (int)a, b);
			} else if (a is uint) {
				return ShiftLeftUIntObj (site, (uint)a, b);
			} else if (a is double) {
				return ShiftLeftDoubleObj (site, (double)a, b);
			} else if (a is float) {
				return ShiftLeftDoubleObj (site, (float)a, b);
			} else {
				ThrowOnInvalidOp (a, SHL);
				return null;
			}
		}

		public static object ShiftRightObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return Convert.ToInt32(a) >> b;
		}
		
		public static object ShiftRightIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a >> Convert.ToInt32(b);
		}

		public static object ShiftRightObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return Convert.ToInt32(a) >> (int)b;
		}

		public static object ShiftRightUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (int)a >> Convert.ToInt32(b);
		}

		public static object ShiftRightObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return Convert.ToInt32(a) >> (int)b;
		}
		
		public static object ShiftRightDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (int)a >> Convert.ToInt32(b);
		}
		
		public static object ShiftRightObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int) {
				return ShiftRightIntObj (site, (int)a, b);
			} else if (a is uint) {
				return ShiftRightUIntObj (site, (uint)a, b);
			} else if (a is double) {
				return ShiftRightDoubleObj (site, (double)a, b);
			} else if (a is float) {
				return ShiftRightDoubleObj (site, (float)a, b);
			} else {
				ThrowOnInvalidOp (a, SHR);
				return null;
			}
		}

		public static object LessThanObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int)
			{
				return (int)a < b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) < (double)b;
		}
		
		public static object LessThanIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is int)
			{
				return a < (int)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return a < Convert.ToDouble(b);
		}
		
		public static object LessThanObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is uint)
			{
				return (uint)a < b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) < (double)b;
		}
		
		public static object LessThanUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is uint)
			{
				return a < (uint)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return (double)a < Convert.ToDouble(b);
		}

		public static object LessThanObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) < b;
		}
		
		public static object LessThanDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return a < Convert.ToDouble(b);
		}

		public static object LessThanObjString (CallSite site, object a, string b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is string) {
				return String.CompareOrdinal((string)a, b) < 0;
			} else {
				ThrowOnInvalidOp (a, LT);
				return null;
			}
		}
		
		public static object LessThanStringObj (CallSite site, string a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is string) {
				return String.CompareOrdinal(a, (string)b) < 0;
			} else {
				ThrowOnInvalidOp (b, LT);
				return null;
			}
		}

		public static object LessThanObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int) {
				return LessThanIntObj (site, (int)a, b);
			} else if (a is string) {
				return LessThanStringObj (site, (string)a, b);
			} else if (a is double) {
				return LessThanDoubleObj (site, (double)a, b);
			} else if (a is float) {
				return LessThanDoubleObj (site, (float)a, b);
			} else if (a is uint) {
				return LessThanUIntObj (site, (uint)a, b);
			} else {
				ThrowOnInvalidOp (a, LT);
				return null;
			}
		}

		public static object GreaterThanObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int)
			{
				return (int)a > b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) > (double)b;
		}
		
		public static object GreaterThanIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is int)
			{
				return a > (int)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return (double)a > Convert.ToDouble(b);
		}
		
		public static object GreaterThanObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is uint)
			{
				return (uint)a > b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) > (double)b;
		}
		
		public static object GreaterThanUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is uint)
			{
				return a > (uint)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return (double)a > Convert.ToDouble(b);
		}

		public static object GreaterThanObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) > b;
		}
		
		public static object GreaterThanDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return a > Convert.ToDouble(b);
		}
		
		public static object GreaterThanObjString (CallSite site, object a, string b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is string) {
				return String.CompareOrdinal((string)a, b) > 0;
			} else {
				ThrowOnInvalidOp (a, GT);
				return null;
			}
		}
		
		public static object GreaterThanStringObj (CallSite site, string a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is string) {
				return String.CompareOrdinal(a, (string)b) > 0;
			} else {
				ThrowOnInvalidOp (b, GT);
				return null;
			}
		}
		
		public static object GreaterThanObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int) {
				return GreaterThanIntObj (site, (int)a, b);
			} else if (a is string) {
				return GreaterThanStringObj (site, (string)a, b);
			} else if (a is double) {
				return GreaterThanDoubleObj (site, (double)a, b);
			} else if (a is float) {
				return GreaterThanDoubleObj (site, (float)a, b);
			} else if (a is uint) {
				return GreaterThanUIntObj (site, (uint)a, b);
			} else {
				ThrowOnInvalidOp (a, GT);
				return null;
			}
		}

		public static object LessThanEqObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int)
			{
				return (int)a <= b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) <= (double)b;
		}
		
		public static object LessThanEqIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is int)
			{
				return a <= (int)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return (double)a <= Convert.ToDouble(b);
		}
		
		public static object LessThanEqObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is uint)
			{
				return (uint)a <= b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) <= (double)b;
		}
		
		public static object LessThanEqUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is uint)
			{
				return a <= (uint)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return (double)a <= Convert.ToDouble(b);
		}

		public static object LessThanEqObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) <= (double)b;
		}
		
		public static object LessThanEqDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return (double)a <= Convert.ToDouble(b);
		}
		
		public static object LessThanEqObjString (CallSite site, object a, string b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is string) {
				return String.CompareOrdinal((string)a, b) <= 0;
			} else {
				ThrowOnInvalidOp (a, LTE);
				return null;
			}
		}
		
		public static object LessThanEqStringObj (CallSite site, string a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is string) {
				return String.CompareOrdinal(a, (string)b) <= 0;
			} else {
				ThrowOnInvalidOp (b, LTE);
				return null;
			}
		}
		
		public static object LessThanEqObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int) {
				return LessThanEqIntObj (site, (int)a, b);
			} else if (a is string) {
				return LessThanEqStringObj (site, (string)a, b);
			} else if (a is double) {
				return LessThanEqDoubleObj (site, (double)a, b);
			} else if (a is float) {
				return LessThanEqDoubleObj (site, (float)a, b);
			} else if (a is uint) {
				return LessThanEqUIntObj (site, (uint)a, b);
			} else {
				ThrowOnInvalidOp (a, LTE);
				return null;
			}
		}
		
		public static object GreaterThanEqObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int)
			{
				return (int)a >= b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) >= (double)b;
		}
		
		public static object GreaterThanEqIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is int)
			{
				return a >= (int)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return (double)a >= Convert.ToDouble(b);
		}
		
		public static object GreaterThanEqObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is uint)
			{
				return (uint)a >= b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) >= (double)b;
		}
		
		public static object GreaterThanEqUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is uint)
			{
				return a >= (uint)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return (double)a >= Convert.ToDouble(b);
		}

		public static object GreaterThanEqObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) >= b;
		}
		
		public static object GreaterThanEqDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return a >= Convert.ToDouble(b);
		}
		
		public static object GreaterThanEqObjString (CallSite site, object a, string b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is string) {
				return String.CompareOrdinal((string)a, b) >= 0;
			} else {
				ThrowOnInvalidOp (a, GTE);
				return null;
			}
		}
		
		public static object GreaterThanEqStringObj (CallSite site, string a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is string) {
				return String.CompareOrdinal(a, (string)b) >= 0;
			} else {
				ThrowOnInvalidOp (b, GTE);
				return null;
			}
		}
		
		public static object GreaterThanEqObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int) {
				return GreaterThanEqIntObj (site, (int)a, b);
			} else if (a is string) {
				return GreaterThanEqStringObj (site, (string)a, b);
			} else if (a is double) {
				return GreaterThanEqDoubleObj (site, (double)a, b);
			} else if (a is float) {
				return GreaterThanEqDoubleObj (site, (float)a, b);
			} else if (a is uint) {
				return GreaterThanEqUIntObj (site, (uint)a, b);
			} else {
				ThrowOnInvalidOp (a, GTE);
				return null;
			}
		}

		public static object EqualsObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int)
			{
				return (int)a == b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) == (double)b;	// Should we compare with an epsilon here?
		}
		
		public static object EqualsIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is int)
			{
				return a == (int)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return (double)a == Convert.ToDouble(b);	// Should we compare with an epsilon here?
		}

		public static object EqualsObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is uint)
			{
				return (uint)a == b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) == (double)b;	// Should we compare with an epsilon here?
		}
		
		public static object EqualsUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is uint)
			{
				return a == (uint)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return (double)a == Convert.ToDouble(b);	// Should we compare with an epsilon here?
		}
		
		public static object EqualsObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) == b;	// Should we compare with an epsilon here?
		}
		
		public static object EqualsDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return a == Convert.ToDouble(b);	// Should we compare with an epsilon here?
		}
		
		public static object EqualsObjString (CallSite site, object a, string b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is string) {
				return String.CompareOrdinal((string)a, b) == 0;
			} else {
				ThrowOnInvalidOp (a, EQ);
				return null;
			}
		}
		
		public static object EqualsStringObj (CallSite site, string a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is string) {
				return String.CompareOrdinal(a, (string)b) == 0;
			} else if (b == null) {
				return a == null;
			} else {
				ThrowOnInvalidOp (b, EQ);
				return null;
			}
		}

		public static object EqualsObjBool (CallSite site, object a, bool b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			return Dynamic.CastObjectToBool(a) == b;
		}
		
		public static object EqualsBoolObj (CallSite site, bool a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			return a == Dynamic.CastObjectToBool(b);
		}

		public static object EqualsObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a == PlayScript.Undefined._undefined) a = null;
			if (b == PlayScript.Undefined._undefined) b = null;

			if (a is int) {
				return EqualsIntObj (site, (int)a, b);
			} else if (a is string) {
				return EqualsStringObj (site, (string)a, b);
			} else if (a is double) {
				return EqualsDoubleObj (site, (double)a, b);
			} else if (a is float) {
				return EqualsDoubleObj (site, (float)a, b);
			} else if (a is bool) {
				return EqualsBoolObj (site, (bool)a, b);
			} else if (a is uint) {
				return EqualsUIntObj (site, (uint)a, b);
			} else if (a == b) {
				return true;
			} else if (a == null) {
				return false;
			} else {
				return a.Equals(b);
			}
		}

		public static object NotEqualsObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int)
			{
				return (int)a != b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return true;
			}
			return Convert.ToDouble(a) != (double)b;	// Should we compare with an epsilon here?
		}
		
		public static object NotEqualsIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is int)
			{
				return a != (int)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return true;
			}
			return (double)a != Convert.ToDouble(b);	// Should we compare with an epsilon here?
		}
		
		public static object NotEqualsObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is uint)
			{
				return (uint)a != b;
			}
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return true;
			}
			return Convert.ToDouble(a) != (double)b;	// Should we compare with an epsilon here?
		}
		
		public static object NotEqualsUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is uint)
			{
				return a != (uint)b;
			}
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return true;
			}
			return (double)a != Convert.ToDouble(b);	// Should we compare with an epsilon here?
		}

		public static object NotEqualsObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return true;
			}
			return Convert.ToDouble(a) != b;
		}
		
		public static object NotEqualsDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return true;
			}
			return a != Convert.ToDouble(b);
		}
		
		public static object NotEqualsObjString (CallSite site, object a, string b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is string) {
				return (string)a != b;
			} else {
				ThrowOnInvalidOp (a, NEQ);
				return null;
			}
		}
		
		public static object NotEqualsStringObj (CallSite site, string a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is string) {
				return a != (string)b;
			} else {
				ThrowOnInvalidOp (b, NEQ);
				return null;
			}
		}

		public static object NotEqualsObjBool (CallSite site, object a, bool b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is bool) {
				return (bool)a != b;
			} else {
				return Dynamic.CastObjectToBool(a) != b;
			}
		}
		
		public static object NotEqualsBoolObj (CallSite site, bool a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is bool) {
				return a != (bool)b;
			} else {
				return a != Dynamic.CastObjectToBool(b);
			}
		}
		
		public static object NotEqualsObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a == PlayScript.Undefined._undefined) a = null;
			if (b == PlayScript.Undefined._undefined) b = null;
			
			if (a is int) {
				return NotEqualsIntObj (site, (int)a, b);
			} else if (a is string) {
				return NotEqualsStringObj (site, (string)a, b);
			} else if (a is double) {
				return NotEqualsDoubleObj (site, (double)a, b);
			} else if (a is float) {
				return NotEqualsDoubleObj (site, (float)a, b);
			} else if (a is bool) {
				return NotEqualsBoolObj (site, (bool)a, b);
			} else if (a is uint) {
				return NotEqualsUIntObj (site, (uint)a, b);
			} else if (a == b) {
				return false;
			} else if (a == null) {
				return b != null;
			} else {
				// value comparison
				return !a.Equals(b);
			}
		}

		// Logical bit operations are all using integer math (should not upconvert to double to improve accuracy)
		// However we still have to test for null and undefined
		// Here are the checks these operators have to do:
		//	1. If null and undefined, assume the value is 0 and apply the operation.
		//	2. Convert to Int32 (or UInt32) both operands and apply the operation.
		// Boolean uses Dynamic.CastObjectToBool() instead of Convert.ToXYZ().

		public static object AndObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return Convert.ToInt32(a) & b;
		}
		
		public static object AndIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return a & Convert.ToInt32(b);
		}
		
		public static object AndObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return Convert.ToUInt32(a) & b;
		}
		
		public static object AndUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return a & Convert.ToUInt32(b);
		}
		
		public static object AndObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return Convert.ToInt32(a) & (int)b;
		}
		
		public static object AndDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return (int)a & Convert.ToInt32(b);
		}
		
		public static object AndObjBool (CallSite site, object a, bool b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is bool) {
				return (bool)a && b;
			} else {
				return Dynamic.CastObjectToBool(a) && b;
			}
		}
		
		public static object AndBoolObj (CallSite site, bool a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is bool) {
				return a && (bool)b;
			} else {
				return a && Dynamic.CastObjectToBool(b);
			}
		}
		public static object AndObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int) {
				return AndIntObj (site, (int)a, b);
			} else if (a is double) {
				return AndDoubleObj (site, (double)a, b);
			} else if (a is float) {
				return AndDoubleObj (site, (float)a, b);
			} else if (a is bool) {
				return AndBoolObj (site, (bool)a, b);
			} else if (a is uint) {
				return AndUIntObj (site, (uint)a, b);
			} else {
				return Dynamic.CastObjectToBool(a) && Dynamic.CastObjectToBool(b);
			}
		}

		public static object OrObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToInt32(a) | b;
		}
		
		public static object OrIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a | Convert.ToInt32(b);
		}
		
		public static object OrObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToUInt32(a) | b;
		}
		
		public static object OrUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a | Convert.ToUInt32(b);
		}
		
		public static object OrObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToInt32(a) | (int)b;
		}
		
		public static object OrDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (int)a | Convert.ToInt32(b);
		}
		
		public static object OrObjBool (CallSite site, object a, bool b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is bool) {
				return (bool)a || b;
			} else {
				return Dynamic.CastObjectToBool(a) || b;
			}
		}
		
		public static object OrBoolObj (CallSite site, bool a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (b is bool) {
				return a || (bool)b;
			} else {
				return a || Dynamic.CastObjectToBool(b);
			}
		}
		
		public static object OrObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			return Dynamic.CastObjectToBool(a) || Dynamic.CastObjectToBool(b);
		}

		public static object XorObjInt (CallSite site, object a, int b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToInt32(a) ^ b;
		}
		
		public static object XorIntObj (CallSite site, int a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a ^ Convert.ToInt32(b);
		}
		
		public static object XorObjUInt (CallSite site, object a, uint b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToUInt32(a) ^ b;
		}
		
		public static object XorUIntObj (CallSite site, uint a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a ^ Convert.ToUInt32(b);
		}
		
		public static object XorObjDouble (CallSite site, object a, double b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToInt32(a) ^ (int)b;
		}
		
		public static object XorDoubleObj (CallSite site, double a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (int)a ^ Convert.ToInt32(b);
		}
		
		public static object XorObjBool (CallSite site, object a, bool b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			return Dynamic.CastObjectToBool(a) ^ b;
		}
		
		public static object XorBoolObj (CallSite site, bool a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			return a ^ Dynamic.CastObjectToBool(b);
		}
		
		public static object XorObjObj (CallSite site, object a, object b)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.BinaryOperationBinderInvoked);
#endif
			if (a is int) {
				return XorIntObj (site, (int)a, b);
			} else if (a is double) {
				return XorDoubleObj (site, (double)a, b);
			} else if (a is float) {
				return XorDoubleObj (site, (float)a, b);
			} else if (a is bool) {
				return XorBoolObj (site, (bool)a, b);
			} else if (a is uint) {
				return XorUIntObj (site, (uint)a, b);
			} else {
				ThrowOnInvalidOp (b, XOR);
				return null;
			}
		}


		static CSharpBinaryOperationBinder2()
		{
			var addDict = new Dictionary<Type, object>();
			addDict.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite,object,int,object>)AddObjInt);
			addDict.Add (typeof(Func<CallSite, int, object, object>), (Func<CallSite,int,object,object>)AddIntObj);
			addDict.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite,object,uint,object>)AddObjUInt);
			addDict.Add (typeof(Func<CallSite, uint, object, object>), (Func<CallSite,uint,object,object>)AddUIntObj);
			addDict.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite,object,double,object>)AddObjDouble);
			addDict.Add (typeof(Func<CallSite, double, object, object>), (Func<CallSite,double,object,object>)AddDoubleObj);
			addDict.Add (typeof(Func<CallSite, object, string, object>), (Func<CallSite,object,string,object>)AddObjString);
			addDict.Add (typeof(Func<CallSite, string, object, object>), (Func<CallSite,string,object,object>)AddStringObj);
			addDict.Add (typeof(Func<CallSite, object, object, object>),  (Func<CallSite,object,object,object>)AddObjObj);
			delegates.Add (ExpressionType.Add, addDict);
			delegates.Add (ExpressionType.AddChecked, addDict);
			delegates.Add (ExpressionType.AddAssign, addDict);
			delegates.Add (ExpressionType.AddAssignChecked, addDict);

			var subDict = new Dictionary<Type, object>();
			subDict.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite,object,int,object>)SubObjInt);
			subDict.Add (typeof(Func<CallSite, int, object, object>), (Func<CallSite,int,object,object>)SubIntObj);
			subDict.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite,object,uint,object>)SubObjUInt);
			subDict.Add (typeof(Func<CallSite, uint, object, object>), (Func<CallSite,uint,object,object>)SubUIntObj);
			subDict.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite,object,double,object>)SubObjDouble);
			subDict.Add (typeof(Func<CallSite, double, object, object>), (Func<CallSite,double,object,object>)SubDoubleObj);
			subDict.Add (typeof(Func<CallSite, object, object, object>), (Func<CallSite,object,object,object>)SubObjObj);
			delegates.Add (ExpressionType.Subtract, subDict); 
			delegates.Add (ExpressionType.SubtractChecked, subDict); 
			delegates.Add (ExpressionType.SubtractAssign, subDict); 
			delegates.Add (ExpressionType.SubtractAssignChecked, subDict); 

			var mulDict = new Dictionary<Type, object>();
			mulDict.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite,object,int,object>)MulObjInt);
			mulDict.Add (typeof(Func<CallSite, int, object, object>), (Func<CallSite,int,object,object>)MulIntObj);
			mulDict.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite,object,uint,object>)MulObjUInt);
			mulDict.Add (typeof(Func<CallSite, uint, object, object>), (Func<CallSite,uint,object,object>)MulUIntObj);
			mulDict.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite,object,double,object>)MulObjDouble);
			mulDict.Add (typeof(Func<CallSite, double, object, object>), (Func<CallSite,double,object,object>)MulDoubleObj);
			mulDict.Add (typeof(Func<CallSite, object, object, object>), (Func<CallSite,object,object,object>)MulObjObj);
			delegates.Add (ExpressionType.Multiply, mulDict); 
			delegates.Add (ExpressionType.MultiplyChecked, mulDict); 
			delegates.Add (ExpressionType.MultiplyAssign, mulDict); 
			delegates.Add (ExpressionType.MultiplyAssignChecked, mulDict); 

			var divDict = new Dictionary<Type, object>();
			divDict.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite,object,int,object>)DivObjInt);
			divDict.Add (typeof(Func<CallSite, int, object, object>), (Func<CallSite,int,object,object>)DivIntObj);
			divDict.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite,object,uint,object>)DivObjUInt);
			divDict.Add (typeof(Func<CallSite, uint, object, object>), (Func<CallSite,uint,object,object>)DivUIntObj);
			divDict.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite,object,double,object>)DivObjDouble);
			divDict.Add (typeof(Func<CallSite, double, object, object>), (Func<CallSite,double,object,object>)DivDoubleObj);
			divDict.Add (typeof(Func<CallSite, object, object, object>),(Func<CallSite,object,object,object>) DivObjObj);
			delegates.Add (ExpressionType.Divide, divDict); 
			delegates.Add (ExpressionType.DivideAssign, divDict); 

			var modDict = new Dictionary<Type, object>();
			modDict.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite,object,int,object>)ModObjInt);
			modDict.Add (typeof(Func<CallSite, int, object, object>), (Func<CallSite,int,object,object>)ModIntObj);
			modDict.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite,object,uint,object>)ModObjUInt);
			modDict.Add (typeof(Func<CallSite, uint, object, object>), (Func<CallSite,uint,object,object>)ModUIntObj);
			modDict.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite,object,double,object>)ModObjDouble);
			modDict.Add (typeof(Func<CallSite, double, object, object>), (Func<CallSite,double,object,object>)ModDoubleObj);
			modDict.Add (typeof(Func<CallSite, object, object, object>),(Func<CallSite,object,object,object>) ModObjObj);
			delegates.Add (ExpressionType.Modulo, modDict); 
			delegates.Add (ExpressionType.ModuloAssign, modDict); 

			var ltDict = new Dictionary<Type, object>();
			ltDict.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite,object,int,object>)LessThanObjInt);
			ltDict.Add (typeof(Func<CallSite, int, object, object>), (Func<CallSite,int,object,object>)LessThanIntObj);
			ltDict.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite,object,uint,object>)LessThanObjUInt);
			ltDict.Add (typeof(Func<CallSite, uint, object, object>), (Func<CallSite,uint,object,object>)LessThanUIntObj);
			ltDict.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite,object,double,object>)LessThanObjDouble);
			ltDict.Add (typeof(Func<CallSite, double, object, object>), (Func<CallSite,double,object,object>)LessThanDoubleObj);
			ltDict.Add (typeof(Func<CallSite, object, string, object>), (Func<CallSite,object,string,object>)LessThanObjString);
			ltDict.Add (typeof(Func<CallSite, string, object, object>), (Func<CallSite,string,object,object>)LessThanStringObj);
			ltDict.Add (typeof(Func<CallSite, object, object, object>),  (Func<CallSite,object,object,object>)LessThanObjObj);
			delegates.Add (ExpressionType.LessThan, ltDict);

			var gtDict = new Dictionary<Type, object>();
			gtDict.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite,object,int,object>)GreaterThanObjInt);
			gtDict.Add (typeof(Func<CallSite, int, object, object>), (Func<CallSite,int,object,object>)GreaterThanIntObj);
			gtDict.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite,object,uint,object>)GreaterThanObjUInt);
			gtDict.Add (typeof(Func<CallSite, uint, object, object>), (Func<CallSite,uint,object,object>)GreaterThanUIntObj);
			gtDict.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite,object,double,object>)GreaterThanObjDouble);
			gtDict.Add (typeof(Func<CallSite, double, object, object>), (Func<CallSite,double,object,object>)GreaterThanDoubleObj);
			gtDict.Add (typeof(Func<CallSite, object, string, object>), (Func<CallSite,object,string,object>)GreaterThanObjString);
			gtDict.Add (typeof(Func<CallSite, string, object, object>), (Func<CallSite,string,object,object>)GreaterThanStringObj);
			gtDict.Add (typeof(Func<CallSite, object, object, object>),  (Func<CallSite,object,object,object>)GreaterThanObjObj);
			delegates.Add (ExpressionType.GreaterThan, gtDict);

			var lteDict = new Dictionary<Type, object>();
			lteDict.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite,object,int,object>)LessThanEqObjInt);
			lteDict.Add (typeof(Func<CallSite, int, object, object>), (Func<CallSite,int,object,object>)LessThanEqIntObj);
			lteDict.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite,object,uint,object>)LessThanEqObjUInt);
			lteDict.Add (typeof(Func<CallSite, uint, object, object>), (Func<CallSite,uint,object,object>)LessThanEqUIntObj);
			lteDict.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite,object,double,object>)LessThanEqObjDouble);
			lteDict.Add (typeof(Func<CallSite, double, object, object>), (Func<CallSite,double,object,object>)LessThanEqDoubleObj);
			lteDict.Add (typeof(Func<CallSite, object, string, object>), (Func<CallSite,object,string,object>)LessThanEqObjString);
			lteDict.Add (typeof(Func<CallSite, string, object, object>), (Func<CallSite,string,object,object>)LessThanEqStringObj);
			lteDict.Add (typeof(Func<CallSite, object, object, object>),  (Func<CallSite,object,object,object>)LessThanEqObjObj);
			delegates.Add (ExpressionType.LessThanOrEqual, lteDict);
			
			var gteDict = new Dictionary<Type, object>();
			gteDict.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite,object,int,object>)GreaterThanEqObjInt);
			gteDict.Add (typeof(Func<CallSite, int, object, object>), (Func<CallSite,int,object,object>)GreaterThanEqIntObj);
			gteDict.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite,object,uint,object>)GreaterThanEqObjUInt);
			gteDict.Add (typeof(Func<CallSite, uint, object, object>), (Func<CallSite,uint,object,object>)GreaterThanEqUIntObj);
			gteDict.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite,object,double,object>)GreaterThanEqObjDouble);
			gteDict.Add (typeof(Func<CallSite, double, object, object>), (Func<CallSite,double,object,object>)GreaterThanEqDoubleObj);
			gteDict.Add (typeof(Func<CallSite, object, string, object>), (Func<CallSite,object,string,object>)GreaterThanEqObjString);
			gteDict.Add (typeof(Func<CallSite, string, object, object>), (Func<CallSite,string,object,object>)GreaterThanEqStringObj);
			gteDict.Add (typeof(Func<CallSite, object, object, object>),  (Func<CallSite,object,object,object>)GreaterThanEqObjObj);
			delegates.Add (ExpressionType.GreaterThanOrEqual, gteDict);

			var eqDict = new Dictionary<Type, object>();
			eqDict.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite,object,int,object>)EqualsObjInt);
			eqDict.Add (typeof(Func<CallSite, int, object, object>), (Func<CallSite,int,object,object>)EqualsIntObj);
			eqDict.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite,object,uint,object>)EqualsObjUInt);
			eqDict.Add (typeof(Func<CallSite, uint, object, object>), (Func<CallSite,uint,object,object>)EqualsUIntObj);
			eqDict.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite,object,double,object>)EqualsObjDouble);
			eqDict.Add (typeof(Func<CallSite, double, object, object>), (Func<CallSite,double,object,object>)EqualsDoubleObj);
			eqDict.Add (typeof(Func<CallSite, object, string, object>), (Func<CallSite,object,string,object>)EqualsObjString);
			eqDict.Add (typeof(Func<CallSite, string, object, object>), (Func<CallSite,string,object,object>)EqualsStringObj);
			eqDict.Add (typeof(Func<CallSite, object, bool, object>), (Func<CallSite,object,bool,object>)EqualsObjBool);
			eqDict.Add (typeof(Func<CallSite, bool, object, object>), (Func<CallSite,bool,object,object>)EqualsBoolObj);
			eqDict.Add (typeof(Func<CallSite, object, object, object>),  (Func<CallSite,object,object,object>)EqualsObjObj);
			delegates.Add (ExpressionType.Equal, eqDict);
			
			var neDict = new Dictionary<Type, object>();
			neDict.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite,object,int,object>)NotEqualsObjInt);
			neDict.Add (typeof(Func<CallSite, int, object, object>), (Func<CallSite,int,object,object>)NotEqualsIntObj);
			neDict.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite,object,uint,object>)NotEqualsObjUInt);
			neDict.Add (typeof(Func<CallSite, uint, object, object>), (Func<CallSite,uint,object,object>)NotEqualsUIntObj);
			neDict.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite,object,double,object>)NotEqualsObjDouble);
			neDict.Add (typeof(Func<CallSite, double, object, object>), (Func<CallSite,double,object,object>)NotEqualsDoubleObj);
			neDict.Add (typeof(Func<CallSite, object, string, object>), (Func<CallSite,object,string,object>)NotEqualsObjString);
			neDict.Add (typeof(Func<CallSite, string, object, object>), (Func<CallSite,string,object,object>)NotEqualsStringObj);
			neDict.Add (typeof(Func<CallSite, bool, object, object>),  (Func<CallSite,bool,object,object>)NotEqualsBoolObj);
			neDict.Add (typeof(Func<CallSite, object, bool, object>),  (Func<CallSite,object,bool,object>)NotEqualsObjBool);
			neDict.Add (typeof(Func<CallSite, object, object, object>),  (Func<CallSite,object,object,object>)NotEqualsObjObj);
			delegates.Add (ExpressionType.NotEqual, neDict);

			var andDict = new Dictionary<Type, object>();
			andDict.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite,object,int,object>)AndObjInt);
			andDict.Add (typeof(Func<CallSite, int, object, object>), (Func<CallSite,int,object,object>)AndIntObj);
			andDict.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite,object,uint,object>)AndObjUInt);
			andDict.Add (typeof(Func<CallSite, uint, object, object>), (Func<CallSite,uint,object,object>)AndUIntObj);
			andDict.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite,object,double,object>)AndObjDouble);
			andDict.Add (typeof(Func<CallSite, double, object, object>), (Func<CallSite,double,object,object>)AndDoubleObj);
			andDict.Add (typeof(Func<CallSite, object, bool, object>), (Func<CallSite,object,bool,object>)AndObjBool);
			andDict.Add (typeof(Func<CallSite, bool, object, object>), (Func<CallSite,bool,object,object>)AndBoolObj);
			andDict.Add (typeof(Func<CallSite, object, object, object>),  (Func<CallSite,object,object,object>)AndObjObj);
			delegates.Add (ExpressionType.And, andDict);
			delegates.Add (ExpressionType.AndAssign, andDict);

			var orDict = new Dictionary<Type, object>();
			orDict.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite,object,int,object>)OrObjInt);
			orDict.Add (typeof(Func<CallSite, int, object, object>), (Func<CallSite,int,object,object>)OrIntObj);
			orDict.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite,object,uint,object>)OrObjUInt);
			orDict.Add (typeof(Func<CallSite, uint, object, object>), (Func<CallSite,uint,object,object>)OrUIntObj);
			orDict.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite,object,double,object>)OrObjDouble);
			orDict.Add (typeof(Func<CallSite, double, object, object>), (Func<CallSite,double,object,object>)OrDoubleObj);
			orDict.Add (typeof(Func<CallSite, object, bool, object>), (Func<CallSite,object,bool,object>)OrObjBool);
			orDict.Add (typeof(Func<CallSite, bool, object, object>), (Func<CallSite,bool,object,object>)OrBoolObj);
			orDict.Add (typeof(Func<CallSite, object, object, object>),  (Func<CallSite,object,object,object>)OrObjObj);
			delegates.Add (ExpressionType.Or, orDict);
			delegates.Add (ExpressionType.OrAssign, orDict);

			var xorDict = new Dictionary<Type, object>();
			xorDict.Add (typeof(Func<CallSite, object, int, object>), (Func<CallSite,object,int,object>)XorObjInt);
			xorDict.Add (typeof(Func<CallSite, int, object, object>), (Func<CallSite,int,object,object>)XorIntObj);
			xorDict.Add (typeof(Func<CallSite, object, uint, object>), (Func<CallSite,object,uint,object>)XorObjUInt);
			xorDict.Add (typeof(Func<CallSite, uint, object, object>), (Func<CallSite,uint,object,object>)XorUIntObj);
			xorDict.Add (typeof(Func<CallSite, object, double, object>), (Func<CallSite,object,double,object>)XorObjDouble);
			xorDict.Add (typeof(Func<CallSite, double, object, object>), (Func<CallSite,double,object,object>)XorDoubleObj);
			xorDict.Add (typeof(Func<CallSite, object, bool, object>), (Func<CallSite,object,bool,object>)XorObjBool);
			xorDict.Add (typeof(Func<CallSite, bool, object, object>), (Func<CallSite,bool,object,object>)XorBoolObj);
			xorDict.Add (typeof(Func<CallSite, object, object, object>),  (Func<CallSite,object,object,object>)XorObjObj);
			delegates.Add (ExpressionType.ExclusiveOr, xorDict);
			delegates.Add (ExpressionType.ExclusiveOrAssign, xorDict);

		}

		public CSharpBinaryOperationBinder2(ExpressionType operation, CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			this.operation = operation;
			this.argumentInfo = new List<CSharpArgumentInfo> (argumentInfo);
			if (this.argumentInfo.Count != 2)
				throw new ArgumentException ("Binary operation requires 2 arguments");
			
//			this.flags = flags;
//			this.context = context;
		}

		public override object Bind (Type delegateType)
		{
			Dictionary<Type, object> targetDict;
			if (delegates.TryGetValue (operation, out targetDict)) {
				object target;
				if (targetDict.TryGetValue (delegateType, out target)) {
					return target;
				}
			}
			throw new Exception("Unable to bind binary operation " + 
			                    Enum.GetName (typeof(ExpressionType), operation) + 
			                    " for target " + delegateType.FullName);
		}
	}

}


#endif
