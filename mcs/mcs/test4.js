// Module: test4.js
// File: test4.as
var com$zynga$zengine$classes;
(function (com$zynga$zengine$classes) {
  var Test = (function () {
    function Test() {
    }
    Test.Main = function() {
      {
        var s = "Hello World!", j = 100, k = 200.0, l = 300;
        {
/tfor (          var q = 0;
; q < 122;           q++;
) {
            trace("Goo!");
          }
        }
        j = 155;
        j = j + k / l;
        j = j + k / l;
        if (s == null) {
          trace("Foo!BarBlah!");
        } else {
          trace("Bar!");
        }
        while (j == 4) {
          trace("blah");
        }
        trace(s + "Blah!");
      }
    };
    return Test;
  })();
  com$zynga$zengine$classes.Test = Test;
})(com$zynga$zengine$classes || (com$zynga$zengine$classes = {});
