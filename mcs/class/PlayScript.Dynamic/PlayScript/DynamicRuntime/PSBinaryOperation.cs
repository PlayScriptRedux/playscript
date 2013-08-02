//
// PSBinaryOperation.cs
//
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


#if !DYNAMIC_SUPPORT

using System;
using System.Collections.Generic;

namespace PlayScript.DynamicRuntime
{
	/*
	 * 		public enum Operator {
			Multiply	= 0 | ArithmeticMask,
			Division	= 1 | ArithmeticMask,
			Modulus		= 2 | ArithmeticMask,
			Addition	= 3 | ArithmeticMask | AdditionMask,
			Subtraction = 4 | ArithmeticMask | SubtractionMask,

			LeftShift	= 5 | ShiftMask,
			RightShift	= 6 | ShiftMask,
			AsURightShift = 7 | ShiftMask,  // PlayScript Unsigned Right Shift

			LessThan	= 8 | ComparisonMask | RelationalMask,
			GreaterThan	= 9 | ComparisonMask | RelationalMask,
			LessThanOrEqual		= 10 | ComparisonMask | RelationalMask,
			GreaterThanOrEqual	= 11 | ComparisonMask | RelationalMask,
			Equality	= 12 | ComparisonMask | EqualityMask,
			Inequality	= 13 | ComparisonMask | EqualityMask,
			AsRefEquality = 14 | ComparisonMask | EqualityMask,
			AsRefInequality = 15 | ComparisonMask | EqualityMask,

			BitwiseAnd	= 16 | BitwiseMask,
			ExclusiveOr	= 17 | BitwiseMask,
			BitwiseOr	= 18 | BitwiseMask,

			LogicalAnd	= 19 | LogicalMask,
			LogicalOr	= 20 | LogicalMask,

			AsE4xChild				= 21 | AsE4xMask,
			AsE4xDescendant			= 22 | AsE4xMask,
			AsE4xChildAttribute		= 23 | AsE4xMask,
			AsE4xDescendantAttribute = 24 | AsE4xMask,

			//
			// Operator masks
			//
			ValuesOnlyMask	= ArithmeticMask - 1,
			ArithmeticMask	= 1 << 6,
			ShiftMask		= 1 << 7,
			ComparisonMask	= 1 << 8,
			EqualityMask	= 1 << 9,
			BitwiseMask		= 1 << 10,
			LogicalMask		= 1 << 11,
			AdditionMask	= 1 << 12,
			SubtractionMask	= 1 << 13,
			RelationalMask	= 1 << 14,
			AsE4xMask		= 1 << 15
		}

*/
	public static class PSBinaryOperation
	{
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

		public static object Addition(object a, int b)
		{
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

		public static object Addition(int a, object b)
		{
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

		public static object Addition(object a, uint b)
		{
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
		
		public static object Addition(uint a, object b)
		{
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

		public static object Addition(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToDouble(a) + b;
		}

		public static object Addition(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a + Convert.ToDouble(b);
		}

		public static object Addition(string a, object b)
		{
			return a + b;
		}

		public static object Addition(object a, string b)
		{
			return a.ToString() + b;
		}

		public static object Addition(object a, object b)
		{
			if (a is int) {
				return Addition((int)a, b);
			} else if (a is double) {
				return Addition((double)a, b);
			} else if (a is float) {
				return Addition((float)a, b);
			} else if (a is String) {
				return Addition((string)a, b);
			} else if (a is uint) {
				return Addition((uint)a, b);
			} else {
				ThrowOnInvalidOp (a, ADD);
				return null;
			}
		}

		public static object Subtraction(object a, int b)
		{
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
		
		public static object Subtraction(int a, object b)
		{
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
		
		public static object Subtraction(object a, uint b)
		{
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
		
		public static object Subtraction(uint a, object b)
		{
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

		public static object Subtraction(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return -b;
			}
			return Convert.ToDouble(a) - b;
		}
		
		public static object Subtraction(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a - Convert.ToDouble(b);
		}

		public static object Subtraction(object a, object b)
		{
			if (a is int) {
				return Subtraction((int)a, b);
			} else if (a is double) {
				return Subtraction((double)a, b);
			} else if (a is float) {
				return Subtraction((float)a, b);
			} else if (a is uint) {
				return Subtraction((uint)a, b);
			} else {
				ThrowOnInvalidOp (a, SUB);
				return null;
			}
		}

		public static object Multiply(object a, int b)
		{
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
		
		public static object Multiply(int a, object b)
		{
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
		
