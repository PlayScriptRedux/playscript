// Compiler options: -psstrict-
package {
    public class Foo {

	static var x:int = 0;

        public static function Main():int {
		if ( x ) {
			trace(x);
			return 1;
		}

		return 0;
        }
    }
}
