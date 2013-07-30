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

#if BINDERS_RUNTIME_STATS
namespace PlayScript
{
	/// <summary>
	/// Simple class to measure PlayScript framework usage statistics.
	/// </summary>
	public class Stats
	{
		public static Stats CurrentInstance = new Stats();

		// Counters for binders
		public int	UnaryOperationBinderCreated;
		public int	UnaryOperationBinderInvoked;
		public int	BinaryOperationBinderCreated;
		public int	BinaryOperationBinderInvoked;
		public int	ConvertBinderCreated;
		public int	ConvertBinderInvoked;
		public int	GetIndexBinderCreated;
		public int	GetIndexBinderInvoked;
		public int	GetIndexBinder_Int_Invoked;
		public int	GetIndexBinder_Key_Invoked;
		public int	GetIndexBinder_Key_Dictionary_Invoked;
		public int	GetIndexBinder_Key_Property_Invoked;
		public int	SetIndexBinderCreated;
		public int	SetIndexBinderInvoked;
		public int	SetIndexBinder_Int_Invoked;
		public int	SetIndexBinder_Key_Invoked;
		public int	SetIndexBinder_Key_Dictionary_Invoked;
		public int	SetIndexBinder_Key_Property_Invoked;
		public int	GetMemberBinderCreated;
		public int	GetMemberBinderInvoked;
		public int	GetMemberBinder_Resolve_Invoked;
		public int	SetMemberBinderCreated;
		public int	SetMemberBinderInvoked;
		public int	SetMemberBinder_Resolve_Invoked;
		public int	InvokeBinderCreated;
		public int	InvokeBinderInvoked;
		public int	InvokeConstructorBinderCreated;
		public int	InvokeConstructorBinderInvoked;
		public int	InvokeMemberBinderCreated;
		public int	InvokeMemberBinderInvoked;
		public int	IsEventBinderCreated;
		public int	IsEventBinderInvoked;

		// Counters for dynamic
		public int	Dynamic_ConvertMethodParametersInvoked;
		public int	Dynamic_FindPropertyGetterInvoked;
		public int	Dynamic_FindPropertySetterInvoked;
		public int	Dynamic_ConvertValueInvoked;
		public int	Dynamic_CanConvertValueInvoked;
		public int	Dynamic_ConvertValueGenericInvoked;
		public int	Dynamic_GetDelegateTypeForMethodInvoked;
		public int	Dynamic_GetInstanceMemberInvoked;
		public int	Dynamic_SetInstanceMemberInvoked;
		public int	Dynamic_GetStaticMemberInvoked;
		public int	Dynamic_SetStaticMemberInvoked;
		public int	Dynamic_CastObjectToBoolInvoked;
		public int	Dynamic_InvokeStaticInvoked;
		public int	Dynamic_ObjectIsClassInvoked;
		public int	Dynamic_HasOwnPropertyInvoked;

		public void Reset()
		{
			// Counters for binders
			UnaryOperationBinderCreated = 0;
			UnaryOperationBinderInvoked = 0;
			BinaryOperationBinderCreated = 0;
			BinaryOperationBinderInvoked = 0;
			ConvertBinderCreated = 0;
			ConvertBinderInvoked = 0;
			GetIndexBinderCreated = 0;
			GetIndexBinderInvoked = 0;
			GetIndexBinder_Int_Invoked = 0;
			GetIndexBinder_Key_Invoked = 0;
			GetIndexBinder_Key_Dictionary_Invoked = 0;
			GetIndexBinder_Key_Property_Invoked = 0;
			SetIndexBinderCreated = 0;
			SetIndexBinderInvoked = 0;
			SetIndexBinder_Int_Invoked = 0;
			SetIndexBinder_Key_Invoked = 0;
			SetIndexBinder_Key_Dictionary_Invoked = 0;
			SetIndexBinder_Key_Property_Invoked = 0;
			GetMemberBinderCreated = 0;
			GetMemberBinderInvoked = 0;
			GetMemberBinder_Resolve_Invoked = 0;
			SetMemberBinderCreated = 0;
			SetMemberBinderInvoked = 0;
			SetMemberBinder_Resolve_Invoked = 0;
			InvokeBinderCreated = 0;
			InvokeBinderInvoked = 0;
			InvokeConstructorBinderCreated = 0;
			InvokeConstructorBinderInvoked = 0;
			InvokeMemberBinderCreated = 0;
			InvokeMemberBinderInvoked = 0;
			IsEventBinderCreated = 0;
			IsEventBinderInvoked = 0;

			// Counters for dynamic
			Dynamic_ConvertMethodParametersInvoked = 0;
			Dynamic_FindPropertyGetterInvoked = 0;
			Dynamic_FindPropertySetterInvoked = 0;
			Dynamic_ConvertValueInvoked = 0;
			Dynamic_CanConvertValueInvoked = 0;
			Dynamic_ConvertValueGenericInvoked = 0;
			Dynamic_GetDelegateTypeForMethodInvoked = 0;
			Dynamic_GetInstanceMemberInvoked = 0;
			Dynamic_SetInstanceMemberInvoked = 0;
			Dynamic_GetStaticMemberInvoked = 0;
			Dynamic_SetStaticMemberInvoked = 0;
			Dynamic_CastObjectToBoolInvoked = 0;
			Dynamic_InvokeStaticInvoked = 0;
			Dynamic_ObjectIsClassInvoked = 0;
			Dynamic_HasOwnPropertyInvoked = 0;
		}

