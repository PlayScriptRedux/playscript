using System;
using System.Collections.Generic;

namespace flash.events
{
	public class EventDispatcher : IEventDispatcher
	{
		private IEventDispatcher _evTarget;
		private Dictionary<string, List<Action<Event>>> _events;

		public EventDispatcher() {
		}

		public EventDispatcher (IEventDispatcher target)
		{
			_evTarget = target;
		}

		#region IEventDispatcher implementation
		public virtual void addEventListener (string type, Delegate listener, bool useCapture = false, int priority = 0, bool useWeakReference = false)
		{
			var listAct = (Action<Event>)listener;

			if (_evTarget != null) {
				_evTarget.addEventListener (type, listener, useCapture, priority, useWeakReference);
			} else {

				if (_events == null) {
					_events = new Dictionary<string, List<Action<Event>>> ();
				}

				List<Action<Event>> evList = null;
				if (!_events.TryGetValue (type, out evList)) {
					evList = new List<Action<Event>> ();
					_events [type] = evList;
				}

				evList.Add (listAct);
			}
		}

		public virtual bool dispatchEvent (Event ev)
		{
			if (_evTarget != null) {
				return dispatchEvent(ev);
			} else {
				bool dispatched = false;
				if (_events != null) {
					List<Action<Event>> evList = null;
					if (_events.TryGetValue (ev.type, out evList)) {
						var l = evList.Count;
						for (var i = 0; i < l; i++) {
							var f = evList [i];
							f (ev);
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

		public virtual void removeEventListener (string type, Delegate listener, bool useCapture)
		{
			if (_evTarget != null) {
				removeEventListener(type, listener, useCapture);
			} else {
				if (_events == null) {
					return;
				}

				List<Action<Event>> evList = null;
				if (_events.TryGetValue (type, out evList)) {
					var listAct = (Action<Event>)listener;
					int idx = evList.IndexOf(listAct);
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

