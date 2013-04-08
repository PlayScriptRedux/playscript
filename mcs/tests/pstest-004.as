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
		
		}
		
	}

}
