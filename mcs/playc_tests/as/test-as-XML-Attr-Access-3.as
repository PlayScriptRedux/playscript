// Compiler options: -psstrict-
package {
    public class Foo {
        public static function Main():int {
		var xml:XML=<example id='123' color='blue'/>;

		var idAttrArrayIndexer:String = xml["@id"];
		trace(idAttrArrayIndexer);
		if (idAttrArrayIndexer != "123") return 1;

		return 0;
        }
    }
}
