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
using System.Collections.Generic;

namespace flash.utils {

	partial class Timer {

		private static void AddToActiveTimerList(Timer timer) {
			lock (sLock) {
				sLockedActiveTimerList.Add(timer);
			}
		}

		private static void RemoveFromActiveTimerList(Timer timer) {
			lock (sLock) {
				sLockedActiveTimerList.Remove(timer);
			}
		}

		private static Timer[] CloneActiveTimers() {
			lock (sLock) {
				return sLockedActiveTimerList.ToArray();
			}
		}

		private static object sLock = new object();

		// list of all active timers that need per-frame processing
		// It seems that only active timer list needs to be locked,
		// as they can be created from another thread to inject functions in the main thread (like http load).
		private static List<Timer> sLockedActiveTimerList = new List<Timer>();


	}

}
