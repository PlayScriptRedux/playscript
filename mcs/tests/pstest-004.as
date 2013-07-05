package
{
	import flash.display.DisplayObject;
	import System.*;

	public class inlineAttribute extends System.Attribute {
	}

	public class Texture
	{
	}

	public class Test 
	{
	
		public static function Main():void {
			var a:Array = new Array();
			var cl:Class = Array;
			trace(a is cl);
		}

//		[inline]
//		public function set texture(texture:Texture):void { _texture = texture; }


	}

}
