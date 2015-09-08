// Compiler options: -psstrict-
package {
    public class Foo {
        public static function Main():int {
		var await = new String("");
		var l = await.Length;
		return 0;
        }
    }
}
