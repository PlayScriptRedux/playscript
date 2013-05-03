package
{
	// Test embed support..
	
	public class Test
	{
		[Embed(source="source")]
		public static var data:Class;

		public static function Main():void {
			var d:Object = new data();
		}
	
	}

}
