// Compiler options: -psstrict-
package {
    public class Foo {

// Testing for absolute namespace resolution

// as/test-as-AbsoluteNS-06.as(12,14): error CS1061: Type `string' does not contain a definition for `length' and no extension method `length' of type `string' could be found. Are you missing an assembly reference?

// /Users/administrator/Documents/Code/playscript/playscriptredux/playscript/mcs/playc_tests/../class/lib/net_4_5/mscorlib.dll (Location of the symbol related to previous error)

	public static function len (str:String):int {
		return str.length;
	}

        public static function Main():int {
		var str:String = "";
		return len(str);
        }
    }
}
