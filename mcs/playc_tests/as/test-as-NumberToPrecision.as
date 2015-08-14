package {
	public class NumberTest  {
		public static function Main():int {
			var num:Number = 31.570;
			var str:String = num.toPrecision(3);
			var num2:Number = Number(str);
			trace(num.toPrecision(3)); // 31.6
			if (num != 31.570) return 1;
			if (str != "31.6") return 2;
			if (num2 != 31.6) return 3;
			return 0;
		}
	}
}
