// Compiler options: -psstrict-
package
{
	public class UntypedReturnTypeTest
	{
		static var sClassName:String = "UntypedReturnTypeTest";
		
		public static function get ClassName()
		{
			return sClassName;
		}
		
		public static function set ClassName(className)
		{
			sClassName = className;
		}
	
		public function get SomeProperty()
		{
			return null;
		}
		
		public function set SomeProperty(property)
		{
		}
		
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
			F1 ();
			if (F2 (0) !== undefined)
				return 1;
			if (F2 (1) != 1)
				return 2;
				
			if (F3 (0) !== undefined)
				return 3;
			if (F3 (1) !== 1)
				return 4;
			
			if (F4 (0) !== undefined)
				return 5;
			if (F4 (1) !== 1)
				return 6;
			
			if (ClassName !== "UntypedReturnTypeTest")
				return 7;
			ClassName = "blah";
            if (ClassName !== "blah")
                return 8;
			
			trace ("ok");
			return 0;
		}
	}
}

