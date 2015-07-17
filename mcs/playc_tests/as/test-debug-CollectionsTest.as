package
{
	import flash.utils.*;
	import System.Collections.IDictionary;

	public class CollectionsTest
	{
		public static function Main():int
		{
			//
			// Test Dictionary functionality
			//
			var key:String = "key1";
			var value:String = "value1";
			var dict:Dictionary = new Dictionary();
			dict[key] = value;
			delete dict[4]; // should do nothing, just testing that it compiles and doesn't crash
			if (!ContainsKey(dict, key) || !dict.hasOwnProperty(key))
				return 1;
			delete dict[key];
			if (ContainsKey(dict, key) || dict.hasOwnProperty(key))
				return 2;

			var typedDict:Dictionary.<String, String> = new Dictionary.<String, String>();
			typedDict[key] = value;
			if (!ContainsKey(typedDict, key) || !typedDict.hasOwnProperty(key))
				return 3;
			delete typedDict[key];
			if (ContainsKey(typedDict, key) || typedDict.hasOwnProperty(key))
				return 4;

			//
			// Test Array functionality
			//
			var a:Array = [];
			a.push("apple");
			a.push(1);
			a.push("orange");
			delete a["0.0"]; // should set index 0 to undefined
			delete a[0]; // should set index 0 to undefined
			if (a[0] !== undefined)
				return 5;
			if (a.indexOf("orange") != 2)
				return 6;
			if (a.indexOf(1) != 1)
				return 7;
			if (!("0.0" in a))
				return 8;
			if (!a.hasOwnProperty("0.0"))
				return 9;
			var a2:Array = a.slice();
			if (!CompareArrays(a, a2))
				return 10;
			if (a["0.0"] != a[0])
				return 11;
			if (a["0.1"] != undefined)
				return 12;
			a["0.1"] = 4;
			if (a["0.1"] != 4)
				return 13;

			//
			// Test Vector functionality
			//
			var v:Vector.<int> = new Vector.<int>();
			v.push(9);
			v.push(2);
			delete v[0]; // should do nothing
			delete v["0.0"]; // should do nothing
			if (v[0] !== 9)
				return 14;
			if (v["0.0"] !== 9)
				return 15;
			var re:ReferenceError = null;
			try {
				trace(v["0.1"]);
			} catch (e:ReferenceError) {
				re = e;
			}
			if (re == null)
				return 16;
			var re2:ReferenceError = null;
			try {
				v["0.1"] = 4;
			} catch (e2:ReferenceError) {
				re2 = e2;
			}
			if (re2 == null)
				return 17;
			v["0.0"] = 12;
			if (v["0.0"] !== v[0])
				return 18;
			if (!("0.0" in v))
				return 19;
			if (!v.hasOwnProperty("0.0"))
				return 20;
			if (v.length != 2)
				return 21;
			// TODO: error CS1955: The member `_root.Vector<int>.this[string]' cannot be used as method or delegate
			//v["push"](8);
			Object(v).push(8);
			if (v.length != 3)
				return 22;
			if (v[2] !== 8)
				return 23;

			return 0;
		}

		public static function ContainsKey(dictionary:IDictionary, key:*):Boolean
		{
			if (!(key in dictionary))
				return false;
			if (!dictionary.hasOwnProperty(key))
				return false;
			return true;
		}

		public static function CompareArrays(list1:Array, list2:Array):Boolean
		{
			if (list1.length != list2.length)
				return false;
			var length:int = list1.length;
			for (var i:int = 0; i < length; i++) {
				if (list1[i] != list2[i])
					return false;
			}
			return true;
		}
	}
}

