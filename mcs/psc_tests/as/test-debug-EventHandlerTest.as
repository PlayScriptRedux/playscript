// Compiler options: -r:./as/Assert.dll
package
{
	import flash.events.ErrorEvent;
	import flash.events.Event;
	import flash.events.EventDispatcher;
	import flash.events.IOErrorEvent;
	import flash.events.SecurityErrorEvent;

    public class EventHandlerTest
    {
		private static var Completed:Boolean = false;
		private static var ErrorOccurred:Boolean = false;

		public static function Main():int
		{
			var dispatcher:EventDispatcher = new EventDispatcher();
			handle(
				Event.COMPLETE, onComplete,
				ErrorEvent.ERROR, onError,
				IOErrorEvent.IO_ERROR, onError,
				SecurityErrorEvent.SECURITY_ERROR, onError
			).once().source = dispatcher;

			// 2 events dispatched, only the first should be handled.
			dispatcher.dispatchEvent(new Event(Event.COMPLETE));
			dispatcher.dispatchEvent(new IOErrorEvent(IOErrorEvent.IO_ERROR));

			if (!Completed)
				return 1;

			if (ErrorOccurred)
				return 2;

			return 0;
		}

		private static function handle( eventType1:String, 
										eventHandler1:Function, 
										eventType2:String, 
										eventHandler2:Function, 
										eventType3:String, 
										eventHandler3:Function, 
										eventType4:String, 
										eventHandler4:Function) : EventHandler
		{
			var result:EventHandler = new EventHandler();
			result.addHandler( eventType1, eventHandler1 );
			result.addHandler( eventType2, eventHandler2 );
			result.addHandler( eventType3, eventHandler3 );
			result.addHandler( eventType4, eventHandler4 );
			return result;
		}

		private static function onComplete(event:Event):void
		{
			Completed = true;
		}

		private static function onError(event:Event):void
		{
			ErrorOccurred = true;
		}
	}
}

public final class EventHandler
{
	private var _source:Object;
	private var _enabled:Boolean = true;
	private var _useWeakReferences:Boolean = true;
	private var _priority:int = 0;
	private var _once:Boolean;
	
	private var handlerMap:Object;
	
	public function EventHandler()
	{
		this.handlerMap = {};
	}
	
	public function destroy() : void
	{
		this.source = null;
		if (handlerMap)
		{
			// Workaround PlayScript issue #262
			//for (var eventType:String in handlerMap)
			var eventTypes:Array = Utils.getKeys( handlerMap );
			for each (var eventType:String in eventTypes)
			{
				var handlers:Array = handlerMap[ eventType ] as Array;
				handlers.length = 0;
				delete handlerMap[ eventType ];
			}
			this.handlerMap = null;
		}
	}
	
	/** get/set the priorities for this batch of listeners */
	public function get priority():int
	{
		return _priority;
	}

	public function set priority(value:int):void
	{
		var wasEnabled:Boolean = this._enabled;
		this.enabled = false;
		_priority = value;
		this.enabled = wasEnabled;
	}

	/** get/set the weak reference settings for this batch of listeners */
	public function get useWeakReferences():Boolean
	{
		return _useWeakReferences;
	}

	public function set useWeakReferences(value:Boolean):void
	{
		var wasEnabled:Boolean = this._enabled;
		this.enabled = false;
		_useWeakReferences = value;
		this.enabled = wasEnabled;
	}
	
	/** get/set the event source for this batch of handlers */
	public function get source():Object
	{
		return _source;
	}

	public function set source(value:Object):void
	{
		var wasEnabled:Boolean = this._enabled;
		this.enabled = false;
		_source = value;
		this.enabled = wasEnabled;
	}
	
	/** enable/disable the listeners */
	public function get enabled():Boolean
	{
		return _enabled;
	}
	
	public function set enabled(value:Boolean):void
	{
		if (value != _enabled)
		{
			_enabled = value;
			if (_source)
			{
				var eventTypes:Array = Utils.getKeys( handlerMap );
				if (value) 
				{
					this.addHandlers( eventTypes );
				}
				else
				{
					this.removeHandlers( eventTypes );
				}
			}
		}
	}
	
