package
{
	// Test local variable hoisting.

	public class Test 
	{

		public static function Main():void {
		}

		public static function foo(qqq:Object=""):void {

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

			try {

			} catch (e:Error) {
				trace("err1");
			}

			try {

			} catch (e:Error) {
				trace("err2");
			}


			function foo():void {
			}

		}

	}

}
