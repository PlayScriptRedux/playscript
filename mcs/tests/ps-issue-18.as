package blah {

	public class Program {

		public static function Main():void {

			var o:Object;

			// If /dynamic- is used on command line, the following code should generate various errors

			o.blah("foo");

			var qq:Object = o[100];
		}

	}

}
