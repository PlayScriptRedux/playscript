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

		
//		public static implicit operator int (list:Class) {
//			throw new System.NotImplementedException();
//		}

//		public static implicit operator Test (o:Object) {
//		{
//			if ( o is string)
//			{
//				return (string)o;
//			}
//			return o.ToString();
//		}



		public static function Main():void {


//			var a:Array = ["a", "b", "c", "d", "e"];


//			var a:flash.utils.Dictionary = new flash.utils.Dictionary;
//			a['a'] = 1;
//			a['b'] = 2;


			var a:Array = ["a", "b", "c", "d", "e"];
			var o:Object = a;

			for each (var j:Object in o)
			{
				trace(j);
			}


			for (var i:Object in o)
			{
				trace(i);
			}




//			var o1:Object = 5;
//			var o2:String = "abc";
//			var o3:Texture = new Texture();
//
//			var s1:String = String(o1);
//			var s2:String = String(o2);
//			var s3:String = String(o3);
//			trace(s1);
//			trace(s2);
//			trace(s3);
			

			/*
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
*/
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
