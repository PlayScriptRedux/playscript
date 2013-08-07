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
using System.Diagnostics;
using System.Reflection;

namespace PlayScript
{
	public interface ICallerA
	{
		void Call();
	}

	public interface ICallerA<P1>
	{
		void Call(P1 a1);
	}

	public interface ICallerA<P1, P2>
	{
		void Call(P1 a1, P2 a2);
	}

	public interface ICallerA<P1, P2, P3>
	{
		void Call(P1 a1, P2 a2, P3 a3);
	}

	public interface ICallerA<P1, P2, P3, P4>
	{
		void Call(P1 a1, P2 a2, P3 a3, P4 a4);
	}

	public interface ICallerA<P1, P2, P3, P4, P5>
	{
		void Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5);
	}

	public interface ICallerA<P1, P2, P3, P4, P5, P6>
	{
		void Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6);
	}

	public interface ICallerA<P1, P2, P3, P4, P5, P6, P7>
	{
		void Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6, P7 a7);
	}

	public interface ICallerA<P1, P2, P3, P4, P5, P6, P7, P8>
	{
		void Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6, P7 a7, P8 a8);
	}

	public interface ICallerF<R>
	{
		R Call();
	}

	public interface ICallerF<P1, R>
	{
		R Call(P1 a1);
	}

	public interface ICallerF<P1, P2, R>
	{
		R Call(P1 a1, P2 a2);
	}

	public interface ICallerF<P1, P2, P3, R>
	{
		R Call(P1 a1, P2 a2, P3 a3);
	}

	public interface ICallerF<P1, P2, P3, P4, R>
	{
		R Call(P1 a1, P2 a2, P3 a3, P4 a4);
	}

	public interface ICallerF<P1, P2, P3, P4, P5, R>
	{
		R Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5);
	}

	public interface ICallerF<P1, P2, P3, P4, P5, P6, R>
	{
		R Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6);
	}

	public interface ICallerF<P1, P2, P3, P4, P5, P6, P7, R>
	{
		R Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6, P7 a7);
	}

	public interface ICallerF<P1, P2, P3, P4, P5, P6, P7, P8, R>
	{
		R Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6, P7 a7, P8 a8);
	}

	public abstract class InvokerBase
	{
		/// <summary>
		/// Sets the arguments for the argument.
		/// 
		/// This is currently used by the EventDispatcher (with the Invoke() method).
		/// There is a new mechanism in place for default values already for dynamic invoke.
		/// We might want to revisit this to allocate less memory (remove mArgument1, etc...) and improve perf a bit, simplify code, etc...
		/// </summary>
		/// <param name="arguments">Arguments passed for the invocation.</param>
		public abstract void SetArguments(object[] arguments);

		/// <summary>
		/// Invoke this instance with the parameters passed in SetArguments().
		/// </summary>
		public abstract object Invoke();

		/// <summary>
		/// Invokes the method and overrides the first paramter.
		/// This is used for event dispatch.
		/// </summary>
		/// <param name="a1">A1.</param>
		public abstract void InvokeOverrideA1(object a1);

		/// <summary>
		/// Invokes the method with a specific set of parameters, parameters are converted as needed.
		/// </summary>
		/// <returns>Return value of the invocation.</returns>
		/// <param name="args">The arguments for the invocation.</param>
		public abstract object SafeInvokeWith(object[] args);

		/// <summary>
		/// Invokes the method with a specific set of parameters, parameters are not converted and must be of the exact types.
		/// </summary>
		/// <returns>Return value of the invocation.</returns>
		/// <param name="args">The arguments for the invocation.</param>
		public abstract object UnsafeInvokeWith(object[] args);
	}

	public class DynamicInvoker : InvokerBase
	{
		MethodInfo mMethod;
		object mTarget;
		object[] mArguments;
		Delegate mDelegate;

		public DynamicInvoker(object target, MethodInfo methodInfo)
		{
			mMethod = methodInfo;
			mTarget = target;
		}

		public DynamicInvoker(Delegate del)
		{
			mDelegate = del;
			mMethod = del.Method;
			mTarget = del.Target;
		}

		public override void SetArguments(object[] arguments)
		{
			mArguments = arguments;
		}

		public override object Invoke()
		{
			return mMethod.Invoke(mTarget, mArguments);
		}

		public override void InvokeOverrideA1(object a1)
		{
			mArguments[0] = a1;
			mMethod.Invoke(mTarget, mArguments);
		}

		public override object SafeInvokeWith(object[] args)
		{
			object[] newArgs = PlayScript.Dynamic.ConvertArgumentList(mMethod, args);
			return mMethod.Invoke(mTarget, newArgs);
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			return mMethod.Invoke(mTarget, args);
		}
	}

	/// <summary>
	/// This class implements a dynamic invoker for variadic listeners.
	/// The difference between DynamicInvokerVariadic and DynamicInvoker is that the parameters are passed themselves as object[].
	/// 
	/// TODO: It seems we could change this implementation to actually use delegate invoke instead of a dynamic invoke, if return type is void.
	/// </summary>
	public class DynamicInvokerVariadic : InvokerBase
	{
		MethodInfo mMethod;
		object mTarget;
		object[] mArguments;
		Delegate mDelegate;

		public DynamicInvokerVariadic(Delegate del)
		{
			mDelegate = del;
			mMethod = del.Method;
			mTarget = del.Target;
		}

		public override void SetArguments(object[] arguments)
		{
			mArguments = arguments;
		}

		public override object Invoke()
		{
			return mMethod.Invoke(mTarget, mArguments);
		}

		public override void InvokeOverrideA1(object a1)
		{
			sTemp[0] = a1;			// Not thread safe. Not an issue with reentrancy though.
			mArguments[0] = sTemp;
			// TODO: This is variadic, so probably a fixed signature, see if we can convert this to a delegate invoke instead of a dynamic invoke
			mMethod.Invoke(mTarget, mArguments);
		}

		public override object SafeInvokeWith(object[] args)
		{
			object[] newArgs = PlayScript.Dynamic.ConvertArgumentList(mMethod, args);
			return mMethod.Invoke(mTarget, newArgs);
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			return mMethod.Invoke(mTarget, args);
		}

		private static object[] sTemp = new object[1];
	}

	public abstract class InvokerParamBase : InvokerBase
	{
		
	}

	public abstract class InvokerParamBase<P1> : InvokerBase
	{
		protected P1 mArgument1;
		protected P1 mDefaultArgument1;
		int mStartingDefaultValue = -1;

		protected void CheckDefaultArguments(int providedArguments, MethodInfo methodInfo)
		{
			Debug.Assert(providedArguments < 1);		// We should call this method only if we expect to use at least one default value

			if (mStartingDefaultValue == -1)
			{
				// Initializes the default values
				ParameterInfo[] parameterInfos = methodInfo.GetParameters();
				int numberOfParameters = parameterInfos.Length;
				Debug.Assert(numberOfParameters == 1);
				for (int i = 0 ; i < numberOfParameters ; ++i)
				{
					ParameterInfo parameter = parameterInfos[i];
					if (parameter.IsOptional)
					{
						if (mStartingDefaultValue == -1)
						{
							mStartingDefaultValue = i;
						}
						switch (i)
						{
							case 0:	mDefaultArgument1 = (P1)parameter.DefaultValue;	break;
						}
					}
				}
				if (mStartingDefaultValue == -1)
				{
					// We did not find any default value, so we have to provide at least N arguments
					mStartingDefaultValue = 1;
				}
			}

			if (providedArguments < mStartingDefaultValue)
			{
				throw new NotSupportedException("Not enough parameters provided");
			}
		}
	}

	public abstract class InvokerParamBase<P1,P2> : InvokerBase
	{
		protected P1 mArgument1;
		protected P1 mDefaultArgument1;
		protected P2 mArgument2;
		protected P2 mDefaultArgument2;
		int mStartingDefaultValue = -1;

		protected void CheckDefaultArguments(int providedArguments, MethodInfo methodInfo)
		{
			Debug.Assert(providedArguments < 2);		// We should call this method only if we expect to use at least one default value

			if (mStartingDefaultValue == -1)
			{
				// Initializes the default values
				ParameterInfo[] parameterInfos = methodInfo.GetParameters();
				int numberOfParameters = parameterInfos.Length;
				Debug.Assert(numberOfParameters == 2);
				for (int i = 0 ; i < numberOfParameters ; ++i)
				{
					ParameterInfo parameter = parameterInfos[i];
					if (parameter.IsOptional)
					{
						if (mStartingDefaultValue == -1)
						{
							mStartingDefaultValue = i;
						}
						switch (i)
						{
							case 0:	mDefaultArgument1 = (P1)parameter.DefaultValue;	break;
							case 1: mDefaultArgument2 = (P2)parameter.DefaultValue; break;
						}
					}
				}
				if (mStartingDefaultValue == -1)
				{
					// We did not find any default value, so we have to provide at least N arguments
					mStartingDefaultValue = 2;
				}
			}

			if (providedArguments < mStartingDefaultValue)
			{
				throw new NotSupportedException("Not enough parameters provided");
			}
		}
	}

	public abstract class InvokerParamBase<P1, P2, P3> : InvokerBase
	{
		protected P1 mArgument1;
		protected P1 mDefaultArgument1;
		protected P2 mArgument2;
		protected P2 mDefaultArgument2;
		protected P3 mArgument3;
		protected P3 mDefaultArgument3;
		int mStartingDefaultValue = -1;

		protected void CheckDefaultArguments(int providedArguments, MethodInfo methodInfo)
		{
			Debug.Assert(providedArguments < 3);		// We should call this method only if we expect to use at least one default value

			if (mStartingDefaultValue == -1)
			{
				// Initializes the default values
				ParameterInfo[] parameterInfos = methodInfo.GetParameters();
				int numberOfParameters = parameterInfos.Length;
				Debug.Assert(numberOfParameters == 3);
				for (int i = 0 ; i < numberOfParameters ; ++i)
				{
					ParameterInfo parameter = parameterInfos[i];
					if (parameter.IsOptional)
					{
						if (mStartingDefaultValue == -1)
						{
							mStartingDefaultValue = i;
						}
						switch (i)
						{
							case 0:	mDefaultArgument1 = (P1)parameter.DefaultValue;	break;
							case 1: mDefaultArgument2 = (P2)parameter.DefaultValue; break;
							case 2: mDefaultArgument3 = (P3)parameter.DefaultValue; break;
						}
					}
				}
				if (mStartingDefaultValue == -1)
				{
					// We did not find any default value, so we have to provide at least N arguments
					mStartingDefaultValue = 3;
				}
			}

			if (providedArguments < mStartingDefaultValue)
			{
				throw new NotSupportedException("Not enough parameters provided");
			}
		}
	}

	public abstract class InvokerParamBase<P1, P2, P3, P4> : InvokerBase
	{
		protected P1 mArgument1;
		protected P1 mDefaultArgument1;
		protected P2 mArgument2;
		protected P2 mDefaultArgument2;
		protected P3 mArgument3;
		protected P3 mDefaultArgument3;
		protected P4 mArgument4;
		protected P4 mDefaultArgument4;
		int mStartingDefaultValue = -1;

		protected void CheckDefaultArguments(int providedArguments, MethodInfo methodInfo)
		{
			Debug.Assert(providedArguments < 4);		// We should call this method only if we expect to use at least one default value

			if (mStartingDefaultValue == -1)
			{
				// Initializes the default values
				ParameterInfo[] parameterInfos = methodInfo.GetParameters();
				int numberOfParameters = parameterInfos.Length;
				Debug.Assert(numberOfParameters == 4);
				for (int i = 0 ; i < numberOfParameters ; ++i)
				{
					ParameterInfo parameter = parameterInfos[i];
					if (parameter.IsOptional)
					{
						if (mStartingDefaultValue == -1)
						{
							mStartingDefaultValue = i;
						}
						switch (i)
						{
							case 0:	mDefaultArgument1 = (P1)parameter.DefaultValue;	break;
							case 1: mDefaultArgument2 = (P2)parameter.DefaultValue; break;
							case 2: mDefaultArgument3 = (P3)parameter.DefaultValue; break;
							case 4: mDefaultArgument4 = (P4)parameter.DefaultValue; break;
						}
					}
				}
				if (mStartingDefaultValue == -1)
				{
					// We did not find any default value, so we have to provide at least N arguments
					mStartingDefaultValue = 4;
				}
			}

			if (providedArguments < mStartingDefaultValue)
			{
				throw new NotSupportedException("Not enough parameters provided");
			}
		}
	}

	public abstract class InvokerParamBase<P1, P2, P3, P4, P5> : InvokerBase
	{
		protected P1 mArgument1;
		protected P1 mDefaultArgument1;
		protected P2 mArgument2;
		protected P2 mDefaultArgument2;
		protected P3 mArgument3;
		protected P3 mDefaultArgument3;
		protected P4 mArgument4;
		protected P4 mDefaultArgument4;
		protected P5 mArgument5;
		protected P5 mDefaultArgument5;
		int mStartingDefaultValue = -1;

		protected void CheckDefaultArguments(int providedArguments, MethodInfo methodInfo)
		{
			Debug.Assert(providedArguments < 5);		// We should call this method only if we expect to use at least one default value

			if (mStartingDefaultValue == -1)
			{
				// Initializes the default values
				ParameterInfo[] parameterInfos = methodInfo.GetParameters();
				int numberOfParameters = parameterInfos.Length;
				Debug.Assert(numberOfParameters == 5);
				for (int i = 0 ; i < numberOfParameters ; ++i)
				{
					ParameterInfo parameter = parameterInfos[i];
					if (parameter.IsOptional)
					{
						if (mStartingDefaultValue == -1)
						{
							mStartingDefaultValue = i;
						}
						switch (i)
						{
							case 0:	mDefaultArgument1 = (P1)parameter.DefaultValue;	break;
							case 1: mDefaultArgument2 = (P2)parameter.DefaultValue; break;
							case 2: mDefaultArgument3 = (P3)parameter.DefaultValue; break;
							case 4: mDefaultArgument4 = (P4)parameter.DefaultValue; break;
							case 5: mDefaultArgument5 = (P5)parameter.DefaultValue; break;
						}
					}
				}
				if (mStartingDefaultValue == -1)
				{
					// We did not find any default value, so we have to provide at least N arguments
					mStartingDefaultValue = 5;
				}
			}

			if (providedArguments < mStartingDefaultValue)
			{
				throw new NotSupportedException("Not enough parameters provided");
			}
		}
	}

	public abstract class InvokerParamBase<P1, P2, P3, P4, P5, P6> : InvokerBase
	{
		protected P1 mArgument1;
		protected P1 mDefaultArgument1;
		protected P2 mArgument2;
		protected P2 mDefaultArgument2;
		protected P3 mArgument3;
		protected P3 mDefaultArgument3;
		protected P4 mArgument4;
		protected P4 mDefaultArgument4;
		protected P5 mArgument5;
		protected P5 mDefaultArgument5;
		protected P6 mArgument6;
		protected P6 mDefaultArgument6;
		int mStartingDefaultValue = -1;

		protected void CheckDefaultArguments(int providedArguments, MethodInfo methodInfo)
		{
			Debug.Assert(providedArguments < 6);		// We should call this method only if we expect to use at least one default value

			if (mStartingDefaultValue == -1)
			{
				// Initializes the default values
				ParameterInfo[] parameterInfos = methodInfo.GetParameters();
				int numberOfParameters = parameterInfos.Length;
				Debug.Assert(numberOfParameters == 6);
				for (int i = 0 ; i < numberOfParameters ; ++i)
				{
					ParameterInfo parameter = parameterInfos[i];
					if (parameter.IsOptional)
					{
						if (mStartingDefaultValue == -1)
						{
							mStartingDefaultValue = i;
						}
						switch (i)
						{
							case 0:	mDefaultArgument1 = (P1)parameter.DefaultValue;	break;
							case 1: mDefaultArgument2 = (P2)parameter.DefaultValue; break;
							case 2: mDefaultArgument3 = (P3)parameter.DefaultValue; break;
							case 4: mDefaultArgument4 = (P4)parameter.DefaultValue; break;
							case 5: mDefaultArgument5 = (P5)parameter.DefaultValue; break;
							case 6: mDefaultArgument6 = (P6)parameter.DefaultValue; break;
						}
					}
				}
				if (mStartingDefaultValue == -1)
				{
					// We did not find any default value, so we have to provide at least N arguments
					mStartingDefaultValue = 6;
				}
			}

			if (providedArguments < mStartingDefaultValue)
			{
				throw new NotSupportedException("Not enough parameters provided");
			}
		}
	}

	public abstract class InvokerParamBase<P1, P2, P3, P4, P5, P6, P7> : InvokerBase
	{
		protected P1 mArgument1;
		protected P1 mDefaultArgument1;
		protected P2 mArgument2;
		protected P2 mDefaultArgument2;
		protected P3 mArgument3;
		protected P3 mDefaultArgument3;
		protected P4 mArgument4;
		protected P4 mDefaultArgument4;
		protected P5 mArgument5;
		protected P5 mDefaultArgument5;
		protected P6 mArgument6;
		protected P6 mDefaultArgument6;
		protected P7 mArgument7;
		protected P7 mDefaultArgument7;
		int mStartingDefaultValue = -1;

		protected void CheckDefaultArguments(int providedArguments, MethodInfo methodInfo)
		{
			Debug.Assert(providedArguments < 7);		// We should call this method only if we expect to use at least one default value

			if (mStartingDefaultValue == -1)
			{
				// Initializes the default values
				ParameterInfo[] parameterInfos = methodInfo.GetParameters();
				int numberOfParameters = parameterInfos.Length;
				Debug.Assert(numberOfParameters == 7);
				for (int i = 0 ; i < numberOfParameters ; ++i)
				{
					ParameterInfo parameter = parameterInfos[i];
					if (parameter.IsOptional)
					{
						if (mStartingDefaultValue == -1)
						{
							mStartingDefaultValue = i;
						}
						switch (i)
						{
							case 0:	mDefaultArgument1 = (P1)parameter.DefaultValue;	break;
							case 1: mDefaultArgument2 = (P2)parameter.DefaultValue; break;
							case 2: mDefaultArgument3 = (P3)parameter.DefaultValue; break;
							case 4: mDefaultArgument4 = (P4)parameter.DefaultValue; break;
							case 5: mDefaultArgument5 = (P5)parameter.DefaultValue; break;
							case 6: mDefaultArgument6 = (P6)parameter.DefaultValue; break;
							case 7: mDefaultArgument7 = (P7)parameter.DefaultValue; break;
						}
					}
				}
				if (mStartingDefaultValue == -1)
				{
					// We did not find any default value, so we have to provide at least N arguments
					mStartingDefaultValue = 7;
				}
			}

			if (providedArguments < mStartingDefaultValue)
			{
				throw new NotSupportedException("Not enough parameters provided");
			}
		}
	}

	public abstract class InvokerParamBase<P1, P2, P3, P4, P5, P6, P7, P8> : InvokerBase
	{
		protected P1 mArgument1;
		protected P1 mDefaultArgument1;
		protected P2 mArgument2;
		protected P2 mDefaultArgument2;
		protected P3 mArgument3;
		protected P3 mDefaultArgument3;
		protected P4 mArgument4;
		protected P4 mDefaultArgument4;
		protected P5 mArgument5;
		protected P5 mDefaultArgument5;
		protected P6 mArgument6;
		protected P6 mDefaultArgument6;
		protected P7 mArgument7;
		protected P7 mDefaultArgument7;
		protected P8 mArgument8;
		protected P8 mDefaultArgument8;
		int mStartingDefaultValue = -1;

		protected void CheckDefaultArguments(int providedArguments, MethodInfo methodInfo)
		{
			Debug.Assert(providedArguments < 8);		// We should call this method only if we expect to use at least one default value

			if (mStartingDefaultValue == -1)
			{
				// Initializes the default values
				ParameterInfo[] parameterInfos = methodInfo.GetParameters();
				int numberOfParameters = parameterInfos.Length;
				Debug.Assert(numberOfParameters == 8);
				for (int i = 0 ; i < numberOfParameters ; ++i)
				{
					ParameterInfo parameter = parameterInfos[i];
					if (parameter.IsOptional)
					{
						if (mStartingDefaultValue == -1)
						{
							mStartingDefaultValue = i;
						}
						switch (i)
						{
							case 0:	mDefaultArgument1 = (P1)parameter.DefaultValue;	break;
							case 1: mDefaultArgument2 = (P2)parameter.DefaultValue; break;
							case 2: mDefaultArgument3 = (P3)parameter.DefaultValue; break;
							case 4: mDefaultArgument4 = (P4)parameter.DefaultValue; break;
							case 5: mDefaultArgument5 = (P5)parameter.DefaultValue; break;
							case 6: mDefaultArgument6 = (P6)parameter.DefaultValue; break;
							case 7: mDefaultArgument7 = (P7)parameter.DefaultValue; break;
							case 8: mDefaultArgument8 = (P8)parameter.DefaultValue; break;
						}
					}
				}
				if (mStartingDefaultValue == -1)
				{
					// We did not find any default value, so we have to provide at least N arguments
					mStartingDefaultValue = 8;
				}
			}

			if (providedArguments < mStartingDefaultValue)
			{
				throw new NotSupportedException("Not enough parameters provided");
			}
		}
	}

	// List of actions

	public class InvokerA : InvokerParamBase, ICallerA
	{
		Action mAction;

		public InvokerA(Action action)
		{
			mAction = action;
		}

		public InvokerA(object target, MethodInfo methodInfo)
		{
			mAction = (Action)Delegate.CreateDelegate(typeof(Action), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if ((arguments != null) && (arguments.Length != 0)) {
				throw new ArgumentException();
			}
		}

		public override object Invoke()
		{
			mAction();
			return null;
		}

		public override void InvokeOverrideA1(object a1)
		{
			throw new InvalidOperationException();
		}

		public override object SafeInvokeWith(object[] args)
		{
			if ((args != null) && (args.Length != 0))
			{
				throw new InvalidOperationException();
			}
			mAction();
			return null;
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			if ((args != null) && (args.Length != 0))
			{
				throw new InvalidOperationException();
			}
			mAction();
			return null;
		}

		void ICallerA.Call()
		{
			mAction();
		}
	}

	public class InvokerA<P1> : InvokerParamBase<P1>, ICallerA, ICallerA<P1>
	{
		Action<P1> mAction;

		public InvokerA(Action<P1> action)
		{
			mAction = action;
		}

		public InvokerA(object target, MethodInfo methodInfo)
		{
			mAction = (Action<P1>)Delegate.CreateDelegate(typeof(Action<P1>), target, methodInfo);
		}

		public InvokerA(Delegate del)
		{
			mAction = (Action<P1>)del;
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 1) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
		}

		public override object Invoke()
		{
			mAction(mArgument1);
			return null;
		}

		public override void InvokeOverrideA1(object a1)
		{
			mAction((P1)a1);
		}

		public override object SafeInvokeWith(object[] args)
		{
			mAction((P1)args[0]);
			return null;
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			mAction((P1)args[0]);
			return null;
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mAction.Method);
			mAction(mDefaultArgument1);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			mAction(a1);
		}
	}

	public class InvokerA<P1, P2> : InvokerParamBase<P1, P2>, ICallerA, ICallerA<P1>, ICallerA<P1, P2>
	{
		Action<P1, P2> mAction;

		public InvokerA(Action<P1, P2> action)
		{
			mAction = action;
		}

		public InvokerA(object target, MethodInfo methodInfo)
		{
			mAction = (Action<P1, P2>)Delegate.CreateDelegate(typeof(Action<P1, P2>), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 2) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
			mArgument2 = (P2)arguments[1];
		}

		public override object Invoke()
		{
			mAction(mArgument1, mArgument2);
			return null;
		}

		public override void InvokeOverrideA1(object a1)
		{
			mAction((P1)a1, mArgument2);
		}

		public override object SafeInvokeWith(object[] args)
		{
			mAction((P1)args[0], (P2)args[1]);
			return null;
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			mAction((P1)args[0], (P2)args[1]);
			return null;
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mAction.Method);
			mAction(mDefaultArgument1, mDefaultArgument2);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mAction.Method);
			mAction(a1, mDefaultArgument2);
		}

		void ICallerA<P1, P2>.Call(P1 a1, P2 a2)
		{
			mAction(a1, a2);
		}
	}

	public class InvokerA<P1, P2, P3> : InvokerParamBase<P1, P2, P3>, ICallerA, ICallerA<P1>, ICallerA<P1, P2>, ICallerA<P1, P2, P3>
	{
		Action<P1, P2, P3> mAction;

		public InvokerA(Action<P1, P2, P3> action)
		{
			mAction = action;
		}

		public InvokerA(object target, MethodInfo methodInfo)
		{
			mAction = (Action<P1, P2, P3>)Delegate.CreateDelegate(typeof(Action<P1, P2, P3>), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 3) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
			mArgument2 = (P2)arguments[1];
			mArgument3 = (P3)arguments[2];
		}

		public override object Invoke()
		{
			mAction(mArgument1, mArgument2, mArgument3);
			return null;
		}

		public override void InvokeOverrideA1(object a1)
		{
			mAction((P1)a1, mArgument2, mArgument3);
		}

		public override object SafeInvokeWith(object[] args)
		{
			mAction((P1)args[0], (P2)args[1], (P3)args[2]);
			return null;
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			mAction((P1)args[0], (P2)args[1], (P3)args[2]);
			return null;
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mAction.Method);
			mAction(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mAction.Method);
			mAction(a1, mDefaultArgument2, mDefaultArgument3);
		}

		void ICallerA<P1, P2>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mAction.Method);
			mAction(a1, a2, mDefaultArgument3);
		}

		void ICallerA<P1, P2, P3>.Call(P1 a1, P2 a2, P3 a3)
		{
			mAction(a1, a2, a3);
		}
	}

	public class InvokerA<P1, P2, P3, P4> : InvokerParamBase<P1, P2, P3, P4>, ICallerA, ICallerA<P1>, ICallerA<P1, P2>, ICallerA<P1, P2, P3>, ICallerA<P1, P2, P3, P4>
	{
		Action<P1, P2, P3, P4> mAction;

		public InvokerA(Action<P1, P2, P3, P4> action)
		{
			mAction = action;
		}

		public InvokerA(object target, MethodInfo methodInfo)
		{
			mAction = (Action<P1, P2, P3, P4>)Delegate.CreateDelegate(typeof(Action<P1, P2, P3, P4>), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 4) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
			mArgument2 = (P2)arguments[1];
			mArgument3 = (P3)arguments[2];
			mArgument4 = (P4)arguments[3];
		}

		public override object Invoke()
		{
			mAction(mArgument1, mArgument2, mArgument3, mArgument4);
			return null;
		}

		public override void InvokeOverrideA1(object a1)
		{
			mAction((P1)a1, mArgument2, mArgument3, mArgument4);
		}

		public override object SafeInvokeWith(object[] args)
		{
			mAction((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3]);
			return null;
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			mAction((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3]);
			return null;
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mAction.Method);
			mAction(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mAction.Method);
			mAction(a1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4);
		}

		void ICallerA<P1, P2>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mAction.Method);
			mAction(a1, a2, mDefaultArgument3, mDefaultArgument4);
		}

		void ICallerA<P1, P2, P3>.Call(P1 a1, P2 a2, P3 a3)
		{
			CheckDefaultArguments(3, mAction.Method);
			mAction(a1, a2, a3, mDefaultArgument4);
		}

		void ICallerA<P1, P2, P3, P4>.Call(P1 a1, P2 a2, P3 a3, P4 a4)
		{
			mAction(a1, a2, a3, a4);
		}
	}

	public class InvokerA<P1, P2, P3, P4, P5> : InvokerParamBase<P1, P2, P3, P4, P5>, ICallerA, ICallerA<P1>, ICallerA<P1, P2>, ICallerA<P1, P2, P3>, ICallerA<P1, P2, P3, P4>, ICallerA<P1, P2, P3, P4, P5>
	{
		Action<P1, P2, P3, P4, P5> mAction;

		public InvokerA(Action<P1, P2, P3, P4, P5> action)
		{
			mAction = action;
		}

		public InvokerA(object target, MethodInfo methodInfo)
		{
			mAction = (Action<P1, P2, P3, P4, P5>)Delegate.CreateDelegate(typeof(Action<P1, P2, P3, P4, P5>), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 5) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
			mArgument2 = (P2)arguments[1];
			mArgument3 = (P3)arguments[2];
			mArgument4 = (P4)arguments[3];
			mArgument5 = (P5)arguments[4];
		}

		public override object Invoke()
		{
			mAction(mArgument1, mArgument2, mArgument3, mArgument4, mArgument5);
			return null;
		}

		public override void InvokeOverrideA1(object a1)
		{
			mAction((P1)a1, mArgument2, mArgument3, mArgument4, mArgument5);
		}

		public override object SafeInvokeWith(object[] args)
		{
			mAction((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4]);
			return null;
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			mAction((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4]);
			return null;
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mAction.Method);
			mAction(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mAction.Method);
			mAction(a1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5);
		}

		void ICallerA<P1, P2>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mAction.Method);
			mAction(a1, a2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5);
		}

		void ICallerA<P1, P2, P3>.Call(P1 a1, P2 a2, P3 a3)
		{
			CheckDefaultArguments(3, mAction.Method);
			mAction(a1, a2, a3, mDefaultArgument4, mDefaultArgument5);
		}

		void ICallerA<P1, P2, P3, P4>.Call(P1 a1, P2 a2, P3 a3, P4 a4)
		{
			CheckDefaultArguments(4, mAction.Method);
			mAction(a1, a2, a3, a4, mDefaultArgument5);
		}

		void ICallerA<P1, P2, P3, P4, P5>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5)
		{
			mAction(a1, a2, a3, a4, a5);
		}
	}

	public class InvokerA<P1, P2, P3, P4, P5, P6> : InvokerParamBase<P1, P2, P3, P4, P5, P6>, ICallerA, ICallerA<P1>, ICallerA<P1, P2>, ICallerA<P1, P2, P3>,
													ICallerA<P1, P2, P3, P4>, ICallerA<P1, P2, P3, P4, P5>, ICallerA<P1, P2, P3, P4, P5, P6>
	{
		Action<P1, P2, P3, P4, P5, P6> mAction;

		public InvokerA(Action<P1, P2, P3, P4, P5, P6> action)
		{
			mAction = action;
		}

		public InvokerA(object target, MethodInfo methodInfo)
		{
			mAction = (Action<P1, P2, P3, P4, P5, P6>)Delegate.CreateDelegate(typeof(Action<P1, P2, P3, P4, P5, P6>), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 6) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
			mArgument2 = (P2)arguments[1];
			mArgument3 = (P3)arguments[2];
			mArgument4 = (P4)arguments[3];
			mArgument5 = (P5)arguments[4];
			mArgument6 = (P6)arguments[5];
		}

		public override object Invoke()
		{
			mAction(mArgument1, mArgument2, mArgument3, mArgument4, mArgument5, mArgument6);
			return null;
		}

		public override void InvokeOverrideA1(object a1)
		{
			mAction((P1)a1, mArgument2, mArgument3, mArgument4, mArgument5, mArgument6);
		}

		public override object SafeInvokeWith(object[] args)
		{
			mAction((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4], (P6)args[5]);
			return null;
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			mAction((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4], (P6)args[5]);
			return null;
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mAction.Method);
			mAction(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4,mDefaultArgument5, mDefaultArgument6);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mAction.Method);
			mAction(a1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6);
		}

		void ICallerA<P1, P2>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mAction.Method);
			mAction(a1, a2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6);
		}

		void ICallerA<P1, P2, P3>.Call(P1 a1, P2 a2, P3 a3)
		{
			CheckDefaultArguments(3, mAction.Method);
			mAction(a1, a2, a3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6);
		}

		void ICallerA<P1, P2, P3, P4>.Call(P1 a1, P2 a2, P3 a3, P4 a4)
		{
			CheckDefaultArguments(4, mAction.Method);
			mAction(a1, a2, a3, a4, mDefaultArgument5, mDefaultArgument6);
		}

		void ICallerA<P1, P2, P3, P4, P5>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5)
		{
			CheckDefaultArguments(5, mAction.Method);
			mAction(a1, a2, a3, a4, a5, mDefaultArgument6);
		}

		void ICallerA<P1, P2, P3, P4, P5, P6>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6)
		{
			mAction(a1, a2, a3, a4, a5, a6);
		}
	}

	public class InvokerA<P1, P2, P3, P4, P5, P6, P7> : InvokerParamBase<P1, P2, P3, P4, P5, P6, P7>, ICallerA, ICallerA<P1>, ICallerA<P1, P2>, ICallerA<P1, P2, P3>,
														ICallerA<P1, P2, P3, P4>, ICallerA<P1, P2, P3, P4, P5>, ICallerA<P1, P2, P3, P4, P5, P6>, ICallerA<P1, P2, P3, P4, P5, P6, P7>
	{
		Action<P1, P2, P3, P4, P5, P6, P7> mAction;

		public InvokerA(Action<P1, P2, P3, P4, P5, P6, P7> action)
		{
			mAction = action;
		}

		public InvokerA(object target, MethodInfo methodInfo)
		{
			mAction = (Action<P1, P2, P3, P4, P5, P6, P7>)Delegate.CreateDelegate(typeof(Action<P1, P2, P3, P4, P5, P6, P7>), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 7) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
			mArgument2 = (P2)arguments[1];
			mArgument3 = (P3)arguments[2];
			mArgument4 = (P4)arguments[3];
			mArgument5 = (P5)arguments[4];
			mArgument6 = (P6)arguments[5];
			mArgument7 = (P7)arguments[7];
		}

		public override object Invoke()
		{
			mAction(mArgument1, mArgument2, mArgument3, mArgument4, mArgument5, mArgument6, mArgument7);
			return null;
		}

		public override void InvokeOverrideA1(object a1)
		{
			mAction((P1)a1, mArgument2, mArgument3, mArgument4, mArgument5, mArgument6, mArgument7);
		}

		public override object SafeInvokeWith(object[] args)
		{
			mAction((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4], (P6)args[5], (P7)args[6]);
			return null;
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			mAction((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4], (P6)args[5], (P7)args[6]);
			return null;
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mAction.Method);
			mAction(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4,mDefaultArgument5, mDefaultArgument6, mDefaultArgument7);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mAction.Method);
			mAction(a1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7);
		}

		void ICallerA<P1, P2>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mAction.Method);
			mAction(a1, a2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7);
		}

		void ICallerA<P1, P2, P3>.Call(P1 a1, P2 a2, P3 a3)
		{
			CheckDefaultArguments(3, mAction.Method);
			mAction(a1, a2, a3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7);
		}

		void ICallerA<P1, P2, P3, P4>.Call(P1 a1, P2 a2, P3 a3, P4 a4)
		{
			CheckDefaultArguments(4, mAction.Method);
			mAction(a1, a2, a3, a4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7);
		}

		void ICallerA<P1, P2, P3, P4, P5>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5)
		{
			CheckDefaultArguments(5, mAction.Method);
			mAction(a1, a2, a3, a4, a5, mDefaultArgument6, mDefaultArgument7);
		}

		void ICallerA<P1, P2, P3, P4, P5, P6>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6)
		{
			CheckDefaultArguments(6, mAction.Method);
			mAction(a1, a2, a3, a4, a5, a6, mDefaultArgument7);
		}

		void ICallerA<P1, P2, P3, P4, P5, P6, P7>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6, P7 a7)
		{
			mAction(a1, a2, a3, a4, a5, a6, a7);
		}
	}

	public class InvokerA<P1, P2, P3, P4, P5, P6, P7, P8> : InvokerParamBase<P1, P2, P3, P4, P5, P6, P7, P8>, ICallerA, ICallerA<P1>, ICallerA<P1, P2>, ICallerA<P1, P2, P3>,
															ICallerA<P1, P2, P3, P4>, ICallerA<P1, P2, P3, P4, P5>, ICallerA<P1, P2, P3, P4, P5, P6>,
															ICallerA<P1, P2, P3, P4, P5, P6, P7>, ICallerA<P1, P2, P3, P4, P5, P6, P7, P8>
	{
		Action<P1, P2, P3, P4, P5, P6, P7, P8> mAction;

		public InvokerA(Action<P1, P2, P3, P4, P5, P6, P7, P8> action)
		{
			mAction = action;
		}

		public InvokerA(object target, MethodInfo methodInfo)
		{
			mAction = (Action<P1, P2, P3, P4, P5, P6, P7, P8>)Delegate.CreateDelegate(typeof(Action<P1, P2, P3, P4, P5, P6, P7, P8>), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 8) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
			mArgument2 = (P2)arguments[1];
			mArgument3 = (P3)arguments[2];
			mArgument4 = (P4)arguments[3];
			mArgument5 = (P5)arguments[4];
			mArgument6 = (P6)arguments[5];
			mArgument7 = (P7)arguments[6];
			mArgument8 = (P8)arguments[7];
		}

		public override object Invoke()
		{
			mAction(mArgument1, mArgument2, mArgument3, mArgument4, mArgument5, mArgument6, mArgument7, mArgument8);
			return null;
		}

		public override void InvokeOverrideA1(object a1)
		{
			mAction((P1)a1, mArgument2, mArgument3, mArgument4, mArgument5, mArgument6, mArgument7, mArgument8);
		}

		public override object SafeInvokeWith(object[] args)
		{
			mAction((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4], (P6)args[5], (P7)args[6], (P8)args[7]);
			return null;
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			mAction((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4], (P6)args[5], (P7)args[6], (P8)args[7]);
			return null;
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mAction.Method);
			mAction(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4,mDefaultArgument5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mAction.Method);
			mAction(a1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		void ICallerA<P1, P2>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mAction.Method);
			mAction(a1, a2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		void ICallerA<P1, P2, P3>.Call(P1 a1, P2 a2, P3 a3)
		{
			CheckDefaultArguments(3, mAction.Method);
			mAction(a1, a2, a3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		void ICallerA<P1, P2, P3, P4>.Call(P1 a1, P2 a2, P3 a3, P4 a4)
		{
			CheckDefaultArguments(4, mAction.Method);
			mAction(a1, a2, a3, a4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		void ICallerA<P1, P2, P3, P4, P5>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5)
		{
			CheckDefaultArguments(5, mAction.Method);
			mAction(a1, a2, a3, a4, a5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		void ICallerA<P1, P2, P3, P4, P5, P6>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6)
		{
			CheckDefaultArguments(6, mAction.Method);
			mAction(a1, a2, a3, a4, a5, a6, mDefaultArgument7, mDefaultArgument8);
		}

		void ICallerA<P1, P2, P3, P4, P5, P6, P7>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6, P7 a7)
		{
			CheckDefaultArguments(7, mAction.Method);
			mAction(a1, a2, a3, a4, a5, a6, a7, mDefaultArgument8);
		}

		void ICallerA<P1, P2, P3, P4, P5, P6, P7, P8>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6, P7 a7, P8 a8)
		{
			mAction(a1, a2, a3, a4, a5, a6, a7, a8);
		}
	}



	// List of functions

	public class InvokerF<R> : InvokerParamBase, ICallerF<R>, ICallerA
	{
		Func<R> mFunc;

		public InvokerF(Func<R> func)
		{
			mFunc = func;
		}

		public InvokerF(object target, MethodInfo methodInfo)
		{
			mFunc = (Func<R>)Delegate.CreateDelegate(typeof(Func<R>), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if ((arguments != null) && (arguments.Length != 0)) {
				throw new ArgumentException();
			}
		}

		public override object Invoke()
		{
			return mFunc();
		}

		public override void InvokeOverrideA1(object a1)
		{
			throw new InvalidOperationException();
		}

		public override object SafeInvokeWith(object[] args)
		{
			if ((args != null) && (args.Length != 0))
			{
				throw new InvalidOperationException();
			}
			return mFunc();
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			if ((args != null) && (args.Length != 0))
			{
				throw new InvalidOperationException();
			}
			return mFunc();
		}

		R ICallerF<R>.Call()
		{
			return mFunc();
		}

		void ICallerA.Call()
		{
			mFunc();
		}
	}

	public class InvokerF<P1, R> : InvokerParamBase<P1>, ICallerF<R>, ICallerA, ICallerF<P1, R>, ICallerA<P1>
	{
		Func<P1, R> mFunc;

		public InvokerF(Func<P1, R> func)
		{
			mFunc = func;
		}

		public InvokerF(object target, MethodInfo methodInfo)
		{
			mFunc = (Func<P1, R>)Delegate.CreateDelegate(typeof(Func<P1, R>), target, methodInfo);
		}

		public InvokerF(Delegate del)
		{
			mFunc = (Func<P1, R>)del;
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 1) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
		}

		public override object Invoke()
		{
			return mFunc(mArgument1);
		}

		public override void InvokeOverrideA1(object a1)
		{
			mFunc((P1)a1);
		}

		public override object SafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0]);
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0]);
		}

		R ICallerF<R>.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			return mFunc(mDefaultArgument1);
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			mFunc(mDefaultArgument1);
		}

		R ICallerF<P1, R>.Call(P1 a1)
		{
			return mFunc(a1);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			mFunc(a1);
		}
	}

	public class InvokerF<P1, P2, R> : InvokerParamBase<P1, P2>, ICallerF<R>, ICallerA, ICallerF<P1, R>, ICallerA<P1>, ICallerF<P1, P2, R>, ICallerA<P1, P2>
	{
		Func<P1, P2, R> mFunc;

		public InvokerF(Func<P1, P2, R> action)
		{
			mFunc = action;
		}

		public InvokerF(object target, MethodInfo methodInfo)
		{
			mFunc = (Func<P1, P2, R>)Delegate.CreateDelegate(typeof(Func<P1, P2, R>), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 2) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
			mArgument2 = (P2)arguments[1];
		}

		public override object Invoke()
		{
			return mFunc(mArgument1, mArgument2);
		}

		public override void InvokeOverrideA1(object a1)
		{
			mFunc((P1)a1, mArgument2);
		}

		public override object SafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0], (P2)args[1]);
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0], (P2)args[1]);
		}

		R ICallerF<R>.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			return mFunc(mDefaultArgument1, mDefaultArgument2);
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			mFunc(mDefaultArgument1, mDefaultArgument2);
		}

		R ICallerF<P1, R>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mFunc.Method);
			return mFunc(a1, mDefaultArgument2);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mFunc.Method);
			mFunc(a1, mDefaultArgument2);
		}

		R ICallerF<P1, P2, R>.Call(P1 a1, P2 a2)
		{
			return mFunc(a1, a2);
		}

		void ICallerA<P1, P2>.Call(P1 a1, P2 a2)
		{
			mFunc(a1, a2);
		}
	}

	public class InvokerF<P1, P2, P3, R> : InvokerParamBase<P1, P2, P3>, ICallerF<R>, ICallerA, ICallerF<P1, R>, ICallerA<P1>, ICallerF<P1, P2, R>, ICallerA<P1, P2>,
											ICallerF<P1, P2, P3, R>, ICallerA<P1, P2, P3>
	{
		Func<P1, P2, P3, R> mFunc;

		public InvokerF(Func<P1, P2, P3, R> func)
		{
			mFunc = func;
		}

		public InvokerF(object target, MethodInfo methodInfo)
		{
			mFunc = (Func<P1, P2, P3, R>)Delegate.CreateDelegate(typeof(Func<P1, P2, P3, R>), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 3) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
			mArgument2 = (P2)arguments[1];
			mArgument3 = (P3)arguments[2];
		}

		public override object Invoke()
		{
			return mFunc(mArgument1, mArgument2, mArgument3);
		}

		public override void InvokeOverrideA1(object a1)
		{
			mFunc((P1)a1, mArgument2, mArgument3);
		}

		public override object SafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0], (P2)args[1], (P3)args[2]);
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0], (P2)args[1], (P3)args[2]);
		}

		R ICallerF<R>.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			return mFunc(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3);
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			mFunc(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3);
		}

		R ICallerF<P1, R>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mFunc.Method);
			return mFunc(a1, mDefaultArgument2, mDefaultArgument3);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mFunc.Method);
			mFunc(a1, mDefaultArgument2, mDefaultArgument3);
		}

		R ICallerF<P1, P2, R>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mFunc.Method);
			return mFunc(a1, a2, mDefaultArgument3);
		}

		void ICallerA<P1, P2>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mFunc.Method);
			mFunc(a1, a2, mDefaultArgument3);
		}

		R ICallerF<P1, P2, P3, R>.Call(P1 a1, P2 a2, P3 a3)
		{
			return mFunc(a1, a2, a3);
		}

		void ICallerA<P1, P2, P3>.Call(P1 a1, P2 a2, P3 a3)
		{
			mFunc(a1, a2, a3);
		}
	}

	public class InvokerF<P1, P2, P3, P4, R> : InvokerParamBase<P1, P2, P3, P4>, ICallerF<R>, ICallerA, ICallerF<P1, R>, ICallerA<P1>, ICallerF<P1, P2, R>, ICallerA<P1, P2>,
												ICallerF<P1, P2, P3, R>, ICallerA<P1, P2, P3>, ICallerF<P1, P2, P3, P4, R>, ICallerA<P1, P2, P3, P4>
	{
		Func<P1, P2, P3, P4, R> mFunc;

		public InvokerF(Func<P1, P2, P3, P4, R> func)
		{
			mFunc = func;
		}

		public InvokerF(object target, MethodInfo methodInfo)
		{
			mFunc = (Func<P1, P2, P3, P4, R>)Delegate.CreateDelegate(typeof(Func<P1, P2, P3, P4, R>), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 4) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
			mArgument2 = (P2)arguments[1];
			mArgument3 = (P3)arguments[2];
			mArgument4 = (P4)arguments[3];
		}

		public override object Invoke()
		{
			return mFunc(mArgument1, mArgument2, mArgument3, mArgument4);
		}

		public override void InvokeOverrideA1(object a1)
		{
			mFunc((P1)a1, mArgument2, mArgument3, mArgument4);
		}

		public override object SafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3]);
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3]);
		}

		R ICallerF<R>.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			return mFunc(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4);
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			mFunc(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4);
		}

		R ICallerF<P1, R>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mFunc.Method);
			return mFunc(a1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mFunc.Method);
			mFunc(a1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4);
		}

		R ICallerF<P1, P2, R>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mFunc.Method);
			return mFunc(a1, a2, mDefaultArgument3, mDefaultArgument4);
		}

		void ICallerA<P1, P2>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mFunc.Method);
			mFunc(a1, a2, mDefaultArgument3, mDefaultArgument4);
		}

		R ICallerF<P1, P2, P3, R>.Call(P1 a1, P2 a2, P3 a3)
		{
			CheckDefaultArguments(3, mFunc.Method);
			return mFunc(a1, a2, a3, mDefaultArgument4);
		}

		void ICallerA<P1, P2, P3>.Call(P1 a1, P2 a2, P3 a3)
		{
			CheckDefaultArguments(3, mFunc.Method);
			mFunc(a1, a2, a3, mDefaultArgument4);
		}

		R ICallerF<P1, P2, P3, P4, R>.Call(P1 a1, P2 a2, P3 a3, P4 a4)
		{
			return mFunc(a1, a2, a3, a4);
		}

		void ICallerA<P1, P2, P3, P4>.Call(P1 a1, P2 a2, P3 a3, P4 a4)
		{
			mFunc(a1, a2, a3, a4);
		}
	}

	public class InvokerF<P1, P2, P3, P4, P5, R> : InvokerParamBase<P1, P2, P3, P4, P5>, ICallerF<R>, ICallerA, ICallerF<P1, R>, ICallerA<P1>, ICallerF<P1, P2, R>, ICallerA<P1, P2>,
													ICallerF<P1, P2, P3, R>, ICallerA<P1, P2, P3>, ICallerF<P1, P2, P3, P4, R>, ICallerA<P1, P2, P3, P4>,
													ICallerF<P1, P2, P3, P4, P5, R>, ICallerA<P1, P2, P3, P4, P5>
	{
		Func<P1, P2, P3, P4, P5, R> mFunc;

		public InvokerF(Func<P1, P2, P3, P4, P5, R> func)
		{
			mFunc = func;
		}

		public InvokerF(object target, MethodInfo methodInfo)
		{
			mFunc = (Func<P1, P2, P3, P4, P5, R>)Delegate.CreateDelegate(typeof(Func<P1, P2, P3, P4, P5, R>), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 5) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
			mArgument2 = (P2)arguments[1];
			mArgument3 = (P3)arguments[2];
			mArgument4 = (P4)arguments[3];
			mArgument5 = (P5)arguments[4];
		}

		public override object Invoke()
		{
			return mFunc(mArgument1, mArgument2, mArgument3, mArgument4, mArgument5);
		}

		public override void InvokeOverrideA1(object a1)
		{
			mFunc((P1)a1, mArgument2, mArgument3, mArgument4, mArgument5);
		}

		public override object SafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4]);
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4]);
		}

		R ICallerF<R>.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			return mFunc(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5);
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			mFunc(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5);
		}

		R ICallerF<P1, R>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mFunc.Method);
			return mFunc(a1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mFunc.Method);
			mFunc(a1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5);
		}

		R ICallerF<P1, P2, R>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mFunc.Method);
			return mFunc(a1, a2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5);
		}

		void ICallerA<P1, P2>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mFunc.Method);
			mFunc(a1, a2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5);
		}

		R ICallerF<P1, P2, P3, R>.Call(P1 a1, P2 a2, P3 a3)
		{
			CheckDefaultArguments(3, mFunc.Method);
			return mFunc(a1, a2, a3, mDefaultArgument4, mDefaultArgument5);
		}

		void ICallerA<P1, P2, P3>.Call(P1 a1, P2 a2, P3 a3)
		{
			CheckDefaultArguments(3, mFunc.Method);
			mFunc(a1, a2, a3, mDefaultArgument4, mDefaultArgument5);
		}

		R ICallerF<P1, P2, P3, P4, R>.Call(P1 a1, P2 a2, P3 a3, P4 a4)
		{
			CheckDefaultArguments(4, mFunc.Method);
			return mFunc(a1, a2, a3, a4, mDefaultArgument5);
		}

		void ICallerA<P1, P2, P3, P4>.Call(P1 a1, P2 a2, P3 a3, P4 a4)
		{
			CheckDefaultArguments(4, mFunc.Method);
			mFunc(a1, a2, a3, a4, mDefaultArgument5);
		}

		R ICallerF<P1, P2, P3, P4, P5, R>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5)
		{
			return mFunc(a1, a2, a3, a4, a5);
		}

		void ICallerA<P1, P2, P3, P4, P5>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5)
		{
			mFunc(a1, a2, a3, a4, a5);
		}
	}

	public class InvokerF<P1, P2, P3, P4, P5, P6, R> : InvokerParamBase<P1, P2, P3, P4, P5, P6>, ICallerF<R>, ICallerA, ICallerF<P1, R>, ICallerA<P1>, ICallerF<P1, P2, R>, ICallerA<P1, P2>,
														ICallerF<P1, P2, P3, R>, ICallerA<P1, P2, P3>, ICallerF<P1, P2, P3, P4, R>, ICallerA<P1, P2, P3, P4>,
														ICallerF<P1, P2, P3, P4, P5, R>, ICallerA<P1, P2, P3, P4, P5>, ICallerF<P1, P2, P3, P4, P5, P6, R>, ICallerA<P1, P2, P3, P4, P5, P6>
	{
		Func<P1, P2, P3, P4, P5, P6, R> mFunc;

		public InvokerF(Func<P1, P2, P3, P4, P5, P6, R> func)
		{
			mFunc = func;
		}

		public InvokerF(object target, MethodInfo methodInfo)
		{
			mFunc = (Func<P1, P2, P3, P4, P5, P6, R>)Delegate.CreateDelegate(typeof(Func<P1, P2, P3, P4, P5, P6, R>), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 6) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
			mArgument2 = (P2)arguments[1];
			mArgument3 = (P3)arguments[2];
			mArgument4 = (P4)arguments[3];
			mArgument5 = (P5)arguments[4];
			mArgument6 = (P6)arguments[5];
		}

		public override object Invoke()
		{
			return mFunc(mArgument1, mArgument2, mArgument3, mArgument4, mArgument5, mArgument6);
		}

		public override void InvokeOverrideA1(object a1)
		{
			mFunc((P1)a1, mArgument2, mArgument3, mArgument4, mArgument5, mArgument6);
		}

		public override object SafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4], (P6)args[5]);
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4], (P6)args[5]);
		}

		R ICallerF<R>.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			return mFunc(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6);
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			mFunc(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6);
		}

		R ICallerF<P1, R>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mFunc.Method);
			return mFunc(a1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mFunc.Method);
			mFunc(a1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6);
		}

		R ICallerF<P1, P2, R>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mFunc.Method);
			return mFunc(a1, a2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6);
		}

		void ICallerA<P1, P2>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mFunc.Method);
			mFunc(a1, a2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6);
		}

		R ICallerF<P1, P2, P3, R>.Call(P1 a1, P2 a2, P3 a3)
		{
			CheckDefaultArguments(3, mFunc.Method);
			return mFunc(a1, a2, a3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6);
		}

		void ICallerA<P1, P2, P3>.Call(P1 a1, P2 a2, P3 a3)
		{
			CheckDefaultArguments(3, mFunc.Method);
			mFunc(a1, a2, a3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6);
		}

		R ICallerF<P1, P2, P3, P4, R>.Call(P1 a1, P2 a2, P3 a3, P4 a4)
		{
			CheckDefaultArguments(4, mFunc.Method);
			return mFunc(a1, a2, a3, a4, mDefaultArgument5, mDefaultArgument6);
		}

		void ICallerA<P1, P2, P3, P4>.Call(P1 a1, P2 a2, P3 a3, P4 a4)
		{
			CheckDefaultArguments(4, mFunc.Method);
			mFunc(a1, a2, a3, a4, mDefaultArgument5, mDefaultArgument6);
		}

		R ICallerF<P1, P2, P3, P4, P5, R>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5)
		{
			CheckDefaultArguments(5, mFunc.Method);
			return mFunc(a1, a2, a3, a4, a5, mDefaultArgument6);
		}

		void ICallerA<P1, P2, P3, P4, P5>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5)
		{
			CheckDefaultArguments(5, mFunc.Method);
			mFunc(a1, a2, a3, a4, a5, mDefaultArgument6);
		}

		R ICallerF<P1, P2, P3, P4, P5, P6, R>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6)
		{
			return mFunc(a1, a2, a3, a4, a5, a6);
		}

		void ICallerA<P1, P2, P3, P4, P5, P6>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6)
		{
			mFunc(a1, a2, a3, a4, a5, a6);
		}
	}

	public class InvokerF<P1, P2, P3, P4, P5, P6, P7, R> : InvokerParamBase<P1, P2, P3, P4, P5, P6, P7>, ICallerF<R>, ICallerA, ICallerF<P1, R>, ICallerA<P1>, ICallerF<P1, P2, R>, ICallerA<P1, P2>,
														ICallerF<P1, P2, P3, R>, ICallerA<P1, P2, P3>, ICallerF<P1, P2, P3, P4, R>, ICallerA<P1, P2, P3, P4>,
														ICallerF<P1, P2, P3, P4, P5, R>, ICallerA<P1, P2, P3, P4, P5>, ICallerF<P1, P2, P3, P4, P5, P6, R>, ICallerA<P1, P2, P3, P4, P5, P6>,
														ICallerF<P1, P2, P3, P4, P5, P6, P7, R>, ICallerA<P1, P2, P3, P4, P5, P6, P7>
	{
		Func<P1, P2, P3, P4, P5, P6, P7, R> mFunc;

		public InvokerF(Func<P1, P2, P3, P4, P5, P6, P7, R> func)
		{
			mFunc = func;
		}

		public InvokerF(object target, MethodInfo methodInfo)
		{
			mFunc = (Func<P1, P2, P3, P4, P5, P6, P7, R>)Delegate.CreateDelegate(typeof(Func<P1, P2, P3, P4, P5, P6, P7, R>), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 6) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
			mArgument2 = (P2)arguments[1];
			mArgument3 = (P3)arguments[2];
			mArgument4 = (P4)arguments[3];
			mArgument5 = (P5)arguments[4];
			mArgument6 = (P6)arguments[5];
			mArgument7 = (P7)arguments[6];
		}

		public override object Invoke()
		{
			return mFunc(mArgument1, mArgument2, mArgument3, mArgument4, mArgument5, mArgument6, mArgument7);
		}

		public override void InvokeOverrideA1(object a1)
		{
			mFunc((P1)a1, mArgument2, mArgument3, mArgument4, mArgument5, mArgument6, mArgument7);
		}

		public override object SafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4], (P6)args[5], (P7)args[6]);
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4], (P6)args[5], (P7)args[6]);
		}

		R ICallerF<R>.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			return mFunc(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7);
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			mFunc(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7);
		}

		R ICallerF<P1, R>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mFunc.Method);
			return mFunc(a1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mFunc.Method);
			mFunc(a1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7);
		}

		R ICallerF<P1, P2, R>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mFunc.Method);
			return mFunc(a1, a2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7);
		}

		void ICallerA<P1, P2>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mFunc.Method);
			mFunc(a1, a2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7);
		}

		R ICallerF<P1, P2, P3, R>.Call(P1 a1, P2 a2, P3 a3)
		{
			CheckDefaultArguments(3, mFunc.Method);
			return mFunc(a1, a2, a3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7);
		}

		void ICallerA<P1, P2, P3>.Call(P1 a1, P2 a2, P3 a3)
		{
			CheckDefaultArguments(3, mFunc.Method);
			mFunc(a1, a2, a3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7);
		}

		R ICallerF<P1, P2, P3, P4, R>.Call(P1 a1, P2 a2, P3 a3, P4 a4)
		{
			CheckDefaultArguments(4, mFunc.Method);
			return mFunc(a1, a2, a3, a4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7);
		}

		void ICallerA<P1, P2, P3, P4>.Call(P1 a1, P2 a2, P3 a3, P4 a4)
		{
			CheckDefaultArguments(4, mFunc.Method);
			mFunc(a1, a2, a3, a4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7);
		}

		R ICallerF<P1, P2, P3, P4, P5, R>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5)
		{
			CheckDefaultArguments(5, mFunc.Method);
			return mFunc(a1, a2, a3, a4, a5, mDefaultArgument6, mDefaultArgument7);
		}

		void ICallerA<P1, P2, P3, P4, P5>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5)
		{
			CheckDefaultArguments(5, mFunc.Method);
			mFunc(a1, a2, a3, a4, a5, mDefaultArgument6, mDefaultArgument7);
		}

		R ICallerF<P1, P2, P3, P4, P5, P6, R>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6)
		{
			CheckDefaultArguments(6, mFunc.Method);
			return mFunc(a1, a2, a3, a4, a5, a6, mDefaultArgument7);
		}

		void ICallerA<P1, P2, P3, P4, P5, P6>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6)
		{
			CheckDefaultArguments(6, mFunc.Method);
			mFunc(a1, a2, a3, a4, a5, a6, mDefaultArgument7);
		}

		R ICallerF<P1, P2, P3, P4, P5, P6, P7, R>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6, P7 a7)
		{
			return mFunc(a1, a2, a3, a4, a5, a6, a7);
		}

		void ICallerA<P1, P2, P3, P4, P5, P6, P7>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6, P7 a7)
		{
			mFunc(a1, a2, a3, a4, a5, a6, a7);
		}
	}

	public class InvokerF<P1, P2, P3, P4, P5, P6, P7, P8, R> : InvokerParamBase<P1, P2, P3, P4, P5, P6, P7, P8>, ICallerF<R>, ICallerA, ICallerF<P1, R>, ICallerA<P1>, ICallerF<P1, P2, R>, ICallerA<P1, P2>,
																ICallerF<P1, P2, P3, R>, ICallerA<P1, P2, P3>, ICallerF<P1, P2, P3, P4, R>, ICallerA<P1, P2, P3, P4>,
																ICallerF<P1, P2, P3, P4, P5, R>, ICallerA<P1, P2, P3, P4, P5>, ICallerF<P1, P2, P3, P4, P5, P6, R>, ICallerA<P1, P2, P3, P4, P5, P6>,
																ICallerF<P1, P2, P3, P4, P5, P6, P7, R>, ICallerA<P1, P2, P3, P4, P5, P6, P7>,
																ICallerF<P1, P2, P3, P4, P5, P6, P7, P8, R>, ICallerA<P1, P2, P3, P4, P5, P6, P7, P8>
	{
		Func<P1, P2, P3, P4, P5, P6, P7, P8, R> mFunc;

		public InvokerF(Func<P1, P2, P3, P4, P5, P6, P7, P8, R> func)
		{
			mFunc = func;
		}

		public InvokerF(object target, MethodInfo methodInfo)
		{
			mFunc = (Func<P1, P2, P3, P4, P5, P6, P7, P8, R>)Delegate.CreateDelegate(typeof(Func<P1, P2, P3, P4, P5, P6, P7, P8, R>), target, methodInfo);
		}

		public override void SetArguments(object[] arguments)
		{
			if (arguments.Length != 6) {
				throw new ArgumentException();
			}

			mArgument1 = (P1)arguments[0];
			mArgument2 = (P2)arguments[1];
			mArgument3 = (P3)arguments[2];
			mArgument4 = (P4)arguments[3];
			mArgument5 = (P5)arguments[4];
			mArgument6 = (P6)arguments[5];
			mArgument7 = (P7)arguments[6];
			mArgument8 = (P8)arguments[7];
		}

		public override object Invoke()
		{
			return mFunc(mArgument1, mArgument2, mArgument3, mArgument4, mArgument5, mArgument6, mArgument7, mArgument8);
		}

		public override void InvokeOverrideA1(object a1)
		{
			mFunc((P1)a1, mArgument2, mArgument3, mArgument4, mArgument5, mArgument6, mArgument7, mArgument8);
		}

		public override object SafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4], (P6)args[5], (P7)args[6], (P8)args[7]);
		}

		public override object UnsafeInvokeWith(object[] args)
		{
			return mFunc((P1)args[0], (P2)args[1], (P3)args[2], (P4)args[3], (P5)args[4], (P6)args[5], (P7)args[6], (P8)args[7]);
		}

		R ICallerF<R>.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			return mFunc(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		void ICallerA.Call()
		{
			CheckDefaultArguments(0, mFunc.Method);
			mFunc(mDefaultArgument1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		R ICallerF<P1, R>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mFunc.Method);
			return mFunc(a1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		void ICallerA<P1>.Call(P1 a1)
		{
			CheckDefaultArguments(1, mFunc.Method);
			mFunc(a1, mDefaultArgument2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		R ICallerF<P1, P2, R>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mFunc.Method);
			return mFunc(a1, a2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		void ICallerA<P1, P2>.Call(P1 a1, P2 a2)
		{
			CheckDefaultArguments(2, mFunc.Method);
			mFunc(a1, a2, mDefaultArgument3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		R ICallerF<P1, P2, P3, R>.Call(P1 a1, P2 a2, P3 a3)
		{
			CheckDefaultArguments(3, mFunc.Method);
			return mFunc(a1, a2, a3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		void ICallerA<P1, P2, P3>.Call(P1 a1, P2 a2, P3 a3)
		{
			CheckDefaultArguments(3, mFunc.Method);
			mFunc(a1, a2, a3, mDefaultArgument4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		R ICallerF<P1, P2, P3, P4, R>.Call(P1 a1, P2 a2, P3 a3, P4 a4)
		{
			CheckDefaultArguments(4, mFunc.Method);
			return mFunc(a1, a2, a3, a4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		void ICallerA<P1, P2, P3, P4>.Call(P1 a1, P2 a2, P3 a3, P4 a4)
		{
			CheckDefaultArguments(4, mFunc.Method);
			mFunc(a1, a2, a3, a4, mDefaultArgument5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		R ICallerF<P1, P2, P3, P4, P5, R>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5)
		{
			CheckDefaultArguments(5, mFunc.Method);
			return mFunc(a1, a2, a3, a4, a5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		void ICallerA<P1, P2, P3, P4, P5>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5)
		{
			CheckDefaultArguments(5, mFunc.Method);
			mFunc(a1, a2, a3, a4, a5, mDefaultArgument6, mDefaultArgument7, mDefaultArgument8);
		}

		R ICallerF<P1, P2, P3, P4, P5, P6, R>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6)
		{
			CheckDefaultArguments(6, mFunc.Method);
			return mFunc(a1, a2, a3, a4, a5, a6, mDefaultArgument7, mDefaultArgument8);
		}

		void ICallerA<P1, P2, P3, P4, P5, P6>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6)
		{
			CheckDefaultArguments(6, mFunc.Method);
			mFunc(a1, a2, a3, a4, a5, a6, mDefaultArgument7, mDefaultArgument8);
		}

		R ICallerF<P1, P2, P3, P4, P5, P6, P7, R>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6, P7 a7)
		{
			CheckDefaultArguments(7, mFunc.Method);
			return mFunc(a1, a2, a3, a4, a5, a6, a7, mDefaultArgument8);
		}

		void ICallerA<P1, P2, P3, P4, P5, P6, P7>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6, P7 a7)
		{
			CheckDefaultArguments(7, mFunc.Method);
			mFunc(a1, a2, a3, a4, a5, a6, a7, mDefaultArgument8);
		}

		R ICallerF<P1, P2, P3, P4, P5, P6, P7, P8, R>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6, P7 a7, P8 a8)
		{
			return mFunc(a1, a2, a3, a4, a5, a6, a7, a8);
		}

		void ICallerA<P1, P2, P3, P4, P5, P6, P7, P8>.Call(P1 a1, P2 a2, P3 a3, P4 a4, P5 a5, P6 a6, P7 a7, P8 a8)
		{
			mFunc(a1, a2, a3, a4, a5, a6, a7, a8);
		}
	}

	// All the corresponding factories

	public abstract class InvokerFactoryBase
	{
		public abstract InvokerBase CreateInvoker(object target, MethodInfo methodInfo);
		public abstract MethodSignature GetMethodSignature();
	}

	public class DefaultInvokerFactory : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new DynamicInvoker(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			// This factory handles all method signatures, so it can't return a defining method signature
			throw new NotSupportedException();
		}
	}

	// Action factories

	public class InvokerAFactory : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerA(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			return new MethodSignature(typeof(void), null);
		}
	}

	public class InvokerAFactory<P1> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerA<P1>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1) };
			return new MethodSignature(typeof(void), parameterTypes);
		}
	}

	public class InvokerAFactory<P1, P2> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerA<P1, P2>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2) };
			return new MethodSignature(typeof(void), parameterTypes);
		}
	}

	public class InvokerAFactory<P1, P2, P3> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerA<P1, P2, P3>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2), typeof(P3) };
			return new MethodSignature(typeof(void), parameterTypes);
		}
	}

	public class InvokerAFactory<P1, P2, P3, P4> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerA<P1, P2, P3, P4>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2), typeof(P3), typeof(P4) };
			return new MethodSignature(typeof(void), parameterTypes);
		}
	}

	public class InvokerAFactory<P1, P2, P3, P4, P5> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerA<P1, P2, P3, P4, P5>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2), typeof(P3), typeof(P4), typeof(P5) };
			return new MethodSignature(typeof(void), parameterTypes);
		}
	}

	public class InvokerAFactory<P1, P2, P3, P4, P5, P6> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerA<P1, P2, P3, P4, P5, P6>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2), typeof(P3), typeof(P4), typeof(P5), typeof(P6) };
			return new MethodSignature(typeof(void), parameterTypes);
		}
	}

	public class InvokerAFactory<P1, P2, P3, P4, P5, P6, P7> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerA<P1, P2, P3, P4, P5, P6, P7>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2), typeof(P3), typeof(P4), typeof(P5), typeof(P6), typeof(P7) };
			return new MethodSignature(typeof(void), parameterTypes);
		}
	}

	public class InvokerAFactory<P1, P2, P3, P4, P5, P6, P7, P8> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerA<P1, P2, P3, P4, P5, P6, P7, P8>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2), typeof(P3), typeof(P4), typeof(P5), typeof(P6), typeof(P7), typeof(P8) };
			return new MethodSignature(typeof(void), parameterTypes);
		}
	}

	// Function factories

	public class InvokerFFactory<R> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerF<R>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			return new MethodSignature(typeof(R), null);
		}
	}

	public class InvokerFFactory<P1, R> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerF<P1, R>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1) };
			return new MethodSignature(typeof(R), parameterTypes);
		}
	}

	public class InvokerFFactory<P1, P2, R> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerF<P1, P2, R>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2) };
			return new MethodSignature(typeof(R), parameterTypes);
		}
	}

	public class InvokerFFactory<P1, P2, P3, R> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerF<P1, P2, P3, R>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2), typeof(P3) };
			return new MethodSignature(typeof(R), parameterTypes);
		}
	}

	public class InvokerFFactory<P1, P2, P3, P4, R> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerF<P1, P2, P3, P4, R>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2), typeof(P3), typeof(P4) };
			return new MethodSignature(typeof(R), parameterTypes);
		}
	}

	public class InvokerFFactory<P1, P2, P3, P4, P5, R> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerF<P1, P2, P3, P4, P5, R>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2), typeof(P3), typeof(P4), typeof(P5) };
			return new MethodSignature(typeof(R), parameterTypes);
		}
	}

	public class InvokerFFactory<P1, P2, P3, P4, P5, P6, R> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerF<P1, P2, P3, P4, P5, P6, R>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2), typeof(P3), typeof(P4), typeof(P5), typeof(P6) };
			return new MethodSignature(typeof(R), parameterTypes);
		}
	}

	public class InvokerFFactory<P1, P2, P3, P4, P5, P6, P7, R> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerF<P1, P2, P3, P4, P5, P6, P7, R>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2), typeof(P3), typeof(P4), typeof(P5), typeof(P6), typeof(P7) };
			return new MethodSignature(typeof(R), parameterTypes);
		}
	}

	public class InvokerFFactory<P1, P2, P3, P4, P5, P6, P7, P8, R> : InvokerFactoryBase
	{
		public override InvokerBase CreateInvoker(object target, MethodInfo methodInfo)
		{
			return new InvokerF<P1, P2, P3, P4, P5, P6, P7, P8, R>(target, methodInfo);
		}

		public override MethodSignature GetMethodSignature()
		{
			Type[] parameterTypes = new Type[] { typeof(P1), typeof(P2), typeof(P3), typeof(P4), typeof(P5), typeof(P6), typeof(P7), typeof(P8) };
			return new MethodSignature(typeof(R), parameterTypes);
		}
	}


}



