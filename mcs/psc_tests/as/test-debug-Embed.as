// Compiler options: 
// Test embed in ps-codegen.cs (embed.g.cs)
package {
	import flash.display.Bitmap;
	import flash.display.Sprite;

	public class Test {

		[Embed(source="./as/playscript-embed.png")]
		public static var imgCls:Class;

		public static function Main():int {

			try {
				var imgObj:Bitmap = new imgCls();
			} catch (e:Error) {	
				// System.NotImplementedException in generic library
			}
			return 0;
		}
	}
}
