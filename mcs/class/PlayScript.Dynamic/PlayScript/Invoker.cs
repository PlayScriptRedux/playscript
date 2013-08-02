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
using System.Reflection;

namespace PlayScript
{
	public abstract class InvokerBase
	{
		public abstract void SetArguments(object[] arguments);
		public abstract void Invoke();
		public abstract void InvokeOverrideA1(object a1);
	}

	public class DynamicInvoker : InvokerBase
	{
		MethodInfo mMethod;
		object mTarget;
		object[] mArguments;

		public DynamicInvoker(Delegate del)
		{
			mMethod = del.Method;
			mTarget = del.Target;
		}

		public override void SetArguments(object[] arguments)
		{
			mArguments = arguments;
		}

		public override void Invoke()
		{
			mMethod.Invoke(mTarget, mArguments);
		}

		public override void InvokeOverrideA1(object a1)
		{
			mArguments[0] = a1;
			mMethod.Invoke(mTarget, mArguments);
		}
	}

	/// <summary>
	/// This class implements a dynamic invoker for variadic listeners.
	/// The difference between DynamicInvokerVariadic and DynamicInvoker is that the parameters are passed themselves as object[].
	/// </summary>
	public class DynamicInvokerVariadic : InvokerBase
	{
		MethodInfo mMethod;
		object mTarget;
		object[] mArguments;

		public DynamicInvokerVariadic(Delegate del)
		{
			mMethod = del.Method;
			mTarget = del.Target;
		}

		public override void SetArguments(object[] arguments)
		{
			mArguments = arguments;
		}

		public override void Invoke()
		{
			mMethod.Invoke(mTarget, mArguments);
		}

		public override void InvokeOverrideA1(object a1)
		{
			mArguments[0] = new object[] { a1 };
			mMethod.Invoke(mTarget, mArguments);
		}
	}

	public class InvokerA : InvokerBase
	{
		Action mAction;

		public InvokerA(Action action)
		{
			mAction = action;
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 0) {
				throw new ArgumentException();
			}
		}

		public override void Invoke()
		{
			mAction();
		}

		public override void InvokeOverrideA1(object a1)
		{
			throw new InvalidOperationException();
		}
	}

	public class InvokerA<P1> : InvokerBase
	{
		Action<P1> mAction;
		P1 mArgument1;

		public InvokerA(Action<P1> action)
		{
			mAction = action;
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 1) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
		}

		public override void Invoke()
		{
			mAction(mArgument1);
		}

		public override void InvokeOverrideA1(object a1)
		{
			mAction((P1)a1);
		}
	}

	public class InvokerA<P1, P2> : InvokerBase
	{
		Action<P1, P2> mAction;
		P1 mArgument1;
		P2 mArgument2;

		public InvokerA(Action<P1, P2> action)
		{
			mAction = action;
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 2) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
			mArgument2 = (P2)arguments[1];
		}

		public override void Invoke()
		{
			mAction(mArgument1, mArgument2);
		}

		public override void InvokeOverrideA1(object a1)
		{
			mAction((P1)a1, mArgument2);
		}
	}
}



