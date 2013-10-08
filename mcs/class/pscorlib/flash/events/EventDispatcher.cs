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
using System.Reflection;

namespace flash.events
{
	public class EventDispatcher : _root.Object, IEventDispatcher
	{
		private class EventListener
		{
			public string		type;
			public bool			useCapture;
			public int			priority;
			public bool			useWeakReference;
			public PlayScript.InvokerBase	invoker;

			// TODO: We probably can remove this at some point...
			public Delegate		callback;
		}

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

		public void addEventListener<P1>(string type, Action<P1> listener, bool useCapture = false, int priority = 0, bool useWeakReference = false)
		{
			if (_evRedirect != null) {
				_evRedirect.addEventListener (type, listener, useCapture, priority, useWeakReference);
			} else {
				EventListener el = addEventListener(type, useCapture, priority, useWeakReference);
				el.callback = listener;
				el.invoker = GetInvoker<P1>(listener);
				el.invoker.SetArguments(GetInitialParameters(listener.Method));
			}

			// add event to global dispatcher
			var globalDispatcher = getGlobalEventDispatcher(type);
			if (globalDispatcher != null) {
				globalDispatcher.addEventListener(type, listener, useCapture, priority, useWeakReference);
			}
		}

		public void addEventListener<P1, P2>(string type, Action<P1, P2> listener, bool useCapture = false, int priority = 0, bool useWeakReference = false)
		{
			if (_evRedirect != null) {
				_evRedirect.addEventListener (type, listener, useCapture, priority, useWeakReference);
			} else {
				EventListener el = addEventListener(type, useCapture, priority, useWeakReference);
				el.callback = listener;
				el.invoker = GetInvoker<P1, P2>(listener);
				el.invoker.SetArguments(GetInitialParameters(listener.Method));
			}

			// add event to global dispatcher
			var globalDispatcher = getGlobalEventDispatcher(type);
			if (globalDispatcher != null) {
				globalDispatcher.addEventListener(type, listener, useCapture, priority, useWeakReference);
			}
		}

		#region IEventDispatcher implementation
		public virtual void addEventListener (string type, Delegate listener, bool useCapture = false, int priority = 0, bool useWeakReference = false)
		{
			if (_evRedirect != null) {
				_evRedirect.addEventListener (type, listener, useCapture, priority, useWeakReference);
			} else {
				EventListener el = addEventListener(type, useCapture, priority, useWeakReference);
				el.callback = listener;
				el.invoker = GetInvoker(listener);
				el.invoker.SetArguments(GetInitialParameters(listener.Method));
			}

            // add event to global dispatcher
            var globalDispatcher = getGlobalEventDispatcher(type);
            if (globalDispatcher != null) {
                globalDispatcher.addEventListener(type, listener, useCapture, priority, useWeakReference);
            }
		}

