package {
    // Test statements.

    public class Test {
        public static function Main():int {

            var b:Boolean = true;

            // Test block

            {
                trace("block");
            }

            {
                var qq:int = 100;
                trace("qq block");
            }

            // These are not blocks - they are object literals.

            var o:Object = {} || {};
            o ||= {};
            o = {} ? {} : {};

            // Test if statement

            if (b) {
                trace("true");
            }
            if (b) {
                trace("true");
            }

            if (!b) {
                trace("false");
            } else {
                trace("true");
            }

            if (!b) {
                trace("false");
            } else {
                trace("true");
            }

            // Test for statement

            var i:int = 0;
            for (i = 0; i < 100; i++) {
                trace(i);
                if (i > 50) {
                    continue;
                }
                var b:Boolean = false;
            }
            i = 0;
            for (; ;) {
                i++;
                if (i > 100) {
                    break;
                }
            }

            // Test for in

            var v:Vector.<int> = new Vector.<int>();
            for (i in v) {
                trace(i);
            }
            for (var ii:int in v) {
                trace(i);
            }

            // Test for each in

            var o:Object = new Object();
            var o2:Object;
            for each (o2 in o) {
                trace(o2);
            }
            for each (var o3:Object in o) {
                trace(o3);
            }

            // Test while

            while (b) {
                trace(b);
            }

            // Test do while

            do {
                trace(b);
            } while (b);

            // Test switch

            switch (i) {
                // Should generate empty switch block warning
            }

            switch (i) {
                case 0:			// AS allows fall throughs.
                    trace(0);
                case 1:
                    trace(1);
                default:
                    trace(2);
            }

            switch (i) { // Should generate empty switch block warning
                case 1:
                case 2:		// Note that AS allows empty case blocks.
            }

            switch (i) {
                case 1:
                case 2:
                default:  // Note that AS allows empty case blocks.
            }

            switch (i) { // Should generate empty switch block warning
                case 1:
                case 2:
                    trace(1);
                    break;
                case 3:
                case 4:
                    trace(3);
                    break;
                default:
            }

            return 0;

        }

    }

}
