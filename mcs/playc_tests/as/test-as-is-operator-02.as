// Compiler options: -psstrict-
package {
	import flash.display.*;
	import flash.events.IEventDispatcher;

	public class Foo {

                public function expectType(description:String, expectedType:*, actualObject:*):Boolean
                {
                        var result:Boolean = (actualObject is expectedType);
                        return result;
                }

		public static function Main():int {
			return 0;
        	}
    	}
}
