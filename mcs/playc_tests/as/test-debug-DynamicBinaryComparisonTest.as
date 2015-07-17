// Compiler options: -r:./as/Assert.dll
package
{
	public class DynamicBinaryComparisonTest
	{
		public static function Main():int
		{
			var o:* = "honor";
			var objZero:* = 0;
			var objFalse:* = false;
			if (o < objZero)
				return 1;
			if (o > objZero)
				return 2;
			if (objFalse > objZero)
				return 3;
			if (objFalse < objZero)
				return 4;
			if (!(objFalse >= objZero))
				return 5;
			var objTrue:* = true;
			if (!(objFalse <= objZero))
				return 6;
			if (!(objTrue > objZero))
				return 7;
			if (objTrue < objZero)
				return 8;
			if (!(objTrue >= objZero))
				return 9;
			if (objTrue <= objZero)
				return 10;

			var objA1:* = new A();
			var objA2:* = new A();
			if (objA1 == objA2)
				return 11;
			if (!(objA1 >= objA2))
				return 12;
			if (!(objA1 >= "[Object A]"))
				return 13;
			if (objA1 == "[Object A]")
				return 14;

			var objUndef:* = undefined;
			if (objUndef >= undefined) // less/greater than undefined is always false
				return 15;
			if (objUndef <= undefined) // less/greater than undefined is always false
				return 16;
			if (objUndef != undefined) // but equality is true
				return 17;

			var objStringOne:* = "1";
			var objTrue:* = true;
			if (!(objStringOne > objFalse))
				return 18;
			if (objStringOne > objTrue)
				return 19;
			if (!(objStringOne >= objTrue))
				return 20;

			return 0;
		}
	}
}

class A
{
}
