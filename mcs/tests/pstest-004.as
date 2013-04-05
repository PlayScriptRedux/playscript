package
{
	// Test local variable hoisting.

	public class Test 
	{

		public static function Main():void {
		}

		public static function foo(i:int):void {

			// This should generate a warning, but work.
			i = 100;

			{
				{
					{
						// This should be hoisted to top block.
						var i:int = 100;
					}
				}
			}

			// This should generate a warning, but work.
			var i:int = 200;

		}

	}

}
