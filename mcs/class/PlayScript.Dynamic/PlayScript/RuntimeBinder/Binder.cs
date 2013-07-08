//
// Binder.cs
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

// #define USE_NEW_BINDERS

#if DYNAMIC_SUPPORT

using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using System.Reflection;

namespace PlayScript.RuntimeBinder
{
	public static class Binder
	{
		public static CallSiteBinder BinaryOperation (CSharpBinderFlags flags, ExpressionType operation, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			return new CSharpBinaryOperationBinder (operation, flags, context, argumentInfo);
		}
		
		public static CallSiteBinder Convert (CSharpBinderFlags flags, Type type, Type context)
		{
			return new CSharpConvertBinder (type, context, flags);
		}
		
		public static CallSiteBinder GetIndex (CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			return new CSharpGetIndexBinder (context, argumentInfo);
		}
		
		public static CallSiteBinder GetMember (CSharpBinderFlags flags, string name, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			return new CSharpGetMemberBinder (name, context, argumentInfo);
		}
		
		public static CallSiteBinder Invoke (CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			return new CSharpInvokeBinder (flags, context, argumentInfo);
		}
		
		public static CallSiteBinder InvokeConstructor (CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			// What are flags for here
			return new CSharpInvokeConstructorBinder (context, argumentInfo);
		}
		
		public static CallSiteBinder InvokeMember (CSharpBinderFlags flags, string name, IEnumerable<Type> typeArguments, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			return new CSharpInvokeMemberBinder (flags, name, context, typeArguments, argumentInfo);
		}
		
		public static CallSiteBinder IsEvent (CSharpBinderFlags flags, string name, Type context)
		{
			return new CSharpIsEventBinder (name, context);
		}
		
		public static CallSiteBinder SetIndex (CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			return new CSharpSetIndexBinder (flags, context, argumentInfo);
		}
		
		public static CallSiteBinder SetMember (CSharpBinderFlags flags, string name, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			return new CSharpSetMemberBinder (flags, name, context, argumentInfo);
		}
		
		public static CallSiteBinder UnaryOperation (CSharpBinderFlags flags, ExpressionType operation, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			return new CSharpUnaryOperationBinder (operation, flags, context, argumentInfo);
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
using PlayScript;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace PlayScript.RuntimeBinder
{
	public static class Binder
	{
		public static Func<object, string, Type, object>   OnGetMemberError;
		public static Action<object, string, object>       OnSetMemberError;
		public static int 								   MemberResolveCount;

		public static CallSiteBinder BinaryOperation (CSharpBinderFlags flags, ExpressionType operation, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			return new CSharpBinaryOperationBinder(operation, flags, context, argumentInfo);
		}

		public static CallSiteBinder Convert (CSharpBinderFlags flags, Type type, Type context)
		{
#if USE_NEW_BINDERS
			return new PSConvertBinder(type, context, flags);
#else
			return new CSharpConvertBinder(type, context, flags);
#endif
		}
		
		public static CallSiteBinder GetIndex (CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
#if USE_NEW_BINDERS
			return new PSGetIndexBinder(context, argumentInfo);
#else
			return new CSharpGetIndexBinder(context, argumentInfo);
#endif
		}
		
		public static CallSiteBinder GetMember (CSharpBinderFlags flags, string name, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
#if USE_NEW_BINDERS
			return new PSGetMemberBinder(name, context, argumentInfo);
#else
			return new CSharpGetMemberBinder(name, context, argumentInfo);
#endif
		}
		
		public static CallSiteBinder Invoke (CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			return new CSharpInvokeBinder(flags, context, argumentInfo);
		}
		
		public static CallSiteBinder InvokeConstructor (CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			return new CSharpInvokeConstructorBinder(context, argumentInfo);
		}

		public static CallSiteBinder InvokeMember (CSharpBinderFlags flags, string name, IEnumerable<Type> typeArguments, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
#if USE_NEW_BINDERS
			return new PSInvokeMemberBinder(flags, name, context, typeArguments, argumentInfo);
#else
			return new CSharpInvokeMemberBinder(flags, name, context, typeArguments, argumentInfo);
#endif
		}

		public static CallSiteBinder IsEvent (CSharpBinderFlags flags, string name, Type context)
		{
			throw new NotImplementedException("Dynamic IsEvent");
		}
		
		public static CallSiteBinder SetIndex (CSharpBinderFlags flags, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
#if USE_NEW_BINDERS
			return new PSSetIndexBinder(flags, context, argumentInfo);
#else
			return new CSharpSetIndexBinder(flags, context, argumentInfo);
#endif
		}
		
		public static CallSiteBinder SetMember (CSharpBinderFlags flags, string name, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
#if USE_NEW_BINDERS
			return new PSSetMemberBinder(flags, name, context, argumentInfo);
#else
			return new CSharpSetMemberBinder(flags, name, context, argumentInfo);
#endif
		}
		
		public static CallSiteBinder UnaryOperation (CSharpBinderFlags flags, ExpressionType operation, Type context, IEnumerable<CSharpArgumentInfo> argumentInfo)
		{
			return new CSharpUnaryOperationBinder(operation, flags, context, argumentInfo);
		}
	}
}

#endif
