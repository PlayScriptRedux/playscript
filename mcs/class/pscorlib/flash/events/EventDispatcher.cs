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
            public Delegate      callback;
			public bool    		 useCapture;
			public int           priority;
			public bool          useWeakReference;
		};


		private IEventDispatcher _evRedirect; // this is used for redirecting all event handling, not sure if this is needed
		private IEventDispatcher _evTarget;
		private Dictionary<string, List<EventListener>> _events;

		public EventDispatcher() {
			_evTarget = this;
		}

		public EventDispatcher (IEventDispatcher target)
		{
			_evTarget = (target != null) ? target : this;
		}

		#region IEventDispatcher implementation
		public virtual void addEventListener (string type, Delegate listener, bool useCapture = false, int priority = 0, bool useWeakReference = false)
		{
			if (_evRedirect != null) {
				_evRedirect.addEventListener (type, listener, useCapture, priority, useWeakReference);
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
			}

            // add event to global dispatcher
            var globalDispatcher = getGlobalEventDispatcher(type);
            if (globalDispatcher != null) {
                globalDispatcher.addEventListener(type, listener, useCapture, priority, useWeakReference);
            }

		}

		public virtual bool dispatchEvent (Event ev)
		{
			if (_evRedirect != null) {
				return _evRedirect.dispatchEvent(ev);
			} else {
				bool dispatched = false;
                var evList = getListenersForEventType(ev.type);
                if (evList != null) {
                    // we store off the count here in case the callback adds a listener
                    var l = evList.Count;
                    for (var i = 0; i < l; i++) {
                        // cast callback to dynamic
                        Delegate callback = evList [i].callback;
                        // we perform a dynamic invoke here because the parameter types dont always match exactly
                        try {
							// set current target for event
							ev._currentTarget = ev._target = _evTarget;
                            callback.DynamicInvoke(ev);
							ev._currentTarget = ev._target = null;
                        } 
                        catch (Exception e)
                        {
                            // if you get an exception here while debugging then make sure that the debugger is setup to catch all exceptions
                            // this is in the Run/Exceptions... menu in MonoDevelop or Xamarin studio
							Console.Error.WriteLine(e.ToString());
                        }
                        dispatched = true;
                    }
                }
				return dispatched;
			}
		}

		public virtual bool hasEventListener (string type)
		{
			if (_evRedirect != null) {
				return _evRedirect.hasEventListener(type);
			} else {
                var evList = getListenersForEventType(type);
                if (evList != null) {
                    // return true if there are event listeners for this event
					return (evList.Count > 0);
				}
				return false;
			}
		}

		public virtual void removeEventListener (string type, Delegate listener, bool useCapture = false)
		{
			if (_evRedirect != null) {
				_evRedirect.removeEventListener(type, listener, useCapture);
			} else {
                var evList = getListenersForEventType(type);
                if (evList != null) {
                    // create a new list here and replace the old one
                    // this handles the case of removing a listener while inside dispatchEvent()
                    List<EventListener> newList = null;
                    foreach (EventListener el in evList) {
                        if (!el.callback.Equals(listener)) {
                            if (newList == null) {
                                newList = new List<EventListener>();
                                newList.Capacity = (evList.Count - 1);
                            }
                            newList.Add(el);
						}
					}

                    if (newList != null) {
                        // replace list
                        _events[type] = newList;
                    } else {
                        // empty list
                        _events.Remove(type);
                    }
				}
			}

            // remove from global event dispatcher
            var globalDispatcher = getGlobalEventDispatcher(type);
            if (globalDispatcher != null) {
                globalDispatcher.removeEventListener(type, listener, useCapture);
            }
		}

		public virtual bool willTrigger (string type)
		{
			if (_evRedirect != null) {
				return _evRedirect.hasEventListener (type);
			} else {
				return hasEventListener (type);
			}
		}
		#endregion

        // this method is to be overriden in a derived event dispatcher that requires global event tracking (such as display objects)
        protected virtual EventDispatcher getGlobalEventDispatcher(string type)
        {
            return null;
        }

        private List<EventListener> getListenersForEventType(string type)
        {
            List<EventListener> list = null;
            if (_events != null) {
                _events.TryGetValue(type, out list);
            }
            return list;
        }

	}
}

