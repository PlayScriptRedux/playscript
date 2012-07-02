using System;
using System.Collections.Generic;

namespace flash.events
{
	public class EventDispatcher : IEventDispatcher
	{
		private IEventDispatcher _evTarget;
		private Dictionary<string, List<Tuple<dynamic,Action<Event>>>> _events;

		public EventDispatcher() {
		}

		public EventDispatcher (IEventDispatcher target)
		{
			_evTarget = target;
		}

		#region IEventDispatcher implementation
		public virtual void addEventListener (string type, dynamic listener, bool useCapture = false, int priority = 0, bool useWeakReference = false)
		{
			if (_evTarget != null) {
				_evTarget.addEventListener (type, listener, useCapture, priority, useWeakReference);
			} else {

				if (_events == null) {
					_events = new Dictionary<string, List<Tuple<dynamic,Action<Event>>>> ();
				}

				List<Tuple<dynamic,Action<Event>>> evList = null;
				if (!_events.TryGetValue (type, out evList)) {
					evList = new List<Tuple<dynamic,Action<Event>>> ();
					_events [type] = evList;
				}

				if (listener is Action<Event>) {
					evList.Add (new Tuple<dynamic, Action<Event>>(listener, (Action<Event>)listener));
				} else {
					evList.Add (new Tuple<dynamic, Action<Event>>(listener, (Action<Event>) delegate (Event ev) { listener(ev); }));
				}
			}
		}

		public virtual bool dispatchEvent (Event ev)
		{
			if (_evTarget != null) {
				return dispatchEvent(ev);
			} else {
				bool dispatched = false;
				if (_events != null) {
					List<Tuple<dynamic,Action<Event>>> evList = null;
					if (_events.TryGetValue (ev.type, out evList)) {
						var l = evList.Count;
						for (var i = 0; i < l; i++) {
							var t = evList [i];
							t.Item2 (ev);
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

		public virtual void removeEventListener (string type, dynamic listener, bool useCapture)
		{
			if (_evTarget != null) {
				removeEventListener(type, listener, useCapture);
			} else {
				if (_events == null) {
					return;
				}

				List<Tuple<dynamic,Action<Event>>> evList = null;
				if (_events.TryGetValue (type, out evList)) {
					int idx = evList.FindIndex( (t) => t.Item1 == listener);
					if (idx >= 0) {
						evList.RemoveAt (idx);
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

