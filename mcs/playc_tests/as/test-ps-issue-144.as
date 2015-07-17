package {

    import flash.display.Bitmap;

    public class Test {

        public function filterPickObject(obj:Bitmap, filter:*):Boolean {
            if (filter as Class && !(obj as filter)) {
                return false;
            } else if (filter as Function && !filter(obj)) {
                return false;
            }
            return true;
        }

        public static function Main():int {
            return 0;
        }

    }

}