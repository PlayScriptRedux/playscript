package com.zynga.zengine.classes
{
	PLATFORM::IOS {
		import blah.blah.blah
	}

	PLATFORM::IOS
	import blah.blah


	PLATFORM::IOS
	public function foo():void {

	}

	PLATFORM::IOS
	public interface IBlah {
		
		function boo():void
				
	}


	[Partial]
	public class Test 
	{
		PLATFORM::IOS {
			public function foo():void {

			}
		}

		public function get goo():int {
			return 10;
		}

		public function set goo(i:int):void {
		}

		public static function Main():void {

			var v:Test;

			v.goo;

			var i:int = 100;
			if (i == 100) {
				trace("blah");
			}

			PLATFORM::IOS {
				trace("boo!");
			}
		}

	}

	[Partial]
	public class Test
	{
		public function bar():void
		{
			trace("bar");
		}
	}
}
