// Compiler options: -psstrict-
package {
    public class Foo {
        public static function Main():int {
		var xml:XML=<example id='123' color='blue'/>;

		// ActionScript 3 preferred access method
		var id:String = xml.@id.toString();

		if (id != "123") return 1;
		trace(id); 

		return 0;
        }
    }
}
