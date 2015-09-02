// Compiler options: -psstrict+
package {
    public class Foo {
        public static function Main():int {

		trace(12 % 5);    // 2 
		if (12 % 5 != 2) return 1;

		trace(-4 % 3); // -1
		if (-4 % 3 != -1) return 2;

		trace(-4 % -3); // -1
		if (-4 % -3 != -1) return 3;

		trace(4 % 4);     // 0
		if (4 % 4 != 0) return 4;

		return 0;
        }
    }
}
