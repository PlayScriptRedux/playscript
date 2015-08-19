// Compiler options: -psstrict-
package {
    public class Foo {
        public static function Main():int {
		var xml:XML=<example id='123' color='blue'/>;

		trace(xml.attributes()[1].name()); //color

		return 0;
        }
    }
}
