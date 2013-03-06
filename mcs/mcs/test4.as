package com.zynga.zengine.classes
{
	import System.*
	
	PLATFORM::IOS {
		import blah.blah.blah
	}

	public interface IBlah {
		
		function boo():void
			
		function get ya():int
				
		function set goo(i:int):void
				
	}

	public class ZZZAttribute 
	extends 
	Attribute 
	{
	}

	public function blah():int 
	{
		var dynamic:int = 400
		return 100 }

	[ZZZ]
	public class Foo 
	{
		public var i:int = 100
	}

	public class Bar 
		extends Foo {
		public var j:int = 200
	}

	public class Test 
	{
		public var a1:int, b1:String, c1:Number;
		public static var a2:int = 0, b2:String = "asd", c2:Number = 1.3;
		public const a3:int = 0, b3:String = "asd", c3:Number = 1.3;

		public var dynamic:String = "asdadf";

		public var v:Vector.<String> = new <String> [ "aaa"
		                                             , "bbb"
		                                             , "ccc"
		                                             ]

		public var gg:Object = { "adf":1324
			, ffff:{ "asdfadf":true } }

        public static function Main() : void {
        
			use namespace Blah;

			myFunc(100, "Blah");

			var q:int
			q++
			++q

			(q < 100 / 4 + -2) ? 1 : 4;

			if (q != 0) trace("blah") else trace("blah2")

			q = q
				- 100 // this is a comment

        	var x:XML = <spoof/>;
        
        	var f:Foo = new Foo()
        	var b:Bar = new Bar()
        	var t:Boolean 
					= true /* another comment */
        	
			var o33:Object = null
        	if (f == b) 
        		trace ("foo")
        	
        	if (f && t) {
        		trace("Is it fixed?");
        	}
        	
        	var o1:Object = "foo";
        	var o2:Object = "bar";
        	if (o1 != null && o2 == null) {
        		trace("Yeah!");
        	}

			function myFunc(i:int, s:String):void {
				trace("Foo " + i + s);
			}

			var ss:String = typeof f;

			var qqq:Object = new byte[100];
		}
	}
}
