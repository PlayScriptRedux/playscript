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

namespace flash.events
{
	public interface IEventDispatcher
	{
 	 	
		// Registers an event listener object with an EventDispatcher object so that the listener receives notification of an event.
		void addEventListener(string type, Delegate listener, bool useCapture = false, int priority = 0, bool useWeakReference = false);
 	 	
		// Dispatches an event into the event flow.
		bool dispatchEvent(Event ev);
 	 	
		// Checks whether the EventDispatcher object has any listeners registered for a specific type of event.
		bool hasEventListener(string type);
 	 	
		// Removes a listener from the EventDispatcher object.
		void removeEventListener(string type, Delegate listener, bool useCapture = false);
 	 	
		// Checks whether an event listener is registered with this EventDispatcher object or any of its ancestors for the specified event type.
		bool willTrigger(string type);

	}
}