	/**
	 * Establish that the handler is to be bound once and then
	 * the EventHandler object is destroyed.  
	 */
	public function once() : EventHandler
	{
		if (!_once)
		{
			_once = true;
			_useWeakReferences = false;
		}
		return this;
	}
	
	/**
	 * Add an event listener for a given event type to the EventHandler, and potentially
	 * to the source reference if the EventHandler is enabled
	 * 
	 * @param eventType String
	 * @param handler Function
	 * @return EventHandler for cascading calls
	 */		
	public function addHandler( eventType:String, handler:Function ) : EventHandler
	{
		var handlers:Array = this.handlerMap[ eventType ] as Array;
		if (!handlers)
		{
			handlers = [];
			this.handlerMap[ eventType ] = handlers;
		}
		handlers.push( handler );
		if (_source && _enabled)
		{
			if (!_once)
			{
				_source.addEventListener( eventType, handler, false, _priority, _useWeakReferences );
			}
			else if (handlers.length == 1)
			{
				_source.addEventListener( eventType, oneTimeHandler, false, _priority, _useWeakReferences );
			}
		}
			
		return this;
	}
	
	public function hasHandler( eventType:String, handler:Function = null ) : Boolean
	{
		var result:Boolean;
		for (var activeType:String in this.handlerMap)
		{
			var activeHandlers:Array = this.handlerMap[activeType];
			if (activeType == eventType && 
				(handler == null || activeHandlers.indexOf( handler ) >= 0))
			{
				result = true;
				break;
			}
		}
		
		return result;
	}
	
	private function addHandlers( eventTypes:Array ) : void
	{
		for each (var addEventType:String in eventTypes)
		{
			if (_once)
			{
				_source.addEventListener(
					addEventType, oneTimeHandler, false,
					_priority, _useWeakReferences
				);
			}
			else
			{
				var addHandlers:Array = handlerMap[ addEventType ] as Array;
				for each (var addHandler:Function in addHandlers)
				{
					_source.addEventListener(
						addEventType, addHandler, false,
						_priority, _useWeakReferences
					);
				}	
			}
		}
	}
	
	/**
	 * Remove an event listener from the EventHandler and potentially the
	 * source reference if the EventHandler is enabled.
	 * 
	 * @param eventType String
	 * @param handler Function
	 * @return EventHandler for cascading calls
	 */		
	public function removeHandler( eventType:String, handler:Function ) : EventHandler
	{
		var handlers:Array = this.handlerMap[ eventType ] as Array;
		if (handlers && handlers.indexOf( handler ) >= 0)
		{
			handlers.splice( handlers.indexOf( handler ), 1 );
			if (_source && _enabled)
			{
				if (!_once)
				{
					_source.removeEventListener( eventType, handler, false );
				}
				else if (handlers.length == 0)
				{
					_source.removeEventListener( eventType, oneTimeHandler, false );
				}
			}
		}
		return this;
	}

	private function removeHandlers( eventTypes:Array ) : void
	{
		for each (var removeEventType:String in eventTypes)
		{
			if (_once)
			{
				_source.removeEventListener(
					removeEventType, oneTimeHandler, false
				);
			}
			else
			{
				var removeHandlers:Array = handlerMap[ removeEventType ] as Array;
				for each (var removeHandler:Function in removeHandlers)
				{
					_source.removeEventListener(
						removeEventType, removeHandler, false
					);
				}
			}
		}
	}
	
	protected function oneTimeHandler( e:Object ) : void
	{
		var handlers:Array = handlerMap[ e.type ] as Array;
		for each (var handler:Function in handlers)
		{
			handler(e);
		}
		destroy();
	}
}

public final class Utils
{
	static public function getKeys( value:Object ) : Array
	{
		var keys:Array = [];
		for (var key:String in value)
		{
			keys.push(key);
		}
		return keys;
	}
}