		public virtual bool dispatchEvent (Event ev)
		{
			ev._target = _evTarget;
			// set current target for event
			ev._currentTarget = _evTarget;

			if (_evRedirect != null) {
				return _evRedirect.dispatchEvent(ev);
			} else {
				bool dispatched = false;
                var evList = getListenersForEventType(ev.type);
                if (evList != null) {
                    // we store off the count here in case the callback adds a listener
                    var l = evList.Count;
                    for (var i = 0; i < l; i++) {
                        // Invoke the method on the listener
						EventListener listener = evList[i];
						try
						{
							var span = Telemetry.Session.BeginSpan();
							listener.invoker.InvokeOverrideA1(ev);
							Telemetry.Session.EndSpanValue(sNameAsEvent, span, ev.type);
						}
						catch (Exception e)
						{
							_root.trace_fn.trace (e.Message);
							// This catch is where exceptions usually trigger the Xamarin debugger IDE to halt the program and display the
							// exception. But this is not the code that originally threw the exception, so what you need to now is to find a 
							// spot during execution where you can add the exception(s) (sometimes adding all of them is a good idea) to stop
							// on in the IDE "Run/Exceptions..." menu item so that the debugger will break right where the exception is
							// first thrown.  
							var inner = e.InnerException;
							if (inner != null) 
							{
								while ((e = inner.InnerException) != null) 
								{
									inner = e;
								}

								throw inner;
							}
							else
							{
								throw e;
							}
						}
						if (ev._stopImmediateProp) {
							break;
						}
                    }
					dispatched = (l != 0);
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

		private static object[] GetInitialParameters(MethodInfo methodInfo)
		{
			ParameterInfo[] parameters = methodInfo.GetParameters();

			object[] initializedParameters = new object[parameters.Length];

			// We dispatch only with one parameter, so if there is more than one parameter,
			// it should be either a default parameter or variadic

			// There are a couple of exceptions though due to AS behavior:
			//	- First parameter might not inherit from Event, it can be object for some methods
			//	- Some methods might be variadic and use object[] as parameter
			//	- Some methods might not even have a parameter, the event itself will simply be discarded

			// Most common case first
			if (parameters.Length == 1) {
				// We do not re-use that array as several events could be dispatched within each others and we want to make sure
				// The event parameter is always set properly
				return initializedParameters;
			} else if (parameters.Length == 0) {
				return null;
			}

			for (int i = 1 ; i < parameters.Length ; ++i) {
				ParameterInfo parameter = parameters[i];
				if ((parameter.Attributes & ParameterAttributes.HasDefault) != 0) {
					initializedParameters[i] = parameter.DefaultValue;
				} else {
					// If that's not default, we expect this to be variadic
					var paramArrayAttribute = parameter.GetCustomAttributes(typeof(ParamArrayAttribute), true);
					if ((paramArrayAttribute != null) && (paramArrayAttribute.Length != 0)) {
						// We can keep this null, we will have to see if it creates some side-effects
					} else {
						throw new ArgumentException("Method has more than one non-optional parameter.");
					}
				}
			}

			return initializedParameters;
		}

		private static bool IsVariadicMethod(MethodInfo methodInfo)
		{
			ParameterInfo[] parameters = methodInfo.GetParameters();
			if (parameters.Length != 1) {
				return false;
			}
			var paramArrayAttribute = parameters[0].GetCustomAttributes(typeof(ParamArrayAttribute), true);
			if ((paramArrayAttribute != null) && (paramArrayAttribute.Length != 0)) {
				return true;
			} else {
				return false;
			}
		}

		private static bool IsEventMethod(MethodInfo methodInfo)
		{
			if (methodInfo.ReturnType != typeof(void)) {
				return false;	// We don't expect return value
			}
			ParameterInfo[] parameters = methodInfo.GetParameters();
			if (parameters.Length != 1) {
				return false;
			}
			if (parameters[0].ParameterType == typeof(Event)) {
				return true;
			}

/*			Due to contravariance constraints, we don't support type inheriting from typeof(Event)
			} else if (parameters[0].ParameterType.IsSubclassOf(typeof(Event))) {
				return true;
			}
*/
			return false;
		}

		private static PlayScript.InvokerBase GetInvoker(Delegate listener)
		{
			if (IsEventMethod(listener.Method)) {
				// Create a specialization if the method has only one Event parameter (fast path)
				// Unfortunately covariant types are not accepted for delegate parameters,
				// as this would have taken care of 90% of the events invocation
				return new PlayScript.InvokerA<Event>((Action<Event>)listener);
			} else if (IsVariadicMethod(listener.Method)) {
				// Create a spcecialization if the method is variadic only (slow path)
				return new PlayScript.DynamicInvokerVariadic(listener);
			} else {
				// Otherwise the generic case (slow path)
				return new PlayScript.DynamicInvoker(listener);
			}
		}

		private static PlayScript.InvokerBase GetInvoker<P1>(Action<P1> listener)
		{
			return new PlayScript.InvokerA<P1>(listener);
		}

		private static PlayScript.InvokerBase GetInvoker<P1, P2>(Action<P1, P2> listener)
		{
			return new PlayScript.InvokerA<P1, P2>(listener);
		}

		private EventListener addEventListener(string type, bool useCapture, int priority, bool useWeakReference)
		{
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
			el.useCapture = useCapture;
			el.priority   = priority;
			el.useWeakReference = useWeakReference;

			// insert listener in priority order
			int  i;
			for (i=0; i < evList.Count; i++) {
				if (priority > evList[i].priority) {
					break;
				}
			}

			evList.Insert(i, el);
			return el;
		}

		private static readonly Amf.Amf3String sNameAsEvent = new Amf.Amf3String(".as.event");
	}
}

