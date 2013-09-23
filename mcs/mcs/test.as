package Blah1
{
	public dynamic class A {

		public var a:int;
		private var _b:Boolean;

		public function get b():Boolean {
			return _b;
		}

		public function set b(value:Boolean):void {
			_b = value;
		}

	}

	public class EqualityTest
	{
		public static function Main():int
		{
			var foo:A = new A();

			foo["a"] = 100;
			foo["b"] = true;
			foo["c"] = "blah";

			trace("output: " + foo["a"] + foo["b"] + foo["c"]);

			return 0;
		}
	}
}
