// Compiler options: -psstrict+
package {
	import System.*;
	import System.IO.*;
	import System.Collections.Generic.*;
	import flash.utils.*;

    public class Foo {

		public class LoaderHandler {

			public var sourceExt:String;
			public var targetExt:String;
			public var handler:Function;
			public var matcher:Function;

			public function LoaderHandler(sourceExt:String, targetExt:String, handler:Function, matcher:Function)
			{
				this.sourceExt = sourceExt;
				this.targetExt = targetExt;
				this.handler = handler;
				this.matcher = matcher;
			}
		}

        private static var loaderHandler:LoaderHandler;

        public static function Main():int {
	
if (loaderHandler != null) {
var callback:Func.<String,ByteArray,ByteArray> = Func.<String,ByteArray,ByteArray>(loaderHandler.handler);

}

		return 0;
        }
    }
}
