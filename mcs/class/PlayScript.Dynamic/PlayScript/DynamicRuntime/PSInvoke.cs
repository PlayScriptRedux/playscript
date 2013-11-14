
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
using System.Reflection;
using PlayScript;
using System.Collections.Generic;
using System.Diagnostics;

namespace PlayScript.DynamicRuntime
{
	[DebuggerStepThrough]
	public class PSInvoke
	{
		// As we are single threaded, we can actually share the same args array for all callers with the same number of parameters.
		private static object[] sArgs = new object[9];
		// We can't do that for the converted arguments are they are different for each call sites (and potentially within the same call site)
		// Size is adjusted everytime. We could have a pool per size to make GC life easier :)
		private object[] mConvertedArgs;

		public PSInvoke (int argCount)
		{
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public void InvokeAction0 (object d)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Action a = d as Action;
			if (a != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				a();
				return;
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 0, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public void InvokeAction1<A1> (object d, A1 a1)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Action<A1> a = d as Action<A1>;
			if (a != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				a(a1);
				return;
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 1, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public void InvokeAction2<A1, A2> (object d, A1 a1, A2 a2)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Action<A1, A2> a = d as Action<A1, A2>;
			if (a != null)
			{
				a(a1, a2);
				return;
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 2, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public void InvokeAction3<A1, A2, A3> (object d, A1 a1, A2 a2, A3 a3)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Action<A1, A2, A3> a = d as Action<A1, A2, A3>;
			if (a != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				a(a1, a2, a3);
				return;
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			sArgs [2] = a3;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 3, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public void InvokeAction4<A1, A2, A3, A4> (object d, A1 a1, A2 a2, A3 a3, A4 a4)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Action<A1, A2, A3, A4> a = d as Action<A1, A2, A3, A4>;
			if (a != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				a(a1, a2, a3, a4);
				return;
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			sArgs [2] = a3;
			sArgs [3] = a4;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 4, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public void InvokeAction5<A1, A2, A3, A4, A5> (object d, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Action<A1, A2, A3, A4, A5> a = d as Action<A1, A2, A3, A4, A5>;
			if (a != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				a(a1, a2, a3, a4, a5);
				return;
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			sArgs [2] = a3;
			sArgs [3] = a4;
			sArgs [4] = a5;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 5, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public void InvokeAction6<A1, A2, A3, A4, A5, A6> (object d, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Action<A1, A2, A3, A4, A5, A6> a = d as Action<A1, A2, A3, A4, A5, A6>;
			if (a != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				a(a1, a2, a3, a4, a5, a6);
				return;
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			sArgs [2] = a3;
			sArgs [3] = a4;
			sArgs [4] = a5;
			sArgs [5] = a6;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 6, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public void InvokeAction7<A1, A2, A3, A4, A5, A6, A7> (object d, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Action<A1, A2, A3, A4, A5, A6, A7> a = d as Action<A1, A2, A3, A4, A5, A6, A7>;
			if (a != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				a(a1, a2, a3, a4, a5, a6, a7);
				return;
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			sArgs [2] = a3;
			sArgs [3] = a4;
			sArgs [4] = a5;
			sArgs [5] = a6;
			sArgs [6] = a7;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 7, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public void InvokeAction8<A1, A2, A3, A4, A5, A6, A7, A8> (object d, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7, A8 a8)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Action<A1, A2, A3, A4, A5, A6, A7, A8> a = d as Action<A1, A2, A3, A4, A5, A6, A7, A8>;
			if (a != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				a(a1, a2, a3, a4, a5, a6, a7, a8);
				return;
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			sArgs [2] = a3;
			sArgs [3] = a4;
			sArgs [4] = a5;
			sArgs [5] = a6;
			sArgs [6] = a7;
			sArgs [7] = a8;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 8, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public void InvokeAction9<A1, A2, A3, A4, A5, A6, A7, A8, A9> (object d, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7, A8 a8, A9 a9)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Action<A1, A2, A3, A4, A5, A6, A7, A8, A9> a = d as Action<A1, A2, A3, A4, A5, A6, A7, A8, A9>;
			if (a != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				a(a1, a2, a3, a4, a5, a6, a7, a8, a9);
				return;
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			sArgs [2] = a3;
			sArgs [3] = a4;
			sArgs [4] = a5;
			sArgs [5] = a6;
			sArgs [6] = a7;
			sArgs [7] = a8;
			sArgs [8] = a9;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 9, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public TR InvokeFunc0<TR> (object d)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Func<TR> f = d as Func<TR>;
			if (f != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				return f();
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 0, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return (TR)del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public TR InvokeFunc1<A1, TR> (object d, A1 a1)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Func<A1, TR> f = d as Func<A1, TR>;
			if (f != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				return f(a1);
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 1, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return (TR)del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public TR InvokeFunc2<A1, A2, TR> (object d, A1 a1, A2 a2)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Func<A1, A2, TR> f = d as Func<A1, A2, TR>;
			if (f != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				return f(a1, a2);
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 2, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return (TR)del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public TR InvokeFunc3<A1, A2, A3, TR> (object d, A1 a1, A2 a2, A3 a3)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Func<A1, A2, A3, TR> f = d as Func<A1, A2, A3, TR>;
			if (f != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				return f(a1, a2, a3);
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			sArgs [2] = a3;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 3, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return (TR)del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public TR InvokeFunc4<A1, A2, A3, A4, TR> (object d, A1 a1, A2 a2, A3 a3, A4 a4)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Func<A1, A2, A3, A4, TR> f = d as Func<A1, A2, A3, A4, TR>;
			if (f != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				return f(a1, a2, a3, a4);
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			sArgs [2] = a3;
			sArgs [3] = a4;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 4, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return (TR)del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public TR InvokeFunc5<A1, A2, A3, A4, A5, TR> (object d, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Func<A1, A2, A3, A4, A5, TR> f = d as Func<A1, A2, A3, A4, A5, TR>;
			if (f != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				return f(a1, a2, a3, a4, a5);
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			sArgs [2] = a3;
			sArgs [3] = a4;
			sArgs [4] = a5;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 5, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return (TR)del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public TR InvokeFunc6<A1, A2, A3, A4, A5, A6, TR> (object d, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Func<A1, A2, A3, A4, A5, A6, TR> f = d as Func<A1, A2, A3, A4, A5, A6, TR>;
			if (f != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				return f(a1, a2, a3, a4, a5, a6);
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			sArgs [2] = a3;
			sArgs [3] = a4;
			sArgs [4] = a5;
			sArgs [5] = a6;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 6, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return (TR)del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public TR InvokeFunc7<A1, A2, A3, A4, A5, A6, A7, TR> (object d, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Func<A1, A2, A3, A4, A5, A6, A7, TR> f = d as Func<A1, A2, A3, A4, A5, A6, A7, TR>;
			if (f != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				return f(a1, a2, a3, a4, a5, a6, a7);
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			sArgs [2] = a3;
			sArgs [3] = a4;
			sArgs [4] = a5;
			sArgs [5] = a6;
			sArgs [6] = a7;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 7, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return (TR)del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public TR InvokeFunc8<A1, A2, A3, A4, A5, A6, A7, A8, TR> (object d, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7, A8 a8)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Func<A1, A2, A3, A4, A5, A6, A7, A8, TR> f = d as Func<A1, A2, A3, A4, A5, A6, A7, A8, TR>;
			if (f != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				return f(a1, a2, a3, a4, a5, a6, a7, a8);
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			sArgs [2] = a3;
			sArgs [3] = a4;
			sArgs [4] = a5;
			sArgs [5] = a6;
			sArgs [6] = a7;
			sArgs [7] = a8;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 8, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return (TR)del.DynamicInvoke (mConvertedArgs);
		}

		[DebuggerHidden]
		[DebuggerStepThrough]
		public TR InvokeFunc9<A1, A2, A3, A4, A5, A6, A7, A8, A9, TR> (object d, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7, A8 a8, A9 a9)
		{
			Stats.Increment(StatsCounter.InvokeBinderInvoked);

			TypeLogger.LogType(d);

			Func<A1, A2, A3, A4, A5, A6, A7, A8, A9, TR> f = d as Func<A1, A2, A3, A4, A5, A6, A7, A8, A9, TR>;
			if (f != null)
			{
				Stats.Increment(StatsCounter.InvokeBinderInvoked_Fast);

				return f(a1, a2, a3, a4, a5, a6, a7, a8, a9);
			}

			Stats.Increment(StatsCounter.InvokeBinderInvoked_Slow);

			Delegate del = (Delegate)d;
			sArgs [0] = a1;
			sArgs [1] = a2;
			sArgs [2] = a3;
			sArgs [3] = a4;
			sArgs [4] = a5;
			sArgs [5] = a6;
			sArgs [6] = a7;
			sArgs [7] = a8;
			sArgs [8] = a9;
			bool canConvert = MethodBinder.ConvertArguments(del.Method, null, sArgs, 9, ref mConvertedArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return (TR)del.DynamicInvoke (mConvertedArgs);
		}
	}

}

#endif