		public static object Multiply(object a, uint b)
		{
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
		
		public static object Multiply(uint a, object b)
		{
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
		
		public static object Multiply(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (double)0;
			}
			return Convert.ToDouble(a) * b;
		}
		
		public static object Multiply(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return (double)0;
			}
			return a * Convert.ToDouble(b);
		}

		public static object Multiply(object a, object b)
		{
			if (a is int) {
				return Multiply((int)a, b);
			} else if (a is double) {
				return Multiply((double)a, b);
			} else if (a is float) {
				return Multiply((float)a, b);
			} else if (a is uint) {
				return Multiply((uint)a, b);
			} else {
				ThrowOnInvalidOp (a, MUL);
				return null;
			}
		}

		public static object Division(object a, int b)
		{
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
		
		public static object Division(int a, object b)
		{
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
		
		public static object Division(object a, uint b)
		{
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
		
		public static object Division(uint a, object b)
		{
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
		
		public static object Division(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (double)0;
			}
			return Convert.ToDouble(a) / b;
		}
		
		public static object Division(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return Double.NaN;		// Should probably also use Positive and Negative Infinity
			}
			return a / Convert.ToDouble(b);
		}

		public static object Division(object a, object b)
		{
			if (a is int) {
				return Division((int)a, b);
			} else if (a is double) {
				return Division((double)a, b);
			} else if (a is float) {
				return Division((float)a, b);
			} else if (a is uint) {
				return Division((uint)a, b);
			} else {
				ThrowOnInvalidOp (a, DIV);
				return null;
			}
		}

		public static object Modulus(object a, int b)
		{
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
		
		public static object Modulus(int a, object b)
		{
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
		
		public static object Modulus(object a, uint b)
		{
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
		
		public static object Modulus(uint a, object b)
		{
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
		
		public static object Modulus(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return Convert.ToDouble(a) % b;
		}
		
		public static object Modulus(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return Double.NaN;		// Should probably also use Positive and Negative Infinity
			}
			return a % Convert.ToDouble(b);
		}

		public static object Modulus(object a, object b)
		{
			if (a is int) {
				return Modulus((int)a, b);
			} else if (a is double) {
				return Modulus((double)a, b);
			} else if (a is float) {
				return Modulus((float)a, b);
			} else if (a is uint) {
				return Modulus((uint)a, b);
			} else {
				ThrowOnInvalidOp (a, MOD);
				return null;
			}
		}

		// Shift operations are like logical operations, integer only

		public static object LeftShift(object a, int b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return Convert.ToInt32(a) << b;
		}
		
		public static object LeftShift(int a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a << Convert.ToInt32(b);
		}

		public static object LeftShift(object a, uint b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return Convert.ToInt32(a) << (int)b;
		}

		public static object LeftShift(uint a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (int)a << Convert.ToInt32(b);
		}

		public static object LeftShift(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return Convert.ToInt32(a) << (int)b;
		}

		public static object LeftShift(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (int)a << Convert.ToInt32(b);
		}
		
		public static object LeftShift(object a, object b)
		{
			if (a is int) {
				return LeftShift((int)a, b);
			} else if (a is uint) {
				return LeftShift((uint)a, b);
			} else if (a is double) {
				return LeftShift((double)a, b);
			} else if (a is float) {
				return LeftShift((float)a, b);
			} else {
				ThrowOnInvalidOp (a, SHL);
				return null;
			}
		}

		public static object RightShift(object a, int b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return Convert.ToInt32(a) >> b;
		}
		
		public static object RightShift(int a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a >> Convert.ToInt32(b);
		}

		public static object RightShift(object a, uint b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return Convert.ToInt32(a) >> (int)b;
		}

		public static object RightShift(uint a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (int)a >> Convert.ToInt32(b);
		}

		public static object RightShift(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return Convert.ToInt32(a) >> (int)b;
		}
		
		public static object RightShift(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (int)a >> Convert.ToInt32(b);
		}
		
		public static object RightShift(object a, object b)
		{
			if (a is int) {
				return RightShift((int)a, b);
			} else if (a is uint) {
				return RightShift((uint)a, b);
			} else if (a is double) {
				return RightShift((double)a, b);
			} else if (a is float) {
				return RightShift((float)a, b);
			} else {
				ThrowOnInvalidOp (a, SHR);
				return null;
			}
		}