		public void WriteToConsole()
		{
			Console.WriteLine();
			Console.WriteLine("Stats:");
			Console.WriteLine("Binders:");
			Console.WriteLine("UnaryOperationBinder - Created: {0} - Invoked: {1}", UnaryOperationBinderCreated, UnaryOperationBinderInvoked);
			Console.WriteLine("BinaryOperationBinder - Created: {0} - Invoked: {1}", BinaryOperationBinderCreated, BinaryOperationBinderInvoked);
			Console.WriteLine("ConvertBinder - Created: {0} - Invoked: {1}", ConvertBinderCreated, ConvertBinderInvoked);
			Console.WriteLine("GetIndexBinder - Created: {0} - Invoked: {1}", GetIndexBinderCreated, GetIndexBinderInvoked);
			Console.WriteLine("GetIndexBinder_Int - Invoked: {0}", GetIndexBinder_Int_Invoked);
			Console.WriteLine("GetIndexBinder_Key - Invoked: {0}", GetIndexBinder_Key_Invoked);
			Console.WriteLine("GetIndexBinder_Key_Dictionary - Invoked: {0}", GetIndexBinder_Key_Dictionary_Invoked);
			Console.WriteLine("GetIndexBinder_Key_Property - Invoked: {0}", GetIndexBinder_Key_Property_Invoked);
			Console.WriteLine("SetIndexBinder - Created: {0} - Invoked: {1}", SetIndexBinderCreated, SetIndexBinderInvoked);
			Console.WriteLine("SetIndexBinder_Int - Invoked: {0}", SetIndexBinder_Int_Invoked);
			Console.WriteLine("SetIndexBinder_Key - Invoked: {0}", SetIndexBinder_Key_Invoked);
			Console.WriteLine("SetIndexBinder_Key_Dictionary - Invoked: {0}", SetIndexBinder_Key_Dictionary_Invoked);
			Console.WriteLine("SetIndexBinder_Key_Property - Invoked: {0}", SetIndexBinder_Key_Property_Invoked);
			Console.WriteLine("GetMemberBinder - Created: {0} - Invoked: {1}", GetMemberBinderCreated, GetMemberBinderInvoked);
			Console.WriteLine("GetMemberBinder_Resolve - Invoked: {0}", GetMemberBinder_Resolve_Invoked);
			Console.WriteLine("SetMemberBinder - Created: {0} - Invoked: {1}", SetMemberBinderCreated, SetMemberBinderInvoked);
			Console.WriteLine("SetMemberBinder_Resolve - Invoked: {0}", SetMemberBinder_Resolve_Invoked);
			Console.WriteLine("InvokeBinder - Created: {0} - Invoked: {1}", InvokeBinderCreated, InvokeBinderInvoked);
			Console.WriteLine("InvokeConstructorBinder - Created: {0} - Invoked: {1}", InvokeConstructorBinderCreated, InvokeConstructorBinderInvoked);
			Console.WriteLine("InvokeMemberBinder - Created: {0} - Invoked: {1}", InvokeMemberBinderCreated, InvokeMemberBinderInvoked);
			Console.WriteLine("IsEventBinder - Created: {0} - Invoked: {1}", IsEventBinderCreated, IsEventBinderInvoked);
			Console.WriteLine("Dynamic:");
			Console.WriteLine("ConvertMethodParameters - Invoked: {0}", Dynamic_ConvertMethodParametersInvoked);
			Console.WriteLine("FindPropertyGetter - Invoked: {0}", Dynamic_FindPropertyGetterInvoked);
			Console.WriteLine("FindPropertySetter - Invoked: {0}", Dynamic_FindPropertySetterInvoked);
			Console.WriteLine("ConvertValue - Invoked: {0}", Dynamic_ConvertValueInvoked);
			Console.WriteLine("CanConvertValue - Invoked: {0}", Dynamic_CanConvertValueInvoked);
			Console.WriteLine("ConvertValueGeneric - Invoked: {0}", Dynamic_ConvertValueGenericInvoked);
			Console.WriteLine("GetDelegateTypeForMethod - Invoked: {0}", Dynamic_GetDelegateTypeForMethodInvoked);
			Console.WriteLine("GetInstanceMember - Invoked: {0}", Dynamic_GetInstanceMemberInvoked);
			Console.WriteLine("SetInstanceMember - Invoked: {0}", Dynamic_SetInstanceMemberInvoked);
			Console.WriteLine("GetStaticMember - Invoked: {0}", Dynamic_GetStaticMemberInvoked);
			Console.WriteLine("SetStaticMember - Invoked: {0}", Dynamic_SetStaticMemberInvoked);
			Console.WriteLine("CastObjectToBool - Invoked: {0}", Dynamic_CastObjectToBoolInvoked);
			Console.WriteLine("InvokeStatic - Invoked: {0}", Dynamic_InvokeStaticInvoked);
			Console.WriteLine("ObjectIsClass - Invoked: {0}", Dynamic_ObjectIsClassInvoked);
			Console.WriteLine("hasOwnProperty - Invoked: {0}", Dynamic_HasOwnPropertyInvoked);
		}
	}
}

#endif
