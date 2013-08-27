package
{
	public class Foo
	{
		public var a:Foo, b:Test;
	}

	public class Test
	{
		public static function Main():void
		{
			var f:Foo = new Foo();
			f.a = new Foo();
			f.b = new Test();
		}
	}
}