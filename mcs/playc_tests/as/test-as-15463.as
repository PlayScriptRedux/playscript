// Compiler options: -psstrict+ -optimize-
// Bug 15463 - mono converts float multiplication back to a float before casting to int
// In ActionScript this should be 70, not 69 as it would be in C#
package {
    public class Foo {
        public static function Main():int {
		trace (int(0.7 * 100.0));
		var i:int = int(0.7 * 100.0);
		if (i != 70) 
			return 1;
		return 0;
        }
    }
}
