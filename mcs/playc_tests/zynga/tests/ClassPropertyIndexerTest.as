package
{
	import com.adobe.test.Assert;

	public class ClassPropertyIndexerTest
	{
		private var value:int = 4;
		
		public function RunTest():int
		{
			Assert.expectEq("this.value == 4", 4, this.value);
			Assert.expectEq("this[\"value\"] == 4", 4, this["value"]);
			this["value"] = 5;
			Assert.expectEq("this.value == 5", 5, this.value);
			Assert.expectEq("this[\"value\"] == 5", 5, this["value"]);
			this.value = 6;
			Assert.expectEq("this.value == 6", 6, this.value);
			Assert.expectEq("this[\"value\"] == 6", 6, this["value"]);
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
