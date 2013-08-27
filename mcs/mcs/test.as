package org.computus.model
{

	public class Timekeeper
	{
		public static var m_direction:uint;

		public static function Main():void
		{
			var a:Array = new Array();

			if (false != a) {
				trace("foo");
			}

			var closestDirection:int = 0;

			if (closestDirection != Number.MAX_VALUE && closestDirection != m_direction) {
				trace("bar");
			}
		}
	}
}