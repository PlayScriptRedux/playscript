// Compiler options: -psstrict-
package {
	class Base { 
		public static var stc:String = "static";

		public function Base() {
			Base.stc = "instance";
		}

		public function get stc() : String {
			return Base.stc;
		}
	}

	public class Foo {
		public static function Main():int {
			trace(Base.stc);

			var base:Base = new Base();
			trace(base.stc);

			trace(Base.stc);

			return 0;
        	}
    	}
}
