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
using System.Diagnostics;

namespace PlayScript
{
	// enumeration of stats counters
	public enum StatsCounter
	{
		// Counters for binders
		UnaryOperationBinderCreated,
		UnaryOperationBinderInvoked,
		BinaryOperationBinderCreated,
		BinaryOperationBinderInvoked,
		ConvertBinderCreated,
		ConvertBinderInvoked,
		GetIndexBinderCreated,
		GetIndexBinderInvoked,
		GetIndexBinder_Int_Invoked,
		GetIndexBinder_Key_Invoked,
		GetIndexBinder_Key_Dictionary_Invoked,
		GetIndexBinder_Key_Property_Invoked,
		SetIndexBinderCreated,
		SetIndexBinderInvoked,
		SetIndexBinder_Int_Invoked,
		SetIndexBinder_Key_Invoked,
		SetIndexBinder_Key_Dictionary_Invoked,
		SetIndexBinder_Key_Property_Invoked,
		GetMemberBinderCreated,
		GetMemberBinderInvoked,
		GetMemberBinder_Resolve_Invoked,
		GetMemberBinder_Expando,
		SetMemberBinderCreated,
		SetMemberBinderInvoked,
		SetMemberBinder_Resolve_Invoked,
		InvokeBinderCreated,
		InvokeBinderInvoked,
		InvokeBinderInvoked_Fast,
		InvokeBinderInvoked_Slow,
		InvokeConstructorBinderCreated,
		InvokeConstructorBinderInvoked,
		InvokeMemberBinderCreated,
		InvokeMemberBinderInvoked,
		InvokeMemberBinderInvoked_Fast,
		InvokeMemberBinderInvoked_Slow,
		IsEventBinderCreated,
		IsEventBinderInvoked,

		// Counters for dynamic
		Dynamic_ConvertMethodParametersInvoked,
		Dynamic_FindPropertyGetterInvoked,
		Dynamic_FindPropertySetterInvoked,
		Dynamic_ConvertValueInvoked,
		Dynamic_CanConvertValueInvoked,
		Dynamic_ConvertValueGenericInvoked,
		Dynamic_GetDelegateTypeForMethodInvoked,
		Dynamic_GetInstanceMemberInvoked,
		Dynamic_SetInstanceMemberInvoked,
		Dynamic_GetStaticMemberInvoked,
		Dynamic_SetStaticMemberInvoked,
		Dynamic_CastObjectToBoolInvoked,
		Dynamic_InvokeStaticInvoked,
		Dynamic_ObjectIsClassInvoked,
		Dynamic_HasOwnPropertyInvoked,

		// other
		Runtime_CastArrayToVector,
		Runtime_CastVectorToArray,

		Total
	};


	/// <summary>
	/// Simple class to measure PlayScript framework usage statistics.
	/// </summary>
	public class Stats
	{
		public int[] Counters = new int[(int)StatsCounter.Total];

		public void Add(Stats other)
		{
			for (int i=0; i < Counters.Length; i++) {
				Counters[i] += other.Counters[i];
			}
		}

		public void Subtract(Stats other)
		{
			for (int i=0; i < Counters.Length; i++) {
				Counters[i] -= other.Counters[i];
			}
		}

		public void CopyFrom(Stats other)
		{
			for (int i=0; i < Counters.Length; i++) {
				Counters[i] = other.Counters[i];
			}
		}

		public void Reset()
		{
			for (int i=0; i < Counters.Length; i++) {
				Counters[i] = 0;
			}
		}

		public Dictionary<string, int> ToDictionary(bool skipZeros)
		{
			var d = new Dictionary<string, int>();
			for (int i=0; i < Counters.Length; i++) {
				var value = Counters[i];
				if (!skipZeros || (value != 0)) {
					d.Add( ((StatsCounter)i).ToString(), value);
				}
			}
			return d;
		}

		// current static stats object
		public static Stats CurrentInstance = new Stats();

		/// <summary>
		/// Increments a specific counter by one.
		/// 
		/// Note that this method is conditionally compiled, so there is no need to use #if BINDERS_RUNTIME_STATS around its usage.
		/// </summary>
		/// <param name="counter">The stat counter to increment.</param>
		[Conditional("BINDERS_RUNTIME_STATS")]
		public static void Increment(StatsCounter counter)
		{
			CurrentInstance.Counters[(int)counter]++;
		}
	}
}
