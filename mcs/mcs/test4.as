package com.zynga.zengine.classes
{
	PLATFORM::IOS {
		import blah.blah.blah;
	}

	public function blah():void {
		var dynamic:int = 400;
	}

	public class Foo {
		public var i:int = 100;
	}

	public class Bar extends Foo {
		public var j:int = 200;
	}

	public class Test 
	{
		public var dynamic:String = "asdadf";

		public var v:Vector.<String> = new <String> [ "aaa", "bbb", "ccc" ];

        public static function Main() : void {
        
			myFunc(100, "Blah");

        	var x:XML = <spoof/>;
        
        	var f:Foo = new Foo();
        	var b:Bar = new Bar();
        	var t:Boolean = true;
        	
        	if (f == b) {
        		trace ("foo");
        	}
        	
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
		}
	}
}
