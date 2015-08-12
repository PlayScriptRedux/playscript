// Compiler options: -debug -r:./as/Assert.dll
package
{
	import com.adobe.test.Assert;
	
	public class ArrayIndexTest
	{
		public static function Main():int
		{
			Assert.errcount = 0;
			var r:Boolean;
			var a1:Array = [ 11.0, 22.0, 33.0, 44.0];
			var i1:int = a1.indexOf(33);
			Assert.expectEq("i1 != 0", true, i1 != 0);
			Assert.expectEq("i1 != 1", true, i1 != 1);
			Assert.expectEq("i1 == 2", true, i1 == 2);
			Assert.expectEq("i1 != 3", true, i1 != 3);
			Assert.expectEq("a1[0] == 11.0", 11.0, a1[0]);
			Assert.expectEq("a1[1] == 22.0", 22.0, a1[1]);
			Assert.expectEq("a1[2] == 33.0", 33.0, a1[2]);
			Assert.expectEq("a1[3] == 44.0", 44.0, a1[3]);
			Assert.expectEq("a1[4] == null", null , a1[4]);
			return Assert.errcount;
		}
    }
}
