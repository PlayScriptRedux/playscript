package
{
	import flash.events.*;
	import System.*;
	import System.Dynamic.*;
	import Microsoft.CSharp.RuntimeBinder.*;
	import System.Collections.Generic.*;
	
	public class Foo extends EventDispatcher {
		public property a:int { get; set; }
		public property b:Number { get; set; }
		public property c:Number[] { get; set; }
	}
	
	public class MyClass 
	{
		private var _foo:int = 200;
		private var _spiggles:String = "Blah";
		private var _skig:int[,] = [[100,200], [200, 300]];

		public static function printVec(v:Vector.<Number>) : void {
			for each (var n:int in v) {
				Console.WriteLine("{0} number.", n);
			}
		}

		public static function printArray(a:Array) : void {
			for each (var o:Object in a) {
				Console.WriteLine("{0} number.", o);
			}
		}
		
		public static function printDblArray(a:Number[]):void {
			for each (var n:Number in a) {
				trace(n.toString() + " number.");
			}
		}
		
		public static function onActivated(ev:Event):void {
			trace("Event type " + ev.type + " was triggered!");
		}

        public static function Main(args:String[]) : void {
        	trace(String(32));
        	trace("Hello world!");
        	trace("one","two");
        	trace("one","two","three");
        	trace("one","two","three","four");
        	trace("one",2,"three",true,"five",null,"seven",123.456,"nine");
	       	var a:Array;
        	a = [100, 200, 300];
            var s:String = '"Swami"';
			var o2:Object = {"a":100, b:"blah", c:[100, {"d":200, e:"spackle", f:["blah1", "blah2", "blah3"]}, 300]};
			o2.a = true;
			var f:Foo = {"a":100, b:200, c:[998, 999, 1000]};
			f.addEventListener(Event.ACTIVATE, onActivated);
			f.dispatchEvent(new Event(Event.ACTIVATE));
			printDblArray(f.c);
			var l:Vector.<Number> = [123, 123, 444];
			printVec(l);
			printVec([100, 200, 300, 400, 500, 600]);
			printArray([101, 201, 301, 401, 501, 601]);
			var sk:int[] = [123, 123, 444];
			l.push(100);
			l.push(200);
			var o:Object = l;
			l.push(400);
			Console.WriteLine("Hello World {0}!", l.length);
		}
	}
}
		
