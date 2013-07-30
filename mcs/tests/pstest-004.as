package
{
	import flash.display.DisplayObject;
	import System.*;
	import System.Diagnostics.*;
	import flash.utils.Dictionary;

	public class inlineAttribute extends System.Attribute {
	}

	public class Texture
	{
		public function foo():void {}

		public function get value():int{return 0;}

		public function get value2():int{return 0;}

		public function get valueF():Number{return mValueF;}
		public function set valueF(v:Number):void {mValueF = v;}

		private var mValueF:Number;
	}

	public class Texture2 extends Texture
	{
		public override function foo():void {}
		public function inlinable():void {}

		public override function get value():int{return 0;}
		public function get value_inlinable():int{return 0;}
	}


	public class Texture3 extends Texture
	{
		public override function foo():void {}
		public function inlinable():void {}
		public function set value_inlinable(val:int):void {}
	}






	public class Test 
	{
/*
		public static function TestSeal():void
		{
			var t:Texture = new Texture();
			var t2:Texture2 = new Texture2();
			var t3:Texture3 = new Texture3();

			t.foo();
			t2.foo();
			t2.inlinable();
			t3.foo();
			t3.inlinable();
		}
*/
		/*
		public static function TestSwitch():void
		{
			var obj:Object = "abc";
			var n:Number = 10.0;
			switch (n) {
			case 5.0: trace(5); break;
			case 10.0: trace(10); break;
			default:
				break;
			}

			switch (obj) {
			default: 
				trace(5);
				break;
			}

			switch (obj) {
			case "def": trace(1); break;
			case "abc": trace(2); break;
			case "xyz": trace(3); break;
//			case 1:  break;
			default: 
				trace(4);
				break;
			}

		}
*/
		
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

		

#if false
		public static function getNullSprite():flash.display.Sprite {
			return null;
		}

		public static function getNonNullSprite():flash.display.Sprite {
			return new flash.display.Sprite();
		}

		public static function ImplicitBooleanCastTest():void {

			var a:Object = false;
			var b:Object = true;
			var c:flash.display.Sprite = new flash.display.Sprite();
			var d:flash.display.Sprite = null;
			
			var e:int = 0;
			var f:Object = Object(e);
			var g:Boolean = null;

			if (getNullSprite ())
			{
				throw new InvalidOperationException();
			}

			if (!getNonNullSprite())
			{
				throw new InvalidOperationException();
			}


			
			if (a) {
				throw new InvalidOperationException();
			} 
			
			if (b) {
			} else {
				throw new InvalidOperationException();
			}
			
			if (c) {
			} else {
				throw new InvalidOperationException();
			}
			
			if (d) {
				throw new InvalidOperationException();
			} else {
			}
			
			if (e) {
				throw new InvalidOperationException();
			} else {
			}
			
			if (f) {
				throw new InvalidOperationException();
			} else {
			}

			if (g) {
				throw new InvalidOperationException();
			}
		}

#endif

	#if false
		public static function ImplicitStringCastTest():void {
			var o1:Object = 5;
			var o2:String = "abc";
			var o3:Texture = new Texture();

			var s1:String = String(o1);
			var s2:String = String(o2);
			var s3:String = String(o3);
			Debug.Assert(s1 == "5");
			Debug.Assert(s2 == "abc");
			Debug.Assert(s3 == "_root.Texture");
		}
#endif

//
//		public static function TestForEach():void {
//			var a:Array = [true, false];
//			for each (var tinted:Boolean in a)
//			{
//				trace(tinted);
//			}
//		}


		public static function getSize(o:Object):int {
			return 100;
		}

		public static function TestConvert():void {
//			var o:Object = 5.0;
//			var v:Number = o;
//			var s:String = o;
//			trace(o, v, s);
////
			/// 
			/*
			var t:Texture = new Texture();
			var ot:Object = t;
			ot.valueF = 123.0;
//			var v:Number = ot.valueF;
//			t.valueF = t.valueF * ot.valueF;
			trace(ot.valueF);

			t.valueF *= ot.valueF;
*/

//			var obj:Object = new Object();
//			obj.summary = 5;

//			obj.summary++;

//			var i:int = obj.summary;
//			var d:Number = obj.summary;
//			trace(obj.summary, i, d);
//			trace(i);


//			var array:Array = new Array();
//			var texture:Texture = null;
//			array.Add(new Texture());
//
//			var t2:Texture = texture || array[0];
//			trace(t2);

			/*
			var total:int  = 0;
			// create summary as dictionary
			var dict:Dictionary = new Dictionary();
			var tracked:Array = new Array();
			for (var o:Object in tracked) {
				var t:Object = tracked[o];
			
				var summary:Object = new Object();

				// get object size
				var size:int = getSize(o);

				// update summary for object context
				summary.count++;
				summary.total += size;

				total += size;
			}*/

			var summary:Object = new Object();

			// get object size
			var size:int = getSize(summary);

			// update summary for object context
			summary.total += size;


			trace(summary.total);

		}
	


//		public static function TestIndex():void {
//
//			var obj:Object = new Object();
//			obj["summary"] = 5;
//
//			var str:String = "summary";
//
//			var s:Object = obj[str];
//			trace(s);
//
//
//			obj[str] = 10.0;
//
//			obj.summary ++;
//
//
//
//			var i:int = obj.summary;
//			var d:Number = obj[str];
//			trace(i, d);
//		}
//
//
//		private static var mRootObject:Object = new Object();
//
//		/** Returns the root symbol name for this asset (the symbol of the root object) if it has one. */
//		public static function get rootSymbolName():String {
//			if (mRootObject != null && mRootObject.symbolName != null) {
//				return mRootObject.symbolName;
//			}
//			return null;
//		}
	

		public static function Main():void {
			//var z:Boolean = getNullSprite();
			//trace(z);

//			TestSeal();

//			TestSwitch();

//			TestForEach();
			//TestIndex();

			TestConvert();

			//removeFromQueue("a", "b");
			/*
			var dataStream:System.IO.Stream = null; // = new System.IO.FileStream("/tmp/amt3.log", System.IO.FileMode.Open);
			var reader = new System.IO.StreamReader(dataStream, System.Text.Encoding.UTF8);
			var str:String = reader.ReadToEnd();
			trace(str);*/
//			ImplicitBooleanCastTest();
//			ImplicitStringCastTest();

/*
			var s1:String = "1";
			var s2:String = null;
			var s3:String = "3";

			if (s1 || s2 || s3) {
				trace("yes");
			}

			var o1:String = s1 || s2 || s3;
			trace(o1);
*/



//			var a:Array = ["a", "b", "c", "d", "e"];


//			var a:flash.utils.Dictionary = new flash.utils.Dictionary;
//			a['a'] = 1;
//			a['b'] = 2;


//			var a:Array = ["a", "b", "c", "d", "e"];
//			var o:Object = a;
//
//			for each (var j:Object in o)
//			{
//				trace(j);
//			}
//
//
//			for (var i:Object in o)
//			{
//				trace(i);
//			}




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
