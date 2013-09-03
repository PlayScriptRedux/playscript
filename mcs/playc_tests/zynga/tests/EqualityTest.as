package
{
	import com.adobe.test.Assert;

	public class EqualityTest
	{
		public static function Main():int
		{
			var num1:Number = new Number();
			num1 = 1.0;
			var num2:Number = 1;
			Assert.expectEq("NaN", "NaN number", NaN);
			Assert.expectEq("num1 === num2", true, num1 === num2);
			Assert.expectEq("num1 == num2", true, num1 == num2);
			var int1:int = 1;
			Assert.expectEq("num1 == int1", true, num1 == int1);
			Assert.expectEq("num1 === int1", true, num1 === int1);
			var uint1:int = 1;
			Assert.expectEq("num1 == uint1", true, num1 == uint1);
			Assert.expectEq("num1 === uint1", true, num1 === uint1);
			Assert.expectEq("\"5\" == \"5\"", true, "5" == "5");
			Assert.expectEq("\"5\" === \"5\"", true, "5" === "5");
			Assert.expectEq("\"5\" == 5", true, "5" == 5);
			Assert.expectEq("\"5\" !== 5", true, "5" !== 5);
			Assert.expectEq("true === true", true, true === true);
			Assert.expectEq("true == true", true, true == true);
			Assert.expectEq("true !== false", true, true !== false);
			Assert.expectEq("true == 1", true, true == 1);
			Assert.expectEq("true !== 1", true, true !== 1);
			Assert.expectEq("null == undefined", true, null == undefined);
			Assert.expectEq("undefined == null", true, undefined == null);
			Assert.expectEq("null !== undefined", true, null !== undefined);
			Assert.expectEq("undefined !== null", true, undefined !== null);
			Assert.expectEq("undefined === undefined", true, undefined === undefined);
			var obj1:Object = 1;
			var obj2:Object = 1.0;
			Assert.expectEq("obj1 === obj2", true, obj1 === obj2);
			Assert.expectEq("obj1 === 1", true, obj1 === 1);
			var obj3:Object = new Object();
			var obj4:Object = new Object();
			Assert.expectEq("obj3 !== obj4", true, obj3 !== obj4);
			Assert.expectEq("\"5.5\" == 5.5", true, "5.5" == 5.5);
			Assert.expectEq("\"5.5\" !== 5.5", true, "5.5" !== 5.5);
			return 0;
		}
	}
}
