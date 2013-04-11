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
using System.Collections.Generic;

namespace flash.events
{
	public class EventDispatcher : IEventDispatcher
	{
		private class EventListener
		{
			public string        type;
			public dynamic       callback;
			public bool    		 useCapture;
			public int           priority;
			public bool          useWeakReference;
		};


		private IEventDispatcher _evTarget;
		private Dictionary<string, List<EventListener>> _events;

		public EventDispatcher() {
		}

		public EventDispatcher (IEventDispatcher target)
		{
			_evTarget = target;
		}

		#region IEventDispatcher implementation
		public virtual void addEventListener (string type, Delegate listener, bool useCapture = false, int priority = 0, bool useWeakReference = false)
		{
			if (_evTarget != null) {
				_evTarget.addEventListener (type, listener, useCapture, priority, useWeakReference);
			} else {

				if (_events == null) {
					_events = new Dictionary<string, List<EventListener>> ();
				}

				List<EventListener> evList = null;
				if (!_events.TryGetValue (type, out evList)) {
					evList = new List<EventListener> ();
					_events [type] = evList;
				}

				// create event listener
				var el = new EventListener();
				el.type     = type;
				el.callback = listener;
				el.useCapture = useCapture;
				el.priority   = priority;
				el.useWeakReference =useWeakReference;

				// insert listener in priority order
				int  i;
				for (i=0; i < evList.Count; i++) {
					if (priority > evList[i].priority) {
						break;
					}
				}

				evList.Insert(i, el);

				// evList.Add (el);
			}
		}

		public virtual bool dispatchEvent (Event ev)
		{
			if (_evTarget != null) {
				return dispatchEvent(ev);
			} else {
				bool dispatched = false;
				if (_events != null) {
					List<EventListener> evList = null;
					if (_events.TryGetValue (ev.type, out evList)) {
						var l = evList.Count;
						for (var i = 0; i < l; i++) {
							var f = evList [i];
							f.callback(ev);
							dispatched = true;
						}
					}
				}
				return dispatched;
			}
		}

		public virtual bool hasEventListener (string type)
		{
			if (_evTarget != null) {
				return hasEventListener(type);
			} else {
				if (_events != null) {
					return _events.ContainsKey (type);
				}
				return false;
			}
		}

		public virtual void removeEventListener (string type, Delegate listener, bool useCapture = false)
		{
			if (_evTarget != null) {
                _evTarget.removeEventListener(type, listener, useCapture);
			} else {
				if (_events == null) {
					return;
				}

				List<EventListener> evList = null;
				if (_events.TryGetValue (type, out evList)) {
					dynamic listAct = listener;

					for (int i=0; i < evList.Count; i++) {
						if ((object)evList[i].callback == (object)listAct) {
							evList.RemoveAt (i);
							break;
						}
					}
				}
			}
		}

		public virtual bool willTrigger (string type)
		{
			if (_evTarget != null) {
				return _evTarget.hasEventListener (type);
			} else {
				return hasEventListener (type);
			}
		}

		#endregion
	}
}

