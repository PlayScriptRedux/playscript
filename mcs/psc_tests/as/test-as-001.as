// Compiler options: -psstrict+
//
// Mono.CSharp.InternalErrorException: /Users/administrator/Documents/Code/playscript/playscriptredux/playscript/mcs/class/pscorlib/flash/filesystem/File.play(184,21): flash.filesystem.File.getDirectoryListing() ---> Mono.CSharp.InternalErrorException: /Users/administrator/Documents/Code/playscript/playscriptredux/playscript/mcs/class/pscorlib/flash/filesystem/File.play(185,5): ---> Mono.CSharp.InternalErrorException: Already created variable `name'
//  at Mono.CSharp.LocalVariable.CreateBuilder (Mono.CSharp.EmitContext ec) [0x00000] in <filename unknown>:0 


package {

    public class Foo {

        public static function Main():int {
		var name_of_array = new Array("value1","value2","value3");
 		for each (var name:String in name_of_array)
 		{
 	 		trace(name);
 	 	}
 		for each (name in name_of_array)
 		{
 	 		trace(name);
 	 	}
		return 0;
        }
    }

}
