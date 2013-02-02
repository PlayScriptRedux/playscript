// Module: test4.js
// File: test4.as
var com$zynga$zengine$classes;
(function (com$zynga$zengine$classes) {
    var Test = (function () {
        function Test() {
            trace("Test");
        }
        Test._field1 = 100;
        Test._field2 = "Zaaa";
        Object.defineProperty(Test, "aaa", {
            get: function() {
                return Test._field1;
            },
            enumerable: true,
            configurable: true
        });
        Test.prototype.f = function() {
            f();
        };
        Test.Main = function() {
            var t = new com$zynga$zengine$classes.Test();
            t.f();
            System.Console.WriteLine("Hello World!");
            var s = "Hello World!", j = 100, k = 200.0, l = 300;
            {
                for (var q = 0; q < 122; q++) {
                    trace("Goo!");
                    continue;
                }
            }
            var a = [cnst, 200, 300];
            a.push(400);
            var o = {"blah":100, "joo":false, "blee":null};
            trace(o.blah);
            j = 155;
            j = j + k / l;
            j = (j + k) / l;
            switch (j) {
                case 0:
                case 1:
                    trace("flah");
                    break;
                case 2:
                    trace("boo");
                    break;
                default:
                    trace("google!");
                    break;
            }
            if (s == null) {
                trace("Foo!BarBlah!");
            } else {
                trace("Bar!");
            }
            while (j == 4) {
                trace("blah");
                break;
            }
            do {
                trace("blah");
            } while (j == 50);
            trace(s + "Blah!");
            return j + 100;
        };
        return Test;
    })();
    com$zynga$zengine$classes.Test = Test;
})(com$zynga$zengine$classes || (com$zynga$zengine$classes = {});
