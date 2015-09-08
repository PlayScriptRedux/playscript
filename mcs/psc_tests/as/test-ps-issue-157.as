package {

    // Issue #157 - Improve support for automatic semi-colon insertion

    public class Program {

        public static function Main():int {

            var s:String = "";
            var hexChars:String = "ABCDEF";
            var n:int = 0, i:int = 0;

            // No semicolon should be inserted after this line..
            s += hexChars.charAt(( n >> ( ( 3 - i ) * 8 + 4 ) ) & 0xF)
            + hexChars.charAt(( n >> ( ( 3 - i ) * 8 ) ) & 0xF);

            return 0;
        }

    }

}
