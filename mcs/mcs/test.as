package
{
	import playscript.utils.*;
	import System.Diagnostics.Stopwatch;

	/** An AbstractClassError is thrown when you attempt to create an instance of an abstract 
	 *  class. */
	public class AbstractClassError extends Error
	{
		/** Creates a new AbstractClassError object. */
		public function AbstractClassError(message:*="", id:*=0)
		{
			super(message, id);
		}
	}

	public class Test
	{
		public static function Main():int
		{
			return 0;
		}
	}
}
