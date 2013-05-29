package
{
	import flash.display.Sprite;
	import flash.events.Event;

	// Delegate and closure support.

	public class Test 
	{

		public static function Main():void {
			
			trace("blah");
			
			// Apply should work.			
			
			function foo():void { 
				trace("foo"); 
			}
			foo.apply(null, null);

			// Using the function id declared in parent block should work.
			
			var s:Sprite;
			s.addEventListener("onEvent", onEvent);			
 			function onEvent(event:Event):void
			{
				s.removeEventListener("eventName", onEvent);
			}
			
			// This should work
            resume();
            function resume():void {}

			// Function with variadic argumetns should work
			var va:Function = function(...args) { trace(arguments); }
			va();

			// We should be able to use a function in an object literal
			var objLit:Object = { a:"blah", b:100, c:resume, d:va, "e":foo, "f":bar };
		}

		public function bar():void {
			trace("bar");
		}
		
	}

}
