
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
using PlayScript;

namespace PlayScript.DynamicRuntime
{
	public static class PSUnaryOperation
	{
		private static void ThrowOnInvalidOp (object o, string op)
		{
			throw new Exception ("Invalid " + op + " operation with type " + o.GetType ().Name);
		}
		
		public static object NegateObject (object a)
		{
			Stats.Increment(StatsCounter.UnaryOperationBinderInvoked);

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

		public static object IncrementObject (object a)
		{
			Stats.Increment(StatsCounter.UnaryOperationBinderInvoked);

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

		public static object DecrementObject (object a)
		{
			Stats.Increment(StatsCounter.UnaryOperationBinderInvoked);

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

		public static bool LogicalNotObject (object a)
		{
			Stats.Increment(StatsCounter.UnaryOperationBinderInvoked);

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

		public static object BitwiseNotObject (object a)
		{
			Stats.Increment(StatsCounter.UnaryOperationBinderInvoked);

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

		public static bool IsTrueObject (object a)
		{
			Stats.Increment(StatsCounter.UnaryOperationBinderInvoked);

			return Dynamic.CastObjectToBool(a) == true;
		}

		public static bool IsFalseObject (object a)
		{
			Stats.Increment(StatsCounter.UnaryOperationBinderInvoked);

			return Dynamic.CastObjectToBool(a) == false;
		}
	}
}

#endif