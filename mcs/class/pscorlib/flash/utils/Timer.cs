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
				sLockedTimerListToAdd.Add(timer);
			}
		}

		private static void RemoveFromActiveTimerList(Timer timer) {
			lock (sLock) {
				sLockedTimerListToRemove.Add(timer);
			}
		}

		private static List<Timer> ActiveTimers() {
			lock (sLock) {
				// Update the active timer list
				sActiveTimers.AddRange(sLockedTimerListToAdd);
				sLockedTimerListToAdd.Clear();

				foreach (Timer timer in sLockedTimerListToRemove)
				{
					sActiveTimers.Remove(timer);
				}
				sLockedTimerListToRemove.Clear();
			}

			return sActiveTimers;
		}

		private static object sLock = new object();

		// List of all timers that need to be added
		private static List<Timer> sLockedTimerListToAdd = new List<Timer>();
		// List of all timers that need to be removed
		private static List<Timer> sLockedTimerListToRemove = new List<Timer>();
		// List of all active timers
		private static List<Timer> sActiveTimers = new List<Timer>();

		// Note that we currently only have active timers.
		// If there is no listeners, there is actually no reason to go through them every time.
		// (it might from a time point of view, but other than that, it iw wasted CPU cycles).
		// TODO: Improve this so the timers with no listeners have a lower CPU cost.
	}

}
