package
{
	public class UndefinedTest
	{
		public static function Main():int
		{
			var o:Object = undefined;
			if (o)
				return 1;
			if (o != null)
				return 2;
			if (o === null)
				return 3;
			for each (var i:* in o)
				trace(i);

			return 0;
		}
	}
}
