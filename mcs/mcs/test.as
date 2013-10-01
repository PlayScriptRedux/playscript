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

//			if (a == b) {  // ERROR!
//				trace("YAY!");
//			}

			return 0;
		}
	}
}
