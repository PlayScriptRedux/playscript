package flash.events {
	
	public class ContextMenuEvent extends Event {

		public static const MENU_ITEM_SELECT : String = "menuItemSelect";
		public static const MENU_SELECT : String = "menuSelect";
		
		public function ContextMenuEvent(type:String, bubbles:Boolean = false, cancelable:Boolean = false) {
			super(type, bubbles, cancelable);
		}
	}
	
}

