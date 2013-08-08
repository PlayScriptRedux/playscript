package {

	public class Foo {

	}


	public class Test {


		public static function Main():void {

			var o:Object = "blah";
			var s:Foo = o as Foo;
			var q:Foo = o as o;
			var b:Boolean = o is String;

			o = s as Object;

			if (o is String) {
				trace("Blah");
			}

			if (o is Foo) {
				trace("blah!");
			}

		}
	}

}
