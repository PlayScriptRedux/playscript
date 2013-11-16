package
{
	import com.adobe.test.Assert;
	
	public class ArrayIndexTest
	{
		public static function Main():int
		{
			testRun(doTests);		 
			return 0;
		}
		
		public static function doTests():void
		{
			var a1:Array = [ 11.0, 22.0, 33.0, 44.0];
	        var i1:int = a1.indexOf(33);		
			test(i1 == 2);
		}
	}
}
