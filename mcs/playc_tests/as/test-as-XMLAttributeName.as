// Compiler options: -psstrict-
package {
    public class Foo {
        public static function Main():int {
		var xml:XML=<example id='123' color='blue'/>;
		if (xml.attribute("color") != "blue") return 1;
		trace(xml.attribute("color")); //blue
		return 0;
        }
    }
}
