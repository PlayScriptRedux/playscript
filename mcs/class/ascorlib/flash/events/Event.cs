using System;

namespace flash.events
{
	public class Event : System.EventArgs
	{
		// [static] The ACTIVATE constant defines the value of the type property of an activate event object.
		public const string ACTIVATE = "activate";
		// [static] The Event.ADDED constant defines the value of the type property of an added event object.
		public const string ADDED = "added";
		// [static] The Event.ADDED_TO_STAGE constant defines the value of the type property of an addedToStage event object.
		public const string ADDED_TO_STAGE = "addedToStage";
		// [static] The Event.CANCEL constant defines the value of the type property of a cancel event object.
		public const string CANCEL = "cancel";
		// [static] The Event.CHANGE constant defines the value of the type property of a change event object.
		public const string CHANGE = "change";
		// [static] The Event.CLEAR constant defines the value of the type property of a clear event object.
		public const string CLEAR = "clear";
		// [static] The Event.CLOSE constant defines the value of the type property of a close event object.
		public const string CLOSE = "close";
		// [static] The Event.CLOSING constant defines the value of the type property of a closing event object.
		public const string CLOSING = "closing";
		// [static] The Event.COMPLETE constant defines the value of the type property of a complete event object.
		public const string COMPLETE = "complete";
		// [static] The Event.CONNECT constant defines the value of the type property of a connect event object.
		public const string CONNECT = "connect";
		// [static] The Event.CONTEXT3D_CREATE constant defines the value of the type property of a context3Dcreate event object.
		public const string CONTEXT3D_CREATE = "context3DCreate";
		// [static] Defines the value of the type property of a copy event object.
		public const string COPY = "copy";
		// [static] Defines the value of the type property of a cut event object.
		public const string CUT = "cut";
		// [static] The Event.DEACTIVATE constant defines the value of the type property of a deactivate event object.
		public const string DEACTIVATE = "deactivate";
		// [static] The Event.DISPLAYING constant defines the value of the type property of a displaying event object.
		public const string DISPLAYING = "displaying";
		// [static] The Event.ENTER_FRAME constant defines the value of the type property of an enterFrame event object.
		public const string ENTER_FRAME = "enterFrame";
		// [static] The Event.EXIT_FRAME constant defines the value of the type property of an exitFrame event object.
		public const string EXIT_FRAME = "exitFrame";
		// [static] The Event.EXITING constant defines the value of the type property of an exiting event object.
		public const string EXITING = "exiting";
		// [static] The Event.FRAME_CONSTRUCTED constant defines the value of the type property of an frameConstructed event object.
		public const string FRAME_CONSTRUCTED = "frameConstructed";
		// [static] The Event.FRAME_LABEL constant defines the value of the type property of an frameLabel event object.
		public const string FRAME_LABEL = "frameLabel";
		// [static] The Event.FULL_SCREEN constant defines the value of the type property of a fullScreen event object.
		public const string FULLSCREEN = "fullScreen";
		// [static] The Event.HTML_BOUNDS_CHANGE constant defines the value of the type property of an htmlBoundsChange event object.
		public const string HTML_BOUNDS_CHANGE = "htmlBoundsChange";
		// [static] The Event.HTML_DOM_INITIALIZE constant defines the value of the type property of an htmlDOMInitialize event object.
		public const string HTML_DOM_INITIALIZE = "htmlDOMInitialize";
		// [static] The Event.HTML_RENDER constant defines the value of the type property of an htmlRender event object.
		public const string HTML_RENDER = "htmlRender";
		// [static] The Event.ID3 constant defines the value of the type property of an id3 event object.
		public const string ID3 = "id3";
		// [static] The Event.INIT constant defines the value of the type property of an init event object.
		public const string INIT = "init";
		// [static] The Event.LOCATION_CHANGE constant defines the value of the type property of a locationChange event object.
		public const string LOCATION_CHANGE = "locationChange";
		// [static] The Event.MOUSE_LEAVE constant defines the value of the type property of a mouseLeave event object.
		public const string MOUSE_LEAVE = "mouseLeave";
		// [static] The Event.NETWORK_CHANGE constant defines the value of the type property of a networkChange event object.
		public const string NETWORK_CHANGE = "networkChange";
		// [static] The Event.OPEN constant defines the value of the type property of an open event object.
		public const string OPEN = "open";
		// [static] The Event.PASTE constant defines the value of the type property of a paste event object.
		public const string PASTE = "paste";
		// [static] The Event.PREPARING constant defines the value of the type property of a preparing event object.
		public const string PREPARING = "preparing";
		// [static] The Event.REMOVED constant defines the value of the type property of a removed event object.
		public const string REMOVED = "removed";
		// [static] The Event.REMOVED_FROM_STAGE constant defines the value of the type property of a removedFromStage event object.
		public const string REMOVED_FROM_STAGE = "removedFromStage";
		// [static] The Event.RENDER constant defines the value of the type property of a render event object.
		public const string RENDER = "render";
		// [static] The Event.RESIZE constant defines the value of the type property of a resize event object.
		public const string RESIZE = "resize";
		// [static] The Event.SCROLL constant defines the value of the type property of a scroll event object.
		public const string SCROLL = "scroll";
		// [static] The Event.SELECT constant defines the value of the type property of a select event object.
		public const string SELECT = "select";
		// [static] The Event.SELECT_ALL constant defines the value of the type property of a selectAll event object.
		public const string SELECT_ALL = "selectAll";
		// [static] The Event.SOUND_COMPLETE constant defines the value of the type property of a soundComplete event object.
		public const string SOUND_COMPLETE = "soundComplete";
		// [static] The Event.STANDARD_ERROR_CLOSE constant defines the value of the type property of a standardErrorClose event object.
		public const string STANDARD_ERROR_CLOSE = "standardErrorClose";
		// [static] The Event.STANDARD_INPUT_CLOSE constant defines the value of the type property of a standardInputClose event object.
		public const string STANDARD_INPUT_CLOSE = "standardInputClose";
		// [static] The Event.STANDARD_OUTPUT_CLOSE constant defines the value of the type property of a standardOutputClose event object.
		public const string STANDARD_OUTPUT_CLOSE = "standardOutputClose";
		// [static] The Event.SUSPEND constant defines the value of the type property of an suspend event object.
		public const string SUSPEND = "suspend";
		// [static] The Event.TAB_CHILDREN_CHANGE constant defines the value of the type property of a tabChildrenChange event object.
		public const string TAB_CHILDREN_CHANGE = "tabChildrenChange";
		// [static] The Event.TAB_ENABLED_CHANGE constant defines the value of the type property of a tabEnabledChange event object.
		public const string TAB_ENABLED_CHANGE = "tabEnabledChange";
		// [static] The Event.TAB_INDEX_CHANGE constant defines the value of the type property of a tabIndexChange event object.
		public const string TAB_INDEX_CHANGE = "tabIndexChange";
		// [static] The Event.TEXT_INTERACTION_MODE_CHANGE constant defines the value of the type property of a interaction mode event object.
		public const string TEXT_INTERACTION_MODE_CHANGE = "textInteractionModeChange";
		// [static] The Event.TEXTURE_READY constant defines the value of the type property of a textureReady event object.
		public const string TEXTURE_READY = "textureReady";
		// [static] The Event.UNLOAD constant defines the value of the type property of an unload event object.
		public const string UNLOAD = "unload";
		// [static] The Event.USER_IDLE constant defines the value of the type property of a userIdle event object.
		public const string USER_IDLE = "userIdle";
		// [static] The Event.USER_PRESENT constant defines the value of the type property of a userPresent event object.
		public const string USER_PRESENT = "userPresent";

		private string _type;
		private bool _bubbles;
		private bool _cancelable;
		internal object _currentTarget;
		internal object _target;
		internal uint _eventPhase;

		public Event (string type, bool bubbles = false, bool cancelable = false)
		{
			_type = type;
			_bubbles = bubbles;
			_cancelable = cancelable;
		}

		// [read-only] Indicates whether an event is a bubbling event.
		public bool bubbles { get { return _bubbles; } }

		// [read-only] Indicates whether the behavior associated with the event can be prevented.
		public bool cancelable  { get { return _cancelable; } }

		// [read-only] The object that is actively processing the Event object with an event listener.
 	 	public object currentTarget  { get { return _currentTarget; } }

		//[read-only] The current phase in the event flow.
		public uint eventPhase { get { return _eventPhase; } } 

		// [read-only] The event target.
 	 	public object target { get { return _target; } } 

		// [read-only] The type of event.
 	 	public string type { get { return _type; } }
	}
}

