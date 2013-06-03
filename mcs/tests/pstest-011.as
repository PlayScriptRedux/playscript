package
{

	public class Foo {

		public static function bar():void {
		}

		public function blah():void {
			this.bar();
		}

	}


	// Delegate and closure support.

	public class Test 
	{

		public static function Main():void {
			var f:Foo = new Foo();
			f.bar();
		}
	}
}