		public static bool LessThan(object a, int b)
		{
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
		
		public static bool LessThan(int a, object b)
		{
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
		
		public static bool LessThan(object a, uint b)
		{
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
		
		public static bool LessThan(uint a, object b)
		{
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

		public static bool LessThan(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) < b;
		}
		
		public static bool LessThan(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return a < Convert.ToDouble(b);
		}

		public static bool LessThan(object a, string b)
		{
			if (a is string) {
				return String.CompareOrdinal((string)a, b) < 0;
			} else {
				ThrowOnInvalidOp (a, LT);
				return false;
			}
		}
		
		public static bool LessThan(string a, object b)
		{
			if (b is string) {
				return String.CompareOrdinal(a, (string)b) < 0;
			} else {
				ThrowOnInvalidOp (b, LT);
				return false;
			}
		}

		public static bool LessThan(object a, object b)
		{
			if (a is int) {
				return LessThan((int)a, b);
			} else if (a is string) {
				return LessThan((string)a, b);
			} else if (a is double) {
				return LessThan((double)a, b);
			} else if (a is float) {
				return LessThan((float)a, b);
			} else if (a is uint) {
				return LessThan((uint)a, b);
			} else {
				ThrowOnInvalidOp (a, LT);
				return false;
			}
		}

		public static bool GreaterThan(object a, int b)
		{
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
		
		public static bool GreaterThan(int a, object b)
		{
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
		
		public static bool GreaterThan(object a, uint b)
		{
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
		
		public static bool GreaterThan(uint a, object b)
		{
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

		public static bool GreaterThan(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) > b;
		}
		
		public static bool GreaterThan(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return a > Convert.ToDouble(b);
		}
		
		public static bool GreaterThan(object a, string b)
		{
			if (a is string) {
				return String.CompareOrdinal((string)a, b) > 0;
			} else {
				ThrowOnInvalidOp (a, GT);
				return false;
			}
		}
		
		public static bool GreaterThan(string a, object b)
		{
			if (b is string) {
				return String.CompareOrdinal(a, (string)b) > 0;
			} else {
				ThrowOnInvalidOp (b, GT);
				return false;
			}
		}
		
		public static bool GreaterThan(object a, object b)
		{
			if (a is int) {
				return GreaterThan((int)a, b);
			} else if (a is string) {
				return GreaterThan((string)a, b);
			} else if (a is double) {
				return GreaterThan((double)a, b);
			} else if (a is float) {
				return GreaterThan((float)a, b);
			} else if (a is uint) {
				return GreaterThan((uint)a, b);
			} else {
				ThrowOnInvalidOp (a, GT);
				return false;
			}
		}

		public static bool LessThanOrEqual(object a, int b)
		{
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
		
		public static bool LessThanOrEqual(int a, object b)
		{
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
		
		public static bool LessThanOrEqual(object a, uint b)
		{
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
		
		public static bool LessThanOrEqual(uint a, object b)
		{
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

		public static bool LessThanOrEqual(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) <= (double)b;
		}
		
		public static bool LessThanOrEqual(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return (double)a <= Convert.ToDouble(b);
		}
		
		public static bool LessThanOrEqual(object a, string b)
		{
			if (a is string) {
				return String.CompareOrdinal((string)a, b) <= 0;
			} else {
				ThrowOnInvalidOp (a, LTE);
				return false;
			}
		}
		
		public static bool LessThanOrEqual(string a, object b)
		{
			if (b is string) {
				return String.CompareOrdinal(a, (string)b) <= 0;
			} else {
				ThrowOnInvalidOp (b, LTE);
				return false;
			}
		}
		
		public static bool LessThanOrEqual(object a, object b)
		{
			if (a is int) {
				return LessThanOrEqual((int)a, b);
			} else if (a is string) {
				return LessThanOrEqual((string)a, b);
			} else if (a is double) {
				return LessThanOrEqual((double)a, b);
			} else if (a is float) {
				return LessThanOrEqual((float)a, b);
			} else if (a is uint) {
				return LessThanOrEqual((uint)a, b);
			} else {
				ThrowOnInvalidOp (a, LTE);
				return false;
			}
		}
		
		public static bool GreaterThanOrEqual(object a, int b)
		{
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
		
		public static bool GreaterThanOrEqual(int a, object b)
		{
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
		
		public static bool GreaterThanOrEqual(object a, uint b)
		{
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
		
		public static bool GreaterThanOrEqual(uint a, object b)
		{
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

		public static bool GreaterThanOrEqual(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) >= b;
		}
		
		public static bool GreaterThanOrEqual(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return a >= Convert.ToDouble(b);
		}
		
		public static bool GreaterThanOrEqual(object a, string b)
		{
			if (a is string) {
				return String.CompareOrdinal((string)a, b) >= 0;
			} else {
				ThrowOnInvalidOp (a, GTE);
				return false;
			}
		}
		
		public static bool GreaterThanOrEqual(string a, object b)
		{
			if (b is string) {
				return String.CompareOrdinal(a, (string)b) >= 0;
			} else {
				ThrowOnInvalidOp (b, GTE);
				return false;
			}
		}
		
		public static bool GreaterThanOrEqual(object a, object b)
		{
			if (a is int) {
				return GreaterThanOrEqual((int)a, b);
			} else if (a is string) {
				return GreaterThanOrEqual((string)a, b);
			} else if (a is double) {
				return GreaterThanOrEqual((double)a, b);
			} else if (a is float) {
				return GreaterThanOrEqual((float)a, b);
			} else if (a is uint) {
				return GreaterThanOrEqual((uint)a, b);
			} else {
				ThrowOnInvalidOp (a, GTE);
				return false;
			}
		}

		public static bool Equality(object a, int b)
		{
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
		
		public static bool Equality(int a, object b)
		{
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

		public static bool Equality(object a, uint b)
		{
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
		
		public static bool Equality(uint a, object b)
		{
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
		
		public static bool Equality(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return Convert.ToDouble(a) == b;	// Should we compare with an epsilon here?
		}
		
		public static bool Equality(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return false;
			}
			return a == Convert.ToDouble(b);	// Should we compare with an epsilon here?
		}
		
		public static bool Equality(object a, string b)
		{
			if (a is string) {
				return String.CompareOrdinal((string)a, b) == 0;
			} else if (a == null) {
				return b == null;
			} else {
				ThrowOnInvalidOp (a, EQ);
				return false;
			}
		}
		
		public static bool Equality(string a, object b)
		{
			if (b is string) {
				return String.CompareOrdinal(a, (string)b) == 0;
			} else if (b == null) {
				return a == null;
			} else {
				ThrowOnInvalidOp (b, EQ);
				return false;
			}
		}

		public static bool Equality(object a, bool b)
		{
			return Dynamic.CastObjectToBool(a) == b;
		}
		
		public static bool Equality(bool a, object b)
		{
			return a == Dynamic.CastObjectToBool(b);
		}

		public static bool Equality(object a, object b)
		{
			if (a == PlayScript.Undefined._undefined) a = null;
			if (b == PlayScript.Undefined._undefined) b = null;

			if (a is int) {
				return Equality((int)a, b);
			} else if (a is string) {
				return Equality((string)a, b);
			} else if (a is double) {
				return Equality((double)a, b);
			} else if (a is float) {
				return Equality((float)a, b);
			} else if (a is bool) {
				return Equality((bool)a, b);
			} else if (a is uint) {
				return Equality((uint)a, b);
			} else if (a == b) {
				return true;
			} else if (a == null) {
				return false;
			} else {
				return a.Equals(b);
			}
		}

		public static bool Inequality(object a, int b)
		{
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
		
		public static bool Inequality(int a, object b)
		{
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
		
		public static bool Inequality(object a, uint b)
		{
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
		
		public static bool Inequality(uint a, object b)
		{
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

		public static bool Inequality(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return true;
			}
			return Convert.ToDouble(a) != b;
		}
		
		public static bool Inequality(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return true;
			}
			return a != Convert.ToDouble(b);
		}
		
		public static bool Inequality(object a, string b)
		{
			return !Equality(a, b);
		}
		
		public static bool Inequality(string a, object b)
		{
			return !Equality(a, b);
		}

		public static bool Inequality(object a, bool b)
		{
			if (a is bool) {
				return (bool)a != b;
			} else {
				return Dynamic.CastObjectToBool(a) != b;
			}
		}
		
		public static bool Inequality(bool a, object b)
		{
			if (b is bool) {
				return a != (bool)b;
			} else {
				return a != Dynamic.CastObjectToBool(b);
			}
		}
		
		public static bool Inequality(object a, object b)
		{
			if (a == PlayScript.Undefined._undefined) a = null;
			if (b == PlayScript.Undefined._undefined) b = null;
			
			if (a is int) {
				return Inequality((int)a, b);
			} else if (a is string) {
				return Inequality((string)a, b);
			} else if (a is double) {
				return Inequality((double)a, b);
			} else if (a is float) {
				return Inequality((float)a, b);
			} else if (a is bool) {
				return Inequality((bool)a, b);
			} else if (a is uint) {
				return Inequality((uint)a, b);
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

		public static object BitwiseAnd(object a, int b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return Convert.ToInt32(a) & b;
		}
		
		public static object BitwiseAnd(int a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return a & Convert.ToInt32(b);
		}
		
		public static object BitwiseAnd(object a, uint b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return Convert.ToUInt32(a) & b;
		}
		
		public static object BitwiseAnd(uint a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return (uint)0;
			}
			return a & Convert.ToUInt32(b);
		}
		
		public static object BitwiseAnd(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return Convert.ToInt32(a) & (int)b;
		}
		
		public static object BitwiseAnd(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return (int)0;
			}
			return (int)a & Convert.ToInt32(b);
		}
		
		public static object BitwiseAnd(object a, bool b)
		{
			if (a is bool) {
				return (bool)a && b;
			} else {
				return Dynamic.CastObjectToBool(a) && b;
			}
		}
		
		public static object BitwiseAnd(bool a, object b)
		{
			if (b is bool) {
				return a && (bool)b;
			} else {
				return a && Dynamic.CastObjectToBool(b);
			}
		}
		public static object BitwiseAnd(object a, object b)
		{
			if (a is int) {
				return BitwiseAnd((int)a, b);
			} else if (a is double) {
				return BitwiseAnd((double)a, b);
			} else if (a is float) {
				return BitwiseAnd((float)a, b);
			} else if (a is bool) {
				return BitwiseAnd((bool)a, b);
			} else if (a is uint) {
				return BitwiseAnd((uint)a, b);
			} else {
				return Dynamic.CastObjectToBool(a) && Dynamic.CastObjectToBool(b);
			}
		}

		public static object BitwiseOr(object a, int b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToInt32(a) | b;
		}
		
		public static object BitwiseOr(int a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a | Convert.ToInt32(b);
		}
		
		public static object BitwiseOr(object a, uint b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToUInt32(a) | b;
		}
		
		public static object BitwiseOr(uint a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a | Convert.ToUInt32(b);
		}
		
		public static object BitwiseOr(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToInt32(a) | (int)b;
		}
		
		public static object BitwiseOr(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (int)a | Convert.ToInt32(b);
		}
		
		public static object BitwiseOr(object a, bool b)
		{
			if (a is bool) {
				return (bool)a || b;
			} else {
				return Dynamic.CastObjectToBool(a) || b;
			}
		}
		
		public static object BitwiseOr(bool a, object b)
		{
			if (b is bool) {
				return a || (bool)b;
			} else {
				return a || Dynamic.CastObjectToBool(b);
			}
		}
		
		public static object BitwiseOr(object a, object b)
		{
			return Dynamic.CastObjectToBool(a) || Dynamic.CastObjectToBool(b);
		}

		public static object ExclusiveOr(object a, int b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToInt32(a) ^ b;
		}
		
		public static object ExclusiveOr(int a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a ^ Convert.ToInt32(b);
		}
		
		public static object ExclusiveOr(object a, uint b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToUInt32(a) ^ b;
		}
		
		public static object ExclusiveOr(uint a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return a ^ Convert.ToUInt32(b);
		}
		
		public static object ExclusiveOr(object a, double b)
		{
			if ((a == null) || (a == PlayScript.Undefined._undefined))
			{
				return b;
			}
			return Convert.ToInt32(a) ^ (int)b;
		}
		
		public static object ExclusiveOr(double a, object b)
		{
			if ((b == null) || (b == PlayScript.Undefined._undefined))
			{
				return a;
			}
			return (int)a ^ Convert.ToInt32(b);
		}
		
		public static object ExclusiveOr(object a, bool b)
		{
			return Dynamic.CastObjectToBool(a) ^ b;
		}
		
		public static object ExclusiveOr(bool a, object b)
		{
			return a ^ Dynamic.CastObjectToBool(b);
		}
		
		public static object ExclusiveOr(object a, object b)
		{
			if (a is int) {
				return ExclusiveOr((int)a, b);
			} else if (a is double) {
				return ExclusiveOr((double)a, b);
			} else if (a is float) {
				return ExclusiveOr((float)a, b);
			} else if (a is bool) {
				return ExclusiveOr((bool)a, b);
			} else if (a is uint) {
				return ExclusiveOr((uint)a, b);
			} else {
				ThrowOnInvalidOp (b, XOR);
				return null;
			}
		}
	}

}


#endif
