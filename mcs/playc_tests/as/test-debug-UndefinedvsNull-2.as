package
{
	import flash.utils.Dictionary;

	public class UndefinedTest
	{
		public static function Main():int
		{
			var d:Dictionary = new Dictionary();
			var o:Object = d["missing"];
			trace(o); // object should be null, flex/as3 compiler
			if (o !== null)
				return 1;
			return 0;
		}
	}
}

