// Compiler options: -psstrict+
package {
    public class Foo {
        public static function Main():int {

		var x:Boolean = false;
		var y:Boolean = true;

		x ||= y; 
		trace (x);
		if (!x) return 1;

		x = false;
		x = x || y;
		trace (x);
		if (!x) return 1;

		return 0;
        }
    }
}
