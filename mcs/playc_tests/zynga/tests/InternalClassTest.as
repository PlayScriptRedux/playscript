package
{
	public class InternalClassTest
	{
		public function InternalClassTest(lock:StaticLock1)
		{
		}

		public function InternalClassTest(lock:StaticLock2)
		{
		}

		public static function Main():int
		{
			return 0;
		}
	}
}

class StaticLock1
{
}

internal class StaticLock2
{
}
