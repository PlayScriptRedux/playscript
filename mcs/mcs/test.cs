namespace Blah2
{
	public class ClassA {}
	public class ClassB {}
	public class ClassC : ClassB {}

	public class EqualityTest
	{
		public static int M2()
		{
			ClassA a = new ClassA();
			ClassA a2 = new ClassA();
			ClassB b = new ClassB();
			ClassC c = new ClassC();

			if (a == a) {
				_root.trace_fn.trace("YAY!");
			}

			if (a == a2) {
				_root.trace_fn.trace("YAY!");
			}

			if (b == c) {
				_root.trace_fn.trace("YAY!");
			}

//			if (a == b) {  // ERROR!
//				_root.trace_fn.trace("YAY!");
//			}

			return 0;
		}
	}
}
