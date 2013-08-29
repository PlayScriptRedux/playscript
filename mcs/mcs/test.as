package
{
	import flash.utils.Dictionary;

	public class Test
	{
		public static function Main():void
		{
			var d:* = new Dictionary();
			d["a"] = {};
			d["b"] = {};
			d["c"] = {};
			d["d"] = [];


			var blah:String = "a";
			var b:Boolean = (blah in d);
			if (b) {
				trace("Yay!");
			}

		}
	}

}
