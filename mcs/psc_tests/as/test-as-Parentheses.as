// Compiler options: -psstrict-
package {
    public class Foo {
        public static function Main():int {

		trace((2 + 3) * (4 + 5)); // 45
		if ((2 + 3) * (4 + 5) != 45) return 1;

		trace(2 + (3 * (4 + 5))); // 29
		if (2 + (3 * (4 + 5)) != 29) return 2;

		trace(2 + (3 * 4) + 5);   // 19
		if (2 + (3 * 4) + 5 != 19) return 3;

		trace(2 + (3 * 4) + 5);   // 19
		if (2 + (3 * 4) + 5 != 19) return 4;

		return 0;
        }
    }
}
