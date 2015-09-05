// Compiler options: -psstrict-
package {
    public class Foo {

// Testing for absolute namespace resolution

// error CS1061: Type `string' does not contain a definition for `length' and no extension method `length' of type `string' could be found. Are you missing an assembly reference?

// error CS1928: Type `string' does not contain a member `substr' and the best extension method overload `_root.String.substr(this string, double, double)' has some invalid arguments

	public static function slice (str:String):String {
		return str.slice(0);
	}

	public static function substr (str:String):String {
		return slice(str);
	}

        public static function Main():int {

		var s:String;

		return 0;
        }
    }
}
