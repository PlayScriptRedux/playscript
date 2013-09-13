//
// CSharpGetIndexBinder.cs
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
using System.Collections;
using System.Collections.Generic;
using PlayScript;

namespace PlayScript.RuntimeBinder
{
	class PSIsEventBinder : CallSiteBinder
	{
		private static Dictionary<Type, object> delegates = new Dictionary<Type, object>();

//		private readonly string mName;

		public PSIsEventBinder (CSharpBinderFlags flags, string name, Type context)
		{
//			mName = name;
		}

		private static bool IsEvent(CallSite site, object o)
		{
#if BINDERS_RUNTIME_STATS
			Stats.Increment(StatsCounter.IsEventBinderInvoked);
#endif
			// $$TODO
			return false;
		}

		static PSIsEventBinder ()
		{
			delegates.Add (typeof(Func<CallSite, object, bool>),    (Func<CallSite, object, bool>)IsEvent);
		}
		
		public override object Bind (Type delegateType)
		{
			object target;
			if (delegates.TryGetValue (delegateType, out target)) {
				return target;
			}
			throw new Exception("Unable to bind get index for target " + delegateType.Name);
		}

	}
}
#endif

