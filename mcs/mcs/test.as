package
{
	import flash.events.*;
	import System.*;
	use namespace internal_ns;
	import System.Dynamic.*;
	import Microsoft.CSharp.RuntimeBinder.*;
	import System.Collections.Generic.*;
	
	public namespace fwubnutz_extreme = "http://www.blah.fwubnutz.com/blah";
	
	public function blahBlah(v:int):void {
		trace(v);
	}
	
	public function fooBaz(...flub):void {
//		var l:int = flub.length;
//		for (var i:int = 0; i < l; i++) {
//			var p:Object = flub[i];
//			trace("param " + p);
//		}
	}
	
	public class TestAttribute extends Attribute {
	}
	
	public class Fooie1 {
		public static function get q():Number {
		 	return 0; }
		public static function set q(value:Number):void {
		}	
	}

	public class Fooie2 extends Fooie1 {
		public static function get q():Number {
		 	return 0; }
		public static function set q(value:Number):void {
		}	
	}
	
			
	[Test]
	public class Foo extends EventDispatcher {
	
		fwubnutz_extreme static const BLAH:int = 100;
		public static const FOO:String = "asdf";
			
		public static var _q2:Array = [];
	
		private var _a:int;
		private var _b:Number;
		private var _jj:*=100;
		private var _c:Vector.<Number> = new Vector.<Number>();
		public function get a():int {
		 	return _a; }
		public function set a(value:int):void {
			_a = value;
		}
		public function get b():Number {
		 	return _b; }
		public function set b(value:Number):void {
			_b = value;
		}
		public function get c():Vector.<Number> {
		 	return _c; }
		public function set c(value:Vector.<Number>):void {
			_c = value;
		}
		
		public static function get q():Number {
		 	return 0; }
		public static function set q(value:Number):void {
		}
		
	}
	
	public class MyClass 
	{
		internal_ns var _foo:int = 200;
		public static var _q:Vector.<Foo> = new <Foo>[];
		private var _spiggles:String = "Blah";
//		private var _skig:int[,] = [[100,200], [200, 300]];

		public static function printVec(v:Vector.<Number>) : void {
			for each (var n:int in v) {
				Console.WriteLine("{0} number.", n);
			}
		}

		public static function printArray(a:Array) : void {
			var object:Object = null;
			for each (object in a) {
				Console.WriteLine("{0} number.", object);
			}
		}
		
		public static function printDblArray(a:Vector.<Number>):void {
			for each (var n:Number in a) {
				trace(n.toString() + " number.");
			}
		}
		
		public static function onActivated(event:Event):void {
			trace("Event type " + event.type + " was triggered!");
		}

		public static function dumpDict(d:Dictionary.<String,int>):void {
			trace('Dump dict..');
	       	for each (var value:int in d) {
	       		trace(value);
	       	}
	       	for (var key:String in d) {
	       		trace(key);
	       	}
	       	trace('----');
		}
		
		public static var obj:Object={};

        public static function Main() : void {
        	var o1:Object = 100;
        	o1 = o1 + 2;
        	var len:int = 20, qq:Number = 100.3, ss1:String = "blah";
        	trace(qq);
        	trace(ss1);
        	var i:int = 0;
        	var v:Vector.<int> = new <int> [ 1, 2, 3, 4, 5, 6, 7, 8 ];
        	len = v.length;
        	for (i in v) {
        		trace("# " + i);
        	}
        	for (var z:int = 0; z < len; z++) {
        		trace("## " + z);
        	}
        	var un:*=undefined;
        	var re:Object = /blah/g;
        	var o6:Object = { a:100, b: 200 };
        	if ("a" in o6) {
        		trace("a is in o6");
        	}
        	if (!("q" in o6)) {
        		trace("q is not in 06");
        	}
        	delete o6.a;
        	trace(String(32));
        	trace("Hello world!");
        	trace("one","two");
        	trace("one","two","three");
        	trace("one","two","three","four");
	       	trace("one",2,"three",true,"five",null,"seven",123.456,"nine");
	       	var d:Dictionary.<String,int> = new Dictionary.<String,int>();
	       	d['key1'] = 100;
	       	d['key2'] = 200;
	       	var o7:Object={};
	       	dumpDict(d);
	       	delete d['key1'];
	       	dumpDict(d);
	       	delete d['key2'];
	       	dumpDict(d);
	       	var a:Array;
        	a = [100, 200, 300];
            var s:String = '"Swami"';
			var o2:Object = {"a":100, b:"blah", c:[100, {"d":200, e:"spackle", f:["blah1", "blah2", "blah3"]}, 300]};
			o2.a = true;
			var o3:Object = new Foo;
			{ o3 = new Foo; }
			o3 = new Foo;
			if (o3 == null) {
				o3 = null;
			}
			var f:Foo = { "a":100, b:200, c:[998, 999, 1000] };
			var fn:Function = onActivated;
			f.addEventListener(Event.ACTIVATE, onActivated);
			f.dispatchEvent(new Event(Event.ACTIVATE));
			printDblArray(f.c);
			var l:Vector.<Number> = [123, 123, 444];
			printVec(l);
			printVec([100, 200, 300, 400, 500, 600]);
			printArray([101, 201, 301, 401, 501, 601]);
			l.push(100);
			l.push(200);
			var o:Object = l;
			l.push(400);
			Console.WriteLine("Hello World {0}!", l.length);
		}
	}
}

{
	import System.*;

	public class Blah2 {
		private var i:int;
	}
}
