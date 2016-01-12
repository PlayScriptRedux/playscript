// Compiler options: -psstrict+
// https://github.com/PlayScriptRedux/playscript/issues/111
package
{
	public class MainClass
	{
		public static function Main():void
		{
			trace("Hello World from ActionScript!");
			var main:MainClass = new MainClass();
			main.oldFunction();
			main.newFunction();
		}

		[Deprecated(replacement="newFunction")] 
		public function oldFunction():void
		{
		}

		public function newFunction():void
		{
		}
	}
}
