package
{
	import flash.utils.Dictionary;

	public class Test
	{
		public static function Main():void
		{
			Method1();

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

		[Inline]
		static function Method1():int {
			// ... Aggressive inlining.
			return "one".Length + "two".Length + "three".Length +
				"four".Length + "five".Length + "six".Length +
					"seven".Length + "eight".Length + "nine".Length +
					"ten".Length;
		}
	}
}
