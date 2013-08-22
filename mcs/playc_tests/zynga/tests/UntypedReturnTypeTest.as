package
{
	public class UntypedReturnTypeTest
	{
		public static function F1 ()
		{
			var x = 2;
		}
		
		public static function F2 (arg:int)
		{
			if (arg > 0)
				return 1;
				
			return;
		}
		
		public static function F3 (arg:int)
		{
			if (arg > 0)
				return 1;
		}
		
		public static function F4 (arg:int)
		{
			if (arg > 0)
				return 1;
		}
		
		public static function Main():int
		{
			var result:Object;
			F1 ();
			if (F2 (0) !== undefined)
				return 1;
			result = F2 (1);
			if (result != 1)
				return 2;
				
			if (F3 (0) !== undefined)
				return 3;
			result = F3 (1);
			if (result !== 1)
				return 4;
			
			if (F4 (0) !== undefined)
				return 5;
			result = F4 (1);
			if (result !== 1)
				return 6;
				
			trace ("ok");
			return 0;
		}
	}
}

