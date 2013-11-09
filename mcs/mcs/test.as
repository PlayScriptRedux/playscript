package
{
	import playscript.utils.*;
	import System.Diagnostics.Stopwatch;

	public class Test
	{
		private static var _o:Object = null;
		private static var _a:Object = null;
		private static var _i:int = 0;
		private static var _n:Number = 0.0;
		private static var _s:String;
		private static var _b:Boolean;
		private static var _e:Object = null;

		public static function Main():int
		{
			var i:int;

			var currentProcess:System.Diagnostics.Process  = System.Diagnostics.Process.GetCurrentProcess();

			var sw:Stopwatch = new Stopwatch();

//			BinJSON.numberSize = NumberSize.Float64;

			var testJsonStr:String = "{ \"testString\":\"Now is the time..\", \"testInt\":100, \"testArray\":[\"foo\", \"bar\", -12345, 1234.56, true, false, null, [100, 200, 300] ], \"testObj\":{ \"a\":100, \"b\":{ \"foo\":true, \"bar\":false }, \"c\":[], \"d\":{} }, \"testFloat\":234.123, \"testTrue\":true, \"testFalse\":false, \"testNull\":null }";

			var o2:Object = JSON.parse(testJsonStr);
			var o:Object = BinJSON.parse(testJsonStr);

			sw.Reset();
			sw.Start();
			for (i = 0; i < 1000000; i++) {
				for (var key:String in o) {
					switch (key) {
						case "testString": _s = o[key]; break;
						case "testInt": _i = o[key]; break;
						case "testArray": _a = o[key]; break;
						case "testObj": _o = o[key]; break;
						case "testFloat": _n = o[key]; break;
						case "testTrue": _b = o[key]; break;
						case "testFalse": _b = o[key]; break;
						case "testNull": _e = o[key]; break;
					}
				}
			}
			sw.Stop();
			trace("Iter key bin " + sw.ElapsedMilliseconds);

			sw.Reset();
			sw.Start();
			for (i = 0; i < 1000000; i++) {
				for (var key:String in o) {
					switch (key) {
						case "testString": _s = o2[key]; break;
						case "testInt": _i = o2[key]; break;
						case "testArray": _a = o2[key]; break;
						case "testObj": _o = o2[key]; break;
						case "testFloat": _n = o2[key]; break;
						case "testTrue": _b = o2[key]; break;
						case "testFalse": _b = o2[key]; break;
						case "testNull": _e = o2[key]; break;
					}
				}
			}
			sw.Stop();
			trace("Iter key expando " + sw.ElapsedMilliseconds);

			sw.Reset();
			sw.Start();
			for (i = 0; i < 100000; i++) {
				_i = o.testInt;
				_n = o.testFloat;
				_s = o.testString;
			}
			sw.Stop();
			trace("Get field bin " + sw.ElapsedMilliseconds);

			sw.Reset();
			sw.Start();
			for (i = 0; i < 100000; i++) {
				_i = o2.testInt;
				_n = o2.testFloat;
				_s = o2.testString;
			}
			sw.Stop();
			trace("Get field expando " + sw.ElapsedMilliseconds);

			var gameSettingsJson:String = System.IO.File.ReadAllText("/Users/bcooley/projects/play-dj/playscript-mono/mcs/mcs/gameSettings.json");
			trace("gameSettingsJson.Length " + gameSettingsJson.Length);
			var particlesJson:String = System.IO.File.ReadAllText("/Users/bcooley/projects/play-dj/playscript-mono/mcs/mcs/particles.json");
			trace("particlesJson.Length " + particlesJson.Length);

			var gameSettings:Object;
			var gameSettings2:Object;

			var particles:Object;
			var particles2:Object;

			var startBytes:System.Int64 = 0;

			System.GC.Collect();
			startBytes = currentProcess.WorkingSet64;
			sw.Reset();
			sw.Start();
//			for (i = 0; i < 20; i++) {
			gameSettings2 = JSON.parse(gameSettingsJson);
//			}
			sw.Stop();
			System.GC.Collect();
			trace("GameSettings JSON.parse() " + sw.ElapsedMilliseconds + " mem " + (currentProcess.WorkingSet64 - startBytes));

			System.GC.Collect();
			startBytes = currentProcess.WorkingSet64;
			sw.Reset();
			sw.Start();
//			for (i = 0; i < 20; i++) {
			gameSettings = BinJSON.parse(gameSettingsJson);
//			}
			sw.Stop();
			System.GC.Collect();
			trace("GameSettings BinJSON.parse() " + sw.ElapsedMilliseconds + " mem " + (currentProcess.WorkingSet64 - startBytes) + " size " + (gameSettings as BinJsonObject).Document.Size);

			System.GC.Collect();
			startBytes = currentProcess.WorkingSet64;
			sw.Reset();
			sw.Start();
			//			for (i = 0; i < 20; i++) {
			particles2 = JSON.parse(particlesJson);
			//			}
			sw.Stop();
			System.GC.Collect();
			trace("Particles JSON.parse() " + sw.ElapsedMilliseconds + " mem " + (currentProcess.WorkingSet64 - startBytes));

			System.GC.Collect();
			startBytes = currentProcess.WorkingSet64;
			sw.Reset();
			sw.Start();
			//			for (i = 0; i < 20; i++) {
			particles = BinJSON.parse(particlesJson);
			//			}
			sw.Stop();
			System.GC.Collect();
			trace("BinJSON.parse() " + sw.ElapsedMilliseconds + " mem " + (currentProcess.WorkingSet64 - startBytes) + " size " + (particles as BinJsonObject).Document.Size);

			return 0;
		}
	}
}
