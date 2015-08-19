// Compiler options: -psstrict-
package {
    public class Foo {
        public static function Main():int {
		var xml:XML=<example id='123' color='blue'/>;

		var idAttribute:String = xml.attribute("id");
		trace(idAttribute);
		if (idAttribute != "123") return 1;
		
		var idAttrArrayIndexer2:String = xml.@["id"];
		trace(idAttrArrayIndexer2);
		if (idAttrArrayIndexer2 != "123") return 2;

		return 0;
        }
    }
}
