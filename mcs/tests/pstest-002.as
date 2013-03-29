package
{
	// Test local variable hoisting in AS

	public class Test 
	{
		public static function Main():void {

			// This should generate a warning
			i = 100;
			j = 40;
			trace(k);

			{
				{
					{
						// This should declare i for outer blocks, but generate a warning
						var i:int = 200;
						var j:int = 300, k:Number = 400.0;
					}
				}
			}

			trace(i);
			trace(j);
			trace(k);

			// This should generate a warning
//			var i:int = 300;
		}
	}

}
