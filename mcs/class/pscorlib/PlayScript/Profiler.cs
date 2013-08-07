using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;

#if PLATFORM_MONOTOUCH
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace PlayScript
{
	public static class Profiler
	{
		public static bool Enabled = true;

		public static void Begin(string name)
		{
			if (!Enabled)
				return;

			Section section;
			if (!sSections.TryGetValue(name, out section)) {
				section = new Section();
				section.Name = name;
				sSections[name] = section;
			}

			section.Timer.Start();
			section.Stats.Subtract(PlayScript.Stats.CurrentInstance);
		}
		
		public static void End(string name)
		{
			if (!Enabled)
				return;

			Section section;
			if (!sSections.TryGetValue(name, out section)) {
				return;
			}

			section.Timer.Stop();
			section.Stats.Add(PlayScript.Stats.CurrentInstance);
		}

		public static void Reset()
		{
			if (!Enabled)
				return;

			// reset all sections
			foreach (Section section in sSections.Values) {
				section.Timer.Reset();
				section.Stats.Reset();
			}

			// reset all counters
			sFrameCount = 0;
			sFrameTotal = 60;
			sDoReport   = false;
		}

		public static void OnFrame()
		{
			if (!Enabled)
				return;

			sFrameCount++;
			if (sFrameCount >= sFrameTotal) {
				if (sDoReport) {
					DoReport();
				} else {
					PrintTimes(System.Console.Out);
				}

				Reset();
			}
		}

		public static void StartSession(string reportName, int frameCount)
		{
			if (!Enabled)
				return;

			// reset counters
			Reset();

			// set the number of frames to profile
			sFrameTotal = frameCount;

			// enable the report
			sDoReport = true;
			sReportName = reportName;

			sReportTime = Stopwatch.StartNew();
		}

		
		public static void PrintTimes(TextWriter tw)
		{
			var str = "profiler: ";
			foreach (Section section in sSections.Values) {
				str += section.Name + ":";
				str += (section.Timer.Elapsed.TotalMilliseconds / sFrameCount).ToString("0.00");
				str += " ";
			}
			tw.WriteLine(str);
		}

		public static void PrintFullTimes(TextWriter tw)
		{
			foreach (Section section in sSections.Values) {
				tw.WriteLine("{0,-12} total:{1,6} average:{2,6}ms",
				             section.Name,
				             section.Timer.Elapsed,
				             (section.Timer.Elapsed.TotalMilliseconds / sFrameCount).ToString("0.00")
				             );
			}
		}

		public static void PrintStats(TextWriter tw)
		{
			foreach (Section section in sSections.Values) {
				var dict = section.Stats.ToDictionary(true);
				if (dict.Count > 0) {
					tw.WriteLine("---------- {0} ----------", section.Name);

					// create sorted list of stats results
					var list = dict.ToList();
					list.Sort((a,b) => {return b.Value.CompareTo(a.Value);} );
					foreach (var entry in list) {
						tw.WriteLine(" {0}: {1}", entry.Key, entry.Value);
					}
				}
			}
		}

		#region Private
		private static void PrintReport(TextWriter tw)
		{
			tw.WriteLine("******** Profiling report *********");
			tw.WriteLine("ReportName:    {0}", sReportName);

			#if PLATFORM_MONOTOUCH
			tw.WriteLine("Device:        {0}", UIDevice.CurrentDevice.Name);
			tw.WriteLine("Model:         {0}", UIDevice.CurrentDevice.Model);
			tw.WriteLine("SystemVersion: {0}", UIDevice.CurrentDevice.SystemVersion);
			tw.WriteLine("Screen Size:   {0}", UIScreen.MainScreen.Bounds);
			tw.WriteLine("Screen Scale:  {0}", UIScreen.MainScreen.Scale);
			#endif
			tw.WriteLine("Total Frames:  {0}", sFrameCount);
			tw.WriteLine("Total Time:    {0}", sReportTime.Elapsed);
			tw.WriteLine("Average FPS:   {0}",  ((double)sFrameCount / sReportTime.Elapsed.TotalSeconds).ToString("0.00") );

			tw.WriteLine("*********** Timing (ms) ***********");
			PrintFullTimes(tw);

			tw.WriteLine("***** Dynamic Runtime Stats ******");
			PrintStats(tw);

			tw.WriteLine("**********************************");
		}

		private static void DoReport()
		{
			sReportTime.Stop();

			#if PLATFORM_MONOTOUCH
			// dump profile to file
			var dirs = NSSearchPath.GetDirectories(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User, true);
			if (dirs.Length > 0) {
				string id = DateTime.Now.ToString("u").Replace(' ', '-');
				var path = Path.Combine(dirs[0], "profile-" + id + ".log");
				Console.WriteLine("Writing profiling report to: {0}", path);
				using (var sw = new StreamWriter(path)) {
					PrintReport(sw);
				}
			}
			#endif

			// print to console 
			PrintReport(System.Console.Out);

			// pause forever
//			Console.WriteLine("Pausing...");
//			for (;;) {
//				System.Threading.Thread.Sleep(1000);
//			}
		}

		// info for a single section
		class Section
		{
			public string               Name;
			public Stopwatch 			Timer = new Stopwatch();
			public Stats 				Stats = new PlayScript.Stats();	
		};

		private static Dictionary<string, Section> sSections = new Dictionary<string, Section>();
		private static int sFrameCount  = 0;
		private static int sFrameTotal  = 60;

		private static bool sDoReport = false;
		private static string sReportName;
		private static Stopwatch sReportTime;
		#endregion


	}
}

