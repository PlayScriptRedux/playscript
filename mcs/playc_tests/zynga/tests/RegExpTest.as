package
{
	public class RegExpTest
	{
		public static function Main():int
		{
			var pattern:RegExp = /foo\d/; 
			var str:String = "foo1 foo2";
			if (pattern.global != false)
				return 1;
			// TODO: remove the need for the string cast
			if (String(pattern.exec(str)) != "foo1")
				return 2;
			if (pattern.lastIndex != 0)
				return 3;
			// TODO: remove the need for the string cast
			if (String(pattern.exec(str)) != "foo1")
				return 4;

			pattern = /foo\d/g;
			if (pattern.global != true)
				return 5;
			// TODO: remove the need for the string cast
			if (String(pattern.exec(str)) != "foo1")
				return 6;
			if (pattern.lastIndex != 4)
				return 7;
			// TODO: remove the need for the string cast
			if (String(pattern.exec(str)) != "foo2")
				return 8;

			var rePhonePattern1:RegExp = /\d{3}-\d{3}-\d{4}|\(\d{3}\)\s?\d{3}-\d{4}/; 
			str = "The phone number is (415)555-1212.";

			if (rePhonePattern1.extended != false)
				return 9;
			// TODO: remove the need for the string cast
			if (String(rePhonePattern1.exec(str)) != "(415)555-1212")
				return 10;

			var rePhonePattern2:RegExp = / \d{3}-\d{3}-\d{4}  |   \( \d{3} \) \ ? \d{3}-\d{4}  /x; 
			if (rePhonePattern2.extended != true)
				return 11;
			// TODO: remove the need for the string cast
			if (String(rePhonePattern2.exec(str)) != "(415)555-1212")
				return 12;

			str = "<p>Hello\n"
				+ "again</p>"
				+ "<p>Hello</p>";

			pattern = /<p>.*?<\/p>/;
			if (pattern.dotall != false)
				return 13;
			// TODO: remove the need for the string cast
			if (String(pattern.exec(str)) != "<p>Hello</p>")
				return 14;

			pattern = /<p>.*?<\/p>/s;
			if (pattern.dotall != true)
				return 15;
			// TODO: remove the need for the string cast
			if (String(pattern.exec(str)) != "<p>Hello\nagain</p>")
				return 16;

			pattern = /(\w*)sh(\w*)/ig;  
			str = "She sells seashells by the seashore";
			var results:Array = [];
			var result:Object = pattern.exec(str);
			while (result != null) {
				results.push(result.index + "-" + result);
				result = pattern.exec(str);
			}
			if (results.toString() != "0-She,,e,10-seashells,sea,ells,27-seashore,sea,ore")
				return 17;

			str = "abc12 def34";
			pattern = /([a-z]+)([0-9]+)/g;
			if (str.replace(pattern, Function(swapMatches)) != "12abc 34def")
				return 18;
			pattern = /([a-z]+)([0-9]+)/;
			if (str.replace(pattern, Function(swapMatches)) != "12abc def34")
				return 19;

			return 0;
		}

		public static function swapMatches(...args:Array):String
		{
			return args[2] + args[1];
		}
	}
}

