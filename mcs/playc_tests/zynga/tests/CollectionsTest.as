package
{
	import flash.utils.*;
	import System.Collections.IDictionary;

	public class CollectionsTest
	{
		public static function Main():int
		{
			var key:String = "key1";
			var value:String = "value1";
			var dict:Dictionary = new Dictionary();
			dict[key] = value;
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

			var a:Array = [];
			a.push("apple");
			a.push(1);
			a.push("orange");
			if (a.indexOf("orange") != 2)
				return 5;
			if (a.indexOf(1) != 1)
				return 6;
			// TODO: support in operator for arrays
			//if (!("apple" in a))
			//	return 7;
			// TODO: support hasOwnProperty for arrays
			//if (!a.hasOwnProperty("apple"))
			//	return 8;
			var a2:Array = a.slice();
			if (!CompareArrays(a, a2))
				return 9;

			return 0;
		}

		public static function ContainsKey(dictionary:IDictionary, key:*):Boolean
		{
			if (!(key in dictionary))
				return false;
			// TODO: calling hasOwnProperty on type IDictionary generates invalid IL
			//if (!dictionary.hasOwnProperty(key))
			//	return false;
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

