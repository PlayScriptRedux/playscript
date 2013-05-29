package
{
	import flash.display.Sprite;
	import flash.events.Event;

	// Delegate and closure support.

	public class Test 
	{

		public static function Main():void {
			var t:Test = new Test();
			t.bar ();
		}

		public function bar():void {

			var action3:Function = function():void { trace("action3"); };

			var o:Object = { blah:action, blah2:action2, blah3:action3, blah4:action4 };

			o.blah();
			o.blah2();
			o.blah3();
			o.blah4();

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
