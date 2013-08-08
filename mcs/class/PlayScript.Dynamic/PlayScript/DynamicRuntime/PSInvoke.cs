
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
	public class PSInvoke 
	{
		private Delegate _d;
		private object[] _args;
		private object[] _params;
		private object[][] _targetArray;
		private int[] _targetIndex;

		public PSInvoke (int argCount)
		{
		}

		public void InvokeAction0 (object d)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			
			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 1);
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			_d.DynamicInvoke (outArgs);
		}
		
		public void InvokeAction1 (object d, object a1)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			
			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 2);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
			} else {
				_args [0] = a1;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			_d.DynamicInvoke (outArgs);
		}
		
		public void InvokeAction2 (object d, object a1, object a2)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			
			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 3);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;

			} else {
				_args [0] = a1;
				_args [1] = a2;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			_d.DynamicInvoke (outArgs);
		}
		
		public void InvokeAction3 (object d, object a1, object a2, object a3)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			
			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 4);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;
				_targetArray [2] [_targetIndex[2]] = a3;

			} else {
				_args [0] = a1;
				_args [1] = a2;
				_args [2] = a3;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			_d.DynamicInvoke (outArgs);
		}
		
		public void InvokeAction4 (object d, object a1, object a2, object a3, object a4)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			
			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 5);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;
				_targetArray [2] [_targetIndex[2]] = a3;
				_targetArray [3] [_targetIndex[3]] = a4;
			} else {
				_args [0] = a1;
				_args [1] = a2;
				_args [2] = a3;
				_args [3] = a4;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			_d.DynamicInvoke (outArgs);
		}
		
		public void InvokeAction5 (object d, object a1, object a2, object a3, object a4, object a5)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			
			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 6);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;
				_targetArray [2] [_targetIndex[2]] = a3;
				_targetArray [3] [_targetIndex[3]] = a4;
				_targetArray [4] [_targetIndex[4]] = a5;
			} else {
				_args [0] = a1;
				_args [1] = a2;
				_args [2] = a3;
				_args [3] = a4;
				_args [4] = a5;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			_d.DynamicInvoke (outArgs);
		}
		
		public void InvokeAction6 (object d, object a1, object a2, object a3, object a4, object a5, object a6)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			
			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 7);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;
				_targetArray [2] [_targetIndex[2]] = a3;
				_targetArray [3] [_targetIndex[3]] = a4;
				_targetArray [4] [_targetIndex[4]] = a5;
				_targetArray [5] [_targetIndex[5]] = a6;
			} else {
				_args [0] = a1;
				_args [1] = a2;
				_args [2] = a3;
				_args [3] = a4;
				_args [4] = a5;
				_args [5] = a6;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			_d.DynamicInvoke (outArgs);
		}
		
		public void InvokeAction7 (object d, object a1, object a2, object a3, object a4, object a5, object a6, object a7)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			
			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 8);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;
				_targetArray [2] [_targetIndex[2]] = a3;
				_targetArray [3] [_targetIndex[3]] = a4;
				_targetArray [4] [_targetIndex[4]] = a5;
				_targetArray [5] [_targetIndex[5]] = a6;
				_targetArray [6] [_targetIndex[6]] = a7;
			} else {
				_args [0] = a1;
				_args [1] = a2;
				_args [2] = a3;
				_args [3] = a4;
				_args [4] = a5;
				_args [5] = a6;
				_args [6] = a7;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			_d.DynamicInvoke (outArgs);
		}
		
		public void InvokeAction8 (object d, object a1, object a2, object a3, object a4, object a5, object a6, object a7, object a8)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			
			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 9);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;
				_targetArray [2] [_targetIndex[2]] = a3;
				_targetArray [3] [_targetIndex[3]] = a4;
				_targetArray [4] [_targetIndex[4]] = a5;
				_targetArray [5] [_targetIndex[5]] = a6;
				_targetArray [6] [_targetIndex[6]] = a7;
				_targetArray [7] [_targetIndex[7]] = a8;
			} else {
				_args [0] = a1;
				_args [1] = a2;
				_args [2] = a3;
				_args [3] = a4;
				_args [4] = a5;
				_args [5] = a6;
				_args [6] = a7;
				_args [7] = a8;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			_d.DynamicInvoke (outArgs);
		}
		
		public void InvokeAction9 (object d, object a1, object a2, object a3, object a4, object a5, object a6, object a7, object a8, object a9)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
