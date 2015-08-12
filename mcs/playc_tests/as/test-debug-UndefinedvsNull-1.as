package
{
	import flash.utils.Dictionary;

	public class UndefinedTest
	{
		public static function Main():int
		{
			var a1:Array = [];
			var o1:Object = a1.pop();
			trace(o1); // null via flex/as compiler
			if (o1 !== null)
				return 1;
			return 0;
		}
	}
}

