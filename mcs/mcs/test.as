package Blah1
{
	public class ClassA {}
	public class ClassB {}
	public class ClassC extends ClassB {}

	public class EqualityTest
	{
		public static function Main():int
		{
			var a:ClassA = new ClassA();
			var a2:ClassA = new ClassA();
			var b:ClassB = new ClassB();
			var c:ClassC = new ClassC();

			var d1:Number, d2:Number;

			if (d1 == d1) {
				trace("IsNAN");
			}

			if (a == a) {
				trace("YAY!");
			}

			if (a == a2) {
				trace("YAY!");
			}

			if (a == true) {
				trace("YAY!");
			}

			if (b == c) {
				trace("YAY!");
			}

			if (a == b) {
				trace("YAY!");
			}

			return 0;
		}
	}
}
