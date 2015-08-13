package
{
	import flash.utils.Dictionary;

	public class UndefinedTest
	{
		public static function Main():int
		{
			var o:* = undefined;
			if (o)
				return 1;
			if (o != null || o != undefined)
				return 2;
			if (o === null || o !== undefined)
				return 3;
			for each (var i:* in o)
				trace(i);
			o = false;
			for each (i in o)
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

			var a:Array = [];
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

			var undef:* = undefined;
			var instance:A = undef;
			if (instance !== null || instance === undefined)
				return 23;

			var a2:Array = [];
			var o2:* = a2.pop();
			if (o2 !== undefined || o2 === null)
				return 24;
			var o3:Object = a2.pop();

			// Moved test to ./as/test-debug-UndefinedvsNull-1.as
			//trace(o3); // should be null
			//if (o3 !== null || o3 === undefined)
			//	return 25;


			instance = a2.pop();
			trace(instance);
			if (instance !== null || instance === undefined)
				return 26;

			var d2:Dictionary = new Dictionary();
			// * should be undefined
			var value:* = d2["missing"];
			if (value !== undefined || d2["missing"] !== undefined)
				return 27;

			// class instance should be null
			instance = d2["missing"];
			if (instance !== null || instance === undefined)
				return 28;

			// object should be null
			// Moved test to ./as/test-debug-UndefinedvsNull-2.as
			var o4:Object = d2["missing"];
//			trace(o4);
//			if (o4 !== null || o4 === undefined)
//				return 29;

			var o5:Object = {};
			// * should be undefined
			value = o5["missing"];
			if (value !== undefined || d2["missing"] !== undefined)
				return 30;

			// object should be null
			var o6:Object = o5["missing"];
			if (o6 !== null || o6 === undefined)
				return 31;

			// class instance should be null
			instance = o5["missing"];
			if (instance !== null || instance === undefined)
				return 32;

			var d3:Dictionary = new Dictionary();
			var s:String = d3["missing"];
			if (s !== null)
				return 33;
			s = String(d3["missing"]);
			if (s != "undefined")
				return 34;
			s = d3["missing"] as String;
			if (s !== null)
				return 35;

			var o7:* = undefined;
			var n:Number = o7;
			if (n === null || !isNaN(n))
				return 36;
			n = Number(o7);
			if (n === null || !isNaN(n))
				return 37;

			return 0;
		}
	}
}

class A
{
}
