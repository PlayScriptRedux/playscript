using System;

namespace flash.events
{
	public interface IEventDispatcher
	{
 	 	
		// Registers an event listener object with an EventDispatcher object so that the listener receives notification of an event.
		void addEventListener(string type, dynamic listener, bool useCapture = false, int priority = 0, bool useWeakReference = false);
 	 	
		// Dispatches an event into the event flow.
		bool dispatchEvent(Event ev);
 	 	
		// Checks whether the EventDispatcher object has any listeners registered for a specific type of event.
		bool hasEventListener(string type);
 	 	
		// Removes a listener from the EventDispatcher object.
		void removeEventListener(string type, dynamic listener, bool useCapture = false);
 	 	
		// Checks whether an event listener is registered with this EventDispatcher object or any of its ancestors for the specified event type.
		bool willTrigger(string type);

	}
}

