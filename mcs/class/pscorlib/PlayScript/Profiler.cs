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
			section.GCCount -= System.GC.CollectionCount(System.GC.MaxGeneration);
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
			section.GCCount += System.GC.CollectionCount(System.GC.MaxGeneration);
		}

		public static void Reset()
		{
			if (!Enabled)
				return;

			// reset all sections
			foreach (Section section in sSections.Values) {
				section.TotalTime = new TimeSpan();
				section.Timer.Reset();
				section.Stats.Reset();
				section.History.Clear();
			}

			// reset all counters
			sFrameCount = 0;
		}

		public static void OnFrame()
		{
			if (!Enabled)
				return;

			// update all sections
			foreach (Section section in sSections.Values) {
				section.TotalTime += section.Timer.Elapsed;
				if (sDoReport) 
				{
					// pad with zeros if necessary
					while (section.History.Count < sFrameCount) {
						section.History.Add(new SectionHistory());
					}

					var history = new SectionHistory();
					history.Time = section.Timer.Elapsed;
					history.GCCount = section.GCCount;
					section.History.Add(history);
				}
				section.GCCount = 0;
				section.Timer.Reset();
			}

			sFrameCount++;
			if (!sDoReport) {
				// normal profiling, just print out every so often
				if ((sPrintFrameCount!=0) && (sFrameCount >= sPrintFrameCount)) {
					PrintTimes(System.Console.Out);
					Reset();
				}
			} else {
				// report generation, accumulate a specified number of frames and then print report
				if (sFrameCount >= sReportFrameCount) {
					// print out report
					DoReport();
					Reset();
					sDoReport = false;
				}
			}

			// check start report countdown
			if (sReportStartDelay > 0) {
				if (--sReportStartDelay == 0) {
					System.GC.Collect();

					// reset counters
					Reset();

					// enable the report
					sDoReport = true;

					sReportGCCount = System.GC.CollectionCount(System.GC.MaxGeneration);

					// start global timer
					sReportTime = Stopwatch.StartNew();
				}
			}
		}

		public static void StartSession(string reportName, int frameCount, int reportStartDelay = 5)
		{
			if (!Enabled)
				return;

			sReportName = reportName;
			sReportFrameCount = frameCount;
			sReportStartDelay = reportStartDelay;

			Console.WriteLine("Starting profiling session: {0} frames:{1}", reportName, frameCount);
		}

		
		public static void PrintTimes(TextWriter tw)
		{
			var str = "profiler: ";
			foreach (Section section in sSections.Values) {
				str += section.Name + ":";
				str += (section.TotalTime.TotalMilliseconds / sFrameCount).ToString("0.00");
				str += " ";
			}
			tw.WriteLine(str);
		}

		public static void PrintFullTimes(TextWriter tw)
		{
			foreach (Section section in sSections.Values) {

				var total = section.TotalTime;
				var average = total.TotalMilliseconds / sFrameCount;

				// do we have a history?
				if (section.History.Count > 0) {
					// get history in milliseconds, sorted
					var history = section.History.Select(a => a.Time.TotalMilliseconds).OrderBy(a => a).ToList();
					// get min/max/median
					var minTime = history.First();
					var maxTime = history.Last();
					var medianTime = history[history.Count / 2];
					
					tw.WriteLine("{0,-12} total:{1,6} average:{2,6:0.00}ms min:{3,6:0.00}ms max:{4,6:0.00}ms median:{5,6:0.00}ms ",
					             section.Name,
					             total,
					             average,
					             minTime,
					             maxTime,
					             medianTime
					             );

				} else {

					tw.WriteLine("{0,-12} total:{1,6} average:{2,6:0.00}ms",
				             section.Name,
				             total,
				             average
				             );
				}
			}
		}

		public static void PrintHistory(TextWriter tw)
		{
			tw.Write("{0,4} ", "");
			foreach (Section section in sSections.Values) 
			{
				tw.Write("{0,12}{1} ", section.Name, ' ');

				// pad history with zeros if necessary
				while (section.History.Count < sFrameCount) {
					section.History.Add(new SectionHistory());
				}
			}
			tw.WriteLine();

			tw.WriteLine("---------------------------");
			for (int frame=0; frame < sFrameCount; frame++)
			{
				tw.Write("{0,4}:", frame);
				int gcCount = 0;
				foreach (Section section in sSections.Values) 
				{
					var history = section.History[frame];
					tw.Write("{0,12:0.00}{1} ", history.Time.TotalMilliseconds, (history.GCCount > 0) ? '*' : ' ' );
					gcCount += history.GCCount;
				}
				if (gcCount > 0) {
					tw.Write("    <=== GC occurred");
				}
				tw.WriteLine();
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
			tw.WriteLine("GC Count:      {0}", sReportGCCount);

			tw.WriteLine("*********** Timing (ms) ***********");
			PrintFullTimes(tw);

			tw.WriteLine("***** Dynamic Runtime Stats ******");
			PrintStats(tw);

			tw.WriteLine("************ History *************");
			PrintHistory(tw);

			tw.WriteLine("**********************************");
		}

		private static void DoReport()
		{
			sReportTime.Stop();

			sReportGCCount = System.GC.CollectionCount(System.GC.MaxGeneration) - sReportGCCount;


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

		class SectionHistory
		{
			public TimeSpan Time;
			public int 		GCCount;
		};

		// info for a single section
		class Section
		{
			public string               Name;
			public Stopwatch 			Timer = new Stopwatch();
			public TimeSpan				TotalTime;
			public List<SectionHistory>	History = new List<SectionHistory>();
			public Stats 				Stats = new PlayScript.Stats();	
			public int 					GCCount;
		};

		private static Dictionary<string, Section> sSections = new Dictionary<string, Section>();
		private static int sFrameCount  = 0;

		// the frequency to print profiiling info
		private static int sPrintFrameCount  = 60;

		// report handling
		private static bool sDoReport = false;
		private static int  sReportStartDelay = 0;
		private static int  sReportFrameCount = 0;
		private static string sReportName;
		private static Stopwatch sReportTime;
		private static int  sReportGCCount;
		#endregion


	}
}

