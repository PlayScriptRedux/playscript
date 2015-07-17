// Compiler options: -r:./as/Assert.dll
package
{
	import flash.utils.Dictionary;

	public class Test
	{
		public static function Main():int
		{
			var o:Object = { "key": null };
			var s:String = o["key"];
			if (s != null)
				return 1;
			var o2:* = o["key"];
			if (o2 != null)
				return 2;
			var o3:Object = o["key"];
			if (o3 != null)
				return 3;
			var d:Dictionary = new Dictionary();
			var f:Function;
			var a:Array = d[f] || new Array(6);
			if (a.length != 6)
				return 4;

			return 0;
		}
	}
}
