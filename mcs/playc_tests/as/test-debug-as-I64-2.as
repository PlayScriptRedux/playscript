// Compiler options: -psstrict-
package {
    public class Foo {

	static var x:Number = 0.0;

        public static function Main():int {
		// trace(x == true);
		if ( x ) {
			trace(x);
			return 1;
		}

		return 0;
        }
    }
}
