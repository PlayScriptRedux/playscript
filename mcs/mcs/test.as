package
{
	import System.*;
	import System.Dynamic.*;
	import Microsoft.CSharp.RuntimeBinder.*;
	import System.Collections.Generic.*;
	
	public class Foo {
		public property a:int { get; set; }
		public property b:Number { get; set; }
		public property c:Array { get; set; }
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
			for each (var n:Number in a) {
				Console.WriteLine("{0} number.", n);
			}
		}

        public static function Main(args:String[]) : void {
	       	var a:Array;
        	a = [100, 200, 300];
            var s:String = '"Swami"';
			var o2:Object = {"a":100, b:200, c:[100, 200, 300]};
			var f:Foo = {"a":100, b:200, c:[100, 200, 300]};
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
		
