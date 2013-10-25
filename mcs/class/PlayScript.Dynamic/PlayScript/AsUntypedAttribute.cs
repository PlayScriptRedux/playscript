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

namespace PlayScript
{
	//
	// There is a distinction between the "Object" and "*" types in ActionScript. The
	// former adds a small amount of type safety (i.e. it disallows numeric operators),
	// whereas the latter is fully dynamic, and is the only type that can contain the
	// value of "undefined". However, this differentiation is only known internal to the
	// compiler, and both types essentially use the dynamic type in C#. This attribute
	// exists to markup C# code, indicating that it is using the "*" type.
	//
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
	public class AsUntypedAttribute : Attribute
	{
		public AsUntypedAttribute ()
		{
		}
	}
}
