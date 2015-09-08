package {
	public class NumberTest  {
		public static function Main():int {
			var num:Number = new Number(10.456345);
			var str:String = num.toFixed(2);
			trace(num); // 10.456345
			if (num != 10.456345) return 1;
			trace(str); // 10.46
			if (str != "10.46") return 2;
			return 0;
		}
	}
}
