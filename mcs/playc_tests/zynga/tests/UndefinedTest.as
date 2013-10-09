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

			return 0;
		}
	}
}
