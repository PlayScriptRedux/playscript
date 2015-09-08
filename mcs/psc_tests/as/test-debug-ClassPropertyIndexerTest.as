// Compiler options: -r:./as/Assert.dll
package
{
	import com.adobe.test.Assert;

	public class ClassPropertyIndexerTest
	{
		private var value:int = 4;
		
		public function GetLetterE():String
		{
			return "e";
		}
		
		public function RunTest():int
		{
			Assert.expectEq("this.value == 4", 4, this.value);
			Assert.expectEq("this[\"valu\" + \"e\"] == 4", 4, this["valu" + "e"]);
			this["value"] = 5;
			Assert.expectEq("this.value == 5", 5, this.value);
			Assert.expectEq("this[\"valu\" + GetLetterE()] == 5", 5, this["valu" + GetLetterE()]);
			this.value = 6;
			Assert.expectEq("this.value == 6", 6, this.value);
			Assert.expectEq("this[\"value\"] == 6", 6, this["value"]);
			this["valu" + GetLetterE()] = 7;
			Assert.expectEq("this.value == 7", 7, this.value);
			Assert.expectEq("this[\"value\"] == 7", 7, this["value"]);
			return 0;
		}
		
		public static function Main():int
		{
			var instance:ClassPropertyIndexerTest = new ClassPropertyIndexerTest();
			instance.RunTest();
			return 0;
		}
	}
}
