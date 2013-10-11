package
{
	import flash.utils.Dictionary;

	public class UndefinedTest
	{
		public static function Main():int
		{
			var o:Object = undefined;
			if (o)
				return 1;
			if (o != null)
				return 2;
			if (o === null)
				return 3;
			for each (var i:* in o)
				trace(i);
			o = false;
			for each (var i:* in o)
				trace(i);

			var key:*;
			o = {};
			key = "undefined";
			if (o[key])
				return 4;
			o[key] = 2;
			if (o[key] != 2)
				return 5;
			key = "null";
			if (o[key])
				return 6;
			o[key] = 4;
			if (o[key] != 4)
				return 7;
			key = undefined;
			if (o[key] != 2)
				return 8;
			key = null;
			if (o[key] != 4)
				return 9;
			
			var d:Dictionary = new Dictionary();
			key = "undefined";
			if (d[key])
				return 10;
			d[key] = 2;
			if (d[key] != 2)
				return 11;
			key = "null";
			if (d[key])
				return 12;
			d[key] = 4;
			if (d[key] != 4)
				return 13;
			key = undefined;
			if (d[key] != 2)
				return 14;
			key = null;
			if (d[key] != 4)
				return 15;

			var a:Array = []
			key = "undefined";
			if (a[key])
				return 16;
			a[key] = 2;
			if (a[key] != 2)
				return 17;
			key = "null";
			if (a[key])
				return 18;
			a[key] = 4;
			if (a[key] != 4)
				return 19;
			key = undefined;
			if (a[key] != 2)
				return 20;
			key = null;
			if (a[key] != 4)
				return 21;

			return 0;
		}
	}
}