#endif
			
			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 10);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;
				_targetArray [2] [_targetIndex[2]] = a3;
				_targetArray [3] [_targetIndex[3]] = a4;
				_targetArray [4] [_targetIndex[4]] = a5;
				_targetArray [5] [_targetIndex[5]] = a6;
				_targetArray [6] [_targetIndex[6]] = a7;
				_targetArray [7] [_targetIndex[7]] = a8;
				_targetArray [8] [_targetIndex[8]] = a9;
			} else {
				_args [0] = a1;
				_args [1] = a2;
				_args [2] = a3;
				_args [3] = a4;
				_args [4] = a5;
				_args [5] = a6;
				_args [6] = a7;
				_args [7] = a8;
				_args [8] = a9;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			_d.DynamicInvoke (outArgs);
		}

		public object InvokeFunc0 (object d)
		{
			#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
			#endif

			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 1);
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return _d.DynamicInvoke (outArgs);
		}

		public object InvokeFunc1 (object d, object a1)
		{
			#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
			#endif

			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 2);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
			} else {
				_args [0] = a1;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return _d.DynamicInvoke (outArgs);
		}

		public object InvokeFunc2 (object d, object a1, object a2)
		{
			#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
			#endif

			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 3);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;

			} else {
				_args [0] = a1;
				_args [1] = a2;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return _d.DynamicInvoke (outArgs);
		}

		public object InvokeFunc3 (object d, object a1, object a2, object a3)
		{
			#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
			#endif

			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 4);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;
				_targetArray [2] [_targetIndex[2]] = a3;

			} else {
				_args [0] = a1;
				_args [1] = a2;
				_args [2] = a3;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return _d.DynamicInvoke (outArgs);
		}

		public object InvokeFunc4 (object d, object a1, object a2, object a3, object a4)
		{
			#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
			#endif

			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 5);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;
				_targetArray [2] [_targetIndex[2]] = a3;
				_targetArray [3] [_targetIndex[3]] = a4;
			} else {
				_args [0] = a1;
				_args [1] = a2;
				_args [2] = a3;
				_args [3] = a4;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return _d.DynamicInvoke (outArgs);
		}

		public object InvokeFunc5 (object d, object a1, object a2, object a3, object a4, object a5)
		{
			#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
			#endif

			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 6);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;
				_targetArray [2] [_targetIndex[2]] = a3;
				_targetArray [3] [_targetIndex[3]] = a4;
				_targetArray [4] [_targetIndex[4]] = a5;
			} else {
				_args [0] = a1;
				_args [1] = a2;
				_args [2] = a3;
				_args [3] = a4;
				_args [4] = a5;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return _d.DynamicInvoke (outArgs);
		}

		public object InvokeFunc6 (object d, object a1, object a2, object a3, object a4, object a5, object a6)
		{
			#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
			#endif

			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 7);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;
				_targetArray [2] [_targetIndex[2]] = a3;
				_targetArray [3] [_targetIndex[3]] = a4;
				_targetArray [4] [_targetIndex[4]] = a5;
				_targetArray [5] [_targetIndex[5]] = a6;
			} else {
				_args [0] = a1;
				_args [1] = a2;
				_args [2] = a3;
				_args [3] = a4;
				_args [4] = a5;
				_args [5] = a6;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return _d.DynamicInvoke (outArgs);
		}

		public object InvokeFunc7 (object d, object a1, object a2, object a3, object a4, object a5, object a6, object a7)
		{
			#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
			#endif

			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 8);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;
				_targetArray [2] [_targetIndex[2]] = a3;
				_targetArray [3] [_targetIndex[3]] = a4;
				_targetArray [4] [_targetIndex[4]] = a5;
				_targetArray [5] [_targetIndex[5]] = a6;
				_targetArray [6] [_targetIndex[6]] = a7;
			} else {
				_args [0] = a1;
				_args [1] = a2;
				_args [2] = a3;
				_args [3] = a4;
				_args [4] = a5;
				_args [5] = a6;
				_args [6] = a7;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return _d.DynamicInvoke (outArgs);
		}

		public object InvokeFunc8 (object d, object a1, object a2, object a3, object a4, object a5, object a6, object a7, object a8)
		{
			#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
			#endif

			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 9);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;
				_targetArray [2] [_targetIndex[2]] = a3;
				_targetArray [3] [_targetIndex[3]] = a4;
				_targetArray [4] [_targetIndex[4]] = a5;
				_targetArray [5] [_targetIndex[5]] = a6;
				_targetArray [6] [_targetIndex[6]] = a7;
				_targetArray [7] [_targetIndex[7]] = a8;
			} else {
				_args [0] = a1;
				_args [1] = a2;
				_args [2] = a3;
				_args [3] = a4;
				_args [4] = a5;
				_args [5] = a6;
				_args [6] = a7;
				_args [7] = a8;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return _d.DynamicInvoke (outArgs);
		}

		public object InvokeFunc9 (object d, object a1, object a2, object a3, object a4, object a5, object a6, object a7, object a8, object a9)
		{
			#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.InvokeBinderInvoked);
			#endif

			if (((Delegate)d) != _d) {
				UpdateInvokeInfo ((Delegate)d, 10);
			}
			if (_params != null) {
				_targetArray [0] [_targetIndex[0]] = a1;
				_targetArray [1] [_targetIndex[1]] = a2;
				_targetArray [2] [_targetIndex[2]] = a3;
				_targetArray [3] [_targetIndex[3]] = a4;
				_targetArray [4] [_targetIndex[4]] = a5;
				_targetArray [5] [_targetIndex[5]] = a6;
				_targetArray [6] [_targetIndex[6]] = a7;
				_targetArray [7] [_targetIndex[7]] = a8;
				_targetArray [8] [_targetIndex[8]] = a9;
			} else {
				_args [0] = a1;
				_args [1] = a2;
				_args [2] = a3;
				_args [3] = a4;
				_args [4] = a5;
				_args [5] = a6;
				_args [6] = a7;
				_args [7] = a8;
				_args [8] = a9;
			}
			object[] outArgs;
			bool canConvert = PlayScript.Dynamic.ConvertMethodParameters(_d.Method, _args, out outArgs);
			Debug.Assert(canConvert, "Could not convert parameters");
			return _d.DynamicInvoke (outArgs);
		}

		public void UpdateInvokeInfo(Delegate d, int callArgs)
		{
			int args = callArgs - 1;
			var t = d.GetType ();

			_d = d;

			// Set up args arrays
			if (t.Namespace == "PlayScript") {
				bool isActionP = t.Name.StartsWith ("ActionP");
				bool isFuncP = t.Name.StartsWith ("FuncP");
				int argsP = 1;
				if (isActionP || isFuncP) { 
					if (t.IsGenericType) {
						argsP += t.GetGenericArguments ().Length;
					}
					if (isFuncP) {
						argsP--;
					}
					_args = new object[argsP];
					_params = new object[args - (argsP - 1)];
					_args [argsP - 1] = _params;
					_targetArray = new object[args][];
					_targetIndex = new int[args];
					for (int i = 0; i < args; i++) {
						if (i < argsP - 1) {
							_targetArray [i] = _args;
							_targetIndex [i] = i;
						} else {
							_targetArray [i] = _params;
							_targetIndex [i] = i - (argsP - 1);
						}
					}
				}
			} else {
				_args = new object[args];
				_params = null;
			}
		}
	}

}

#endif
