// Compiler options: -psstrict-
package {
	import flash.display.*;
	import flash.events.IEventDispatcher;

	public class Foo {
		public static function Main():int {

			// http://help.adobe.com/en_US/FlashPlatform/reference/actionscript/3/operators.html#is

			var mySprite:Sprite = new Sprite();
			trace(mySprite is Sprite);           // true
			trace(mySprite is DisplayObject);    // true
			trace(mySprite is IEventDispatcher); // true

			if (!(mySprite is Sprite)) return 1;
			if (!(mySprite is DisplayObject)) return 2;
			if (!(mySprite is IEventDispatcher)) return 3;

			return 0;
        	}
    	}
}
