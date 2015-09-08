// Compiler options: -psstrict-
package {
    public class Foo {

// Testing for absolute namespace resolution

// as/test-as-AbsoluteNS-05.as(15,35): error CS1061: Type `string' does not contain a definition for `length' and no extension method `length' of type `string' could be found. Are you missing an assembly reference?

// /Users/administrator/Documents/Code/playscript/playscriptredux/playscript/mcs/playc_tests/../class/lib/net_4_5/mscorlib.dll (Location of the symbol related to previous error)

// as/test-as-AbsoluteNS-05.as(15,21): error CS1928: Type `string' does not contain a member `substr' and the best extension method overload `_root.String.substr(this string, double, double)' has some invalid arguments

	public static function slice (str:String):String {
		var foo:String = str;
		return substr(foo.substr(0, str.length));
	}

	public static function substr (str:String):String {
		return slice(str.substr(0,1));
	}

        public static function Main():int {
		return 0;
        }
    }
}
