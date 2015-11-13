// Compiler options: -psstrict-
package {
    public class Foo {
        public static function Main():int {

		// The following example shows how array and object initializers can 
		// be nested within each other:

		var person2:Object = {name:"Gina Vechio", children:["Ruby", "Chickie", "Puppa"]}; 

		// The following code uses the information in the previous example 
		// and produces the same result using a constructor function:

		var person:Object = new Object(); 
		person.name = "Gina Vechio"; 
		person.children = new Array(); 
		person.children[0] = "Ruby"; 
		person.children[1] = "Chickie"; 
		person.children[2] = "Puppa"; 

		// Thus person and person2 should contain identicial values

		if (person.name != person2.name) return 9;

		if (person.children[2] != person2.children[2]) return 1;
		if (person.children[1] != person2.children[1]) return 2;
		if (person.children[0] != person2.children[0]) return 3;

		return 0;
        }
    }
}
