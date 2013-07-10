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
		


			var dict = new flash.utils.Dictionary();
			dict["a"] = 5;
			dict["b"] = 2;
			delete dict["a"];
			delete dict.b;

			var sd = new flash.display.ShaderData(null);
			sd["a"] = 5;
			sd["b"] = 2;
			delete sd["a"];
			delete sd.b;


			var sdobj:Object = sd;
			delete sdobj["x"];

//			var str = "abc";
//			delete str["x"]; // should not compile
			

//			
//			var list:Vector.<int> = new Vector.<int>;
//			var list2:Vector.<int> = new Vector.<int>();
//			trace(list);
//			trace(list2);
//
//			var list3:System.Collections.Generic.List.<int> = new System.Collections.Generic.List.<int>;
//			var list4:System.Collections.Generic.List.<int> = new System.Collections.Generic.List.<int>();
//			trace(list3);
//			trace(list4);
//

//			var a:Array = new Array();
//			var cl:Class = Array;
//			trace(a is cl);
		}

//		[inline]
//		public function set texture(texture:Texture):void { _texture = texture; }


	}

}
