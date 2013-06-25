package
{
	import flash.display.Sprite;
	import flash.events.Event;

	// Delegate and closure support.

	public class Test 
	{
	
		public function foo(a:int):int {
			return a + a;
		}

		public function bar(b:Number):Number {
			return b * b;
		}
		
		public override function toString():String {
			return "blah";
		}

		public static function Main():void {
			var t:Test = new Test();
			trace(t.foo(100));
			trace(t.bar(200.0));
		}

	}

}
