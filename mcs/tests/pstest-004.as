package
{
	import flash.display.Sprite;
	import flash.events.Event;

	// Delegate and closure support.

	public class Test 
	{

		public static function Main():void {
		
			var qq:Array = [ "100", "200" ];
			
			var uu:uint = uint(qq[0]);
			var ii:int = int(qq[1]);
		
			var a:Array = null;
			
			for each (var i:int in a) {
				trace(i);
			}
			
			var b:Object = null;
			for each (var j:int in b) {
				trace(j);
			}
		
			var t:Test = new Test();
			t.foo ();
			t.bar ();
		}

		public function foo():void {

			// varargs anonymous methods

			var f1:Function = function(...args):void {
				trace(args);
				for each (var a:Object in args) {
					trace(a);
				}
			}    // NOTE: <-- No semicolon terminating this statement (compiler should add semicolon)

			// Test anonymous function embedded in an object literal (should not try to insert a semicolon)
			var o:Object = { f1 : function():void { trace("booyah!"); } }

			var f2:Function = function(...args):int {
				for each (var a:Object in args) {
					trace(a);
				}
				return 1;
			};

			var f3:Function = function(i:int, s:String, n:Number, ...args):void {
				trace(""+i+s+n);
				for each (var a:Object in args) {
					trace(a);
				}
			}    // NOTE: <-- No semicolon terminating this statement (compiler should add semicolon)

			var f4:Function = function(i:int, s:String, n:Number, ...args):int {
				trace(""+i+s+n);
				for each (var a:Object in args) {
					trace(a);
				}
				return 0;
			};

			// Test varargs anonymous method
			f1(true, 100, 482.45, "aaa");
			f2(true, 100, 482.45, "aaa");
			f3(100, "aaa", 123.45, "blah", 100, true, 323.44, "blah");
			f4(100, "aaa", 123.45, "blah", 100, true, 323.44, "blah");

			// Test varargs local method
			g1(true, 100, 482.45, "aaa");
			g2(true, 100, 482.45, "aaa");
			g3(100, "aaa", 123.45, "blah", 100, true, 323.44, "blah");
			g4(100, "aaa", 123.45, "blah", 100, true, 323.44, "blah");

			// varargs local methods

			function g1(...args):void {
				for each (var a:Object in args) {
					trace(a);
				}
			}

			function g2(...args):int {
				for each (var a:Object in args) {
					trace(a);
				}
				return 1;
			}

			function g3(i:int, s:String, n:Number, ...args):void {
				trace(""+i+s+n);
				for each (var a:Object in args) {
					trace(a);
				}
			}

			function g4(i:int, s:String, n:Number, ...args):int {
				trace(""+i+s+n);
				for each (var a:Object in args) {
					trace(a);
				}
				return 0;
			}

		}

		public function bar():void {

			var neighborCoords:Array = [{x:-1, y:0}, {x:1, y:0}, {x:0, y:-1}, {x:0, y:1}];

			var action3:Function = function():void { trace("action3"); };

			// Test functions in object declarations

			var o:Object = { blah:action, blah2:action2, blah3:action3, blah4:action4 };

			o.blah();
			o.blah2();
			o.blah3();
			o.blah4();

			// varargs local methods

			function action2():void {
				trace("action2");
			}

		}

		public function action():void {
			trace("action");
		}

		public static function action4():void {
			trace("action4");
		}

	}

}
