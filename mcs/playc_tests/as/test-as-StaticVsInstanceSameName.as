// Compiler options: -psstrict-
package {
	class Base { 
		public static var test:String = "static"; 
		public static function MyName():String {
			trace("Static method");
			return "Static";
		}
		public function MyName():String {
			trace("Instance method");
			return "Instance";
		}
	}

    public class Foo {
        public static function Main():int {
		var base:Base = new Base();
		if (base.MyName() == Base.MyName()) return 1;
		return 0;
        }
    }
}
