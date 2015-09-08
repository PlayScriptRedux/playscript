// Compiler options: -psstrict-
package {
    public class Foo {
        public static function Main():int {

		var xml:XML = <example id='123' color='blue'/>;
		var attNamesList:XMLList = xml.@*;

		trace (attNamesList is XMLList); // true
		trace (attNamesList.length()); // 2

		for (var i:int = 0; i < attNamesList.length(); i++)
		{ 
		    trace (typeof (attNamesList[i])); // xml
		    trace (attNamesList[i].nodeKind()); // attribute
		    trace (attNamesList[i].name()); // id and color
		} 

		return 0;
        }
    }
}
