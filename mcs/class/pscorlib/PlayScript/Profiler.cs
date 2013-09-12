using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

#if PLATFORM_MONOMAC
using MonoMac.OpenGL;
using MonoMac.Foundation;
#elif PLATFORM_MONOTOUCH
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;
#elif PLATFORM_MONODROID
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;
#endif

namespace PlayScript
{
	public static class Profiler
	{
		public static bool Enabled = true;
		public static bool ProfileGPU = false;
		public static long LastTelemetryFrameSpanStart = long.MaxValue;
		private static StringBuilder temporaryStringBuilder = new StringBuilder();

		// if telemetryName is provided then it will be used for the name sent to telemetry when this section is entered
		public static void Begin(string name, string telemetryName = null)
		{
			if (!Enabled)
				return;

			Section section;
			if (!sSections.TryGetValue(name, out section)) {
				section = new Section();
				section.Name = name;
				if (telemetryName != null) {
					// use provided telemetry name
					section.Span = new Telemetry.Span(telemetryName);
				} else if (name != "swap" && name != "frame") {
					// use section name for telemetry data
					section.Span = new Telemetry.Span(name);
				}
				sSections[name] = section;
				// keep ordered list of sections
				sSectionList.Add(section);
			}

			section.Stats.Subtract(PlayScript.Stats.CurrentInstance);
			section.GCCount -= System.GC.CollectionCount(System.GC.MaxGeneration);
			if (section.Span != null)
				section.Span.Begin();
			section.Timer.Start();
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
			if (section.Span != null)
				section.Span.End();
			section.Stats.Add(PlayScript.Stats.CurrentInstance);
			section.GCCount += System.GC.CollectionCount(System.GC.MaxGeneration);
		}

		public static void Reset()
		{
			if (!Enabled)
				return;

			// reset all sections
			foreach (Section section in sSectionList) {
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

			if (LastTelemetryFrameSpanStart != long.MaxValue)
			{
				long endFrameTime = Stopwatch.GetTimestamp();
				long spanTimeInTicks = endFrameTime - LastTelemetryFrameSpanStart;
				// From ticks to ns
				double spanTimeInNs = (double)spanTimeInTicks * (1000000000.0 / (double)Stopwatch.Frequency);
				PerformanceFrameData frameData = GetPerformanceFrameData();
				double autoProfileFrameInNs = frameData.AutoProfileFrame * 1000000.0;		// Convert ms to ns
				if (spanTimeInNs >= autoProfileFrameInNs) {
					Telemetry.Session.EndSpan("SlowFrame", LastTelemetryFrameSpanStart);
				}
			}
			Profiler.End("frame");

#if PLATFORM_MONOMAC || PLATFORM_MONOTOUCH || PLATFORM_MONODROID
			if (ProfileGPU) {
				// this stalls and waits for the gpu to finish 
				PlayScript.Profiler.Begin("gpu", ".rend.gl.swap");
				GL.Finish();
				PlayScript.Profiler.End("gpu");
			}
#endif

			// update all sections
			foreach (Section section in sSectionList) {
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
					OnEndReport();
					Reset();
					sDoReport = false;
				}
			}

			// check start report countdown
			if (sReportStartDelay > 0) {
				if (--sReportStartDelay == 0) {
					OnStartReport();
				}
			}
		
			LastTelemetryFrameSpanStart = Telemetry.Session.BeginSpan();
			Profiler.Begin("frame");
		}

		/// <summary>
		/// This should be invoked when a loading milestone has occurred
		/// </summary>
		public static void LoadMilestone(string name)
		{
			// store load complete time
			sLoadMilestones[name] = sGlobalTimer.Elapsed;
		}

		public static void StartSession(string reportName, int frameCount, int reportStartDelay = 5)
		{
			if (!Enabled)
				return;

			sReportName = reportName;
			sReportFrameCount = frameCount;
			sReportStartDelay = reportStartDelay;

			Console.WriteLine("Starting profiling session: {0} frames:{1} frameDelay:", reportName, frameCount, reportStartDelay);
		}

		
		public static void PrintTimes(TextWriter tw)
		{
			temporaryStringBuilder.Length = 0;
			temporaryStringBuilder.Append("profiler: ");
			foreach (Section section in sSectionList) {
				double timeToDisplay = section.TotalTime.TotalMilliseconds / sFrameCount;
				if (timeToDisplay < 0.01) {
					continue;
				}
				temporaryStringBuilder.Append(section.Name);
				temporaryStringBuilder.Append(':');
				temporaryStringBuilder.AppendFormat("{0:0.00}", timeToDisplay);
				temporaryStringBuilder.Append(' ');
			}
			tw.WriteLine(temporaryStringBuilder.ToString());
		}

		public static void PrintFullTimes(TextWriter tw)
		{
			foreach (Section section in sSectionList.OrderBy(a => -a.TotalTime)) {

				var total = section.TotalTime;
				if (total.TotalMilliseconds < 0.01) {
					// Skip negligible times
					continue;
				}

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
			foreach (Section section in sSectionList) 
			{
				tw.Write("{0,12}{1} ", section.Name, ' ');

				// pad history with zeros if necessary
				while (section.History.Count < sFrameCount) {
					section.History.Add(new SectionHistory());
				}
			}
			tw.WriteLine();

			tw.WriteLine("---------------------------");
			int numberOfFramesToPrint = Math.Min(sFrameCount, MaxNumberOfFramesPrintedInHistory);
			for (int frame=0; frame < numberOfFramesToPrint; frame++)
			{
				tw.Write("{0,4}:", frame);
				int gcCount = 0;
				foreach (Section section in sSectionList) 
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
			foreach (Section section in sSectionList) {
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

		public static void PrintLoading(TextWriter tw)
		{
			tw.WriteLine("Offline Mode:  {0}", PlayScript.Player.Offline);
			foreach (var milestone in sLoadMilestones) {
				tw.WriteLine(" {0,-20} {1}", milestone.Key, milestone.Value);
			}
		}


		private static void PrintHistogram(TextWriter tw, double bucketSize, List<double> history, double minRange, double maxRange, double splitThreshold)
		{
			var counts = new List<int>();
			foreach (var time in history) {
				if (time >= minRange && time <= maxRange) {
					// find bucket
					int i = (int)Math.Floor( (time - minRange) / bucketSize);

					// resize counts
					while (i >= counts.Count)
						counts.Add(0);

					// increment histogram
					counts[i]++;
				}
			}

			// print
			double startTime = minRange;
			double endTime = startTime + bucketSize;
			for (int i=0; i < counts.Count; i++) {
				if (counts[i] > 0)
				{
					double percent = (counts[i] * 100.0 / history.Count);
					if ((percent <= splitThreshold) || (bucketSize <= 0.1))
					{
						// print counts for this range
						tw.WriteLine("{0,4}ms->{1,4}ms {2,5} {3,5:0.0}% {4}", 
						             startTime, endTime, counts[i], percent, new string('=', (int)Math.Ceiling(percent) ) );
					}
					else
					{
						// split histogram here if this range has too many data points
						PrintHistogram(tw, bucketSize / 10, history, startTime, endTime, splitThreshold);
					}
				}

				// next range
				startTime += bucketSize;
				endTime   += bucketSize;
			}
		}

		public static void PrintHistograms(TextWriter tw)
		{
			foreach (var section in sSectionList)
			{
				var history = section.History.Select(h => h.Time.TotalMilliseconds).OrderBy(h=>h).ToList();
				if (history.Count == 0)
					continue;

				tw.WriteLine(" --- {0} ---", section.Name);

				if (section.Name == "frame") {
					// hardcode this for "frame"
					double mid = 17 * 2;
					PrintHistogram(tw,  1.0, history, 0.0, mid, 100.0);
					PrintHistogram(tw, 10.0, history, mid, double.MaxValue, 40.0);
				} else {
					PrintHistogram(tw, 10.0, history, 0.0, double.MaxValue, 40.0);
				}
			}
		}

		#region Private
		private static double GetFpsFromMs(double value)
		{
			return (value <= 0.0) ? double.NaN : (1000.0 / value);
		}

		private static void PrintAverageClamped(TextWriter tw, string key, double minimumClampedValue)
		{
			Section section;
			if (sSections.TryGetValue(key, out section) == false)
			{
				return;
			}
			double sum = 0;
			for (int frame=0; frame < sFrameCount; frame++)
			{
				var history = section.History[frame];
				double milliseconds = Math.Max(history.Time.TotalMilliseconds, minimumClampedValue);
				sum += milliseconds;
			}
			sum /= sFrameCount;
			tw.WriteLine("Avg (clamped):{0,6:0.00}ms - {1, 12:0.0} fps        min {2:0.00}ms", sum, GetFpsFromMs(sum), minimumClampedValue);
		}

		private static void PrintPercentile(TextWriter tw, string key, int percentile)
		{
			Section section;
			if (sSections.TryGetValue(key, out section) == false)
			{
				return;
			}
			List<double> history = section.History.Select(a => a.Time.TotalMilliseconds).OrderBy(a => a).ToList();
			int index = (history.Count * percentile) / 100;
			double pTime = history[index];
			tw.WriteLine("{0, 2}p:          {1,6:0.00}ms - {2, 12:0.0} fps", percentile, pTime, GetFpsFromMs(pTime));
		}

		private static void PrintPercentageOfFrames(TextWriter tw, string key, string text, Func<double, bool> func, string additionalText)
		{
			Section section;
			if (sSections.TryGetValue(key, out section) == false)
			{
				return;
			}
			int matchingFrames = 0;
			for (int frame = 0 ; frame < sFrameCount ; frame++)
			{
				var history = section.History[frame];
				if (func(history.Time.TotalMilliseconds))
				{
					matchingFrames++;
				}
			}
			tw.WriteLine("{0}: {1,4:0.0}%                             {2}", text, (double)(100.0 * matchingFrames) / (double)sFrameCount, additionalText);
		}

		private static void PrintReport(TextWriter tw)
		{
			tw.WriteLine("******** Profiling report *********");
			tw.WriteLine("ReportName:    {0}", sReportName);

			#if PLATFORM_MONOTOUCH
			tw.WriteLine("Device:        {0}", UIDevice.CurrentDevice.Name);
			tw.WriteLine("Model:         {0}", IOSDeviceHardware.Version.ToString());
			tw.WriteLine("SystemVersion: {0}", UIDevice.CurrentDevice.SystemVersion);
			tw.WriteLine("Screen Size:   {0}", UIScreen.MainScreen.Bounds);
			tw.WriteLine("Screen Scale:  {0}", UIScreen.MainScreen.Scale);
			#endif
			tw.WriteLine("************* Loading *************");
			PrintLoading(tw);

			tw.WriteLine("************* Session *************");

			tw.WriteLine("Total Frames:  {0}", sFrameCount);
			tw.WriteLine("Total Time:    {0}", sReportTime.Elapsed);
			PerformanceFrameData performanceFrameData = GetPerformanceFrameData();
			PrintAverageClamped(tw, "frame", performanceFrameData.FastFrame);
			PrintPercentile(tw, "frame", 95);
			PrintPercentageOfFrames(tw, "frame", "% Fast Frames", a => (a <= performanceFrameData.FastFrame), string.Format("<={0:0.00}ms", performanceFrameData.FastFrame));
			PrintPercentageOfFrames(tw, "frame", "% Slow frames", a => (a >= performanceFrameData.SlowFrame), string.Format(">={0:0.00}ms", performanceFrameData.SlowFrame));
			tw.WriteLine("GC Count:      {0}", sReportGCCount);

			tw.WriteLine("*********** Timing (ms) ***********");
			PrintFullTimes(tw);

			tw.WriteLine("*********** Histogram ************");
			PrintHistograms(tw);

			tw.WriteLine("***** Dynamic Runtime Stats ******");
			PrintStats(tw);

			tw.WriteLine("************ History *************");
			PrintHistory(tw);

			tw.WriteLine("**********************************");
		}

		/// <summary>
		/// This is called when a profiling report is started
		/// </summary>
		private static void OnStartReport()
		{
			// begin a telemetry session
			Telemetry.Session.StartRecording();

			// garbage collect so that it does not impact our session
			System.GC.Collect();

			// reset counters
			Reset();

			// enable the report
			sDoReport = true;

			sReportGCCount = System.GC.CollectionCount(System.GC.MaxGeneration);

			// start global timer
			sReportTime = Stopwatch.StartNew();

			// disable traces while profiling
			_root.TraceConfig.enable = false;
		}

		private static string  GetProfileLogDir()
		{
			#if PLATFORM_MONOTOUCH || PLATFORM_MONOMAC
			// dump profile to file
			var dirs = NSSearchPath.GetDirectories(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User, true);
			if (dirs.Length > 0) {
				return dirs[0];
			} 
			#endif
			return null;
		}


		/// <summary>
		/// This is called when a profilng report is ended
		/// </summary>
		private static void OnEndReport()
		{
			// re-enable traces after profiling
			_root.TraceConfig.enable = true;

			sReportTime.Stop();

			sReportGCCount = System.GC.CollectionCount(System.GC.MaxGeneration) - sReportGCCount;

			var recording = Telemetry.Session.EndRecording();
			if (recording != null) {
				// send telemetry data over network
				Telemetry.Session.SendRecordingOverNetwork(recording);
			}

			var profileLogDir = GetProfileLogDir();
			if (profileLogDir != null) {
				string id = DateTime.Now.ToString("u").Replace(' ', '-').Replace(':', '-');
				var path = Path.Combine(profileLogDir, "profile-" + id + ".log");
				Console.WriteLine("Writing profiling report to: {0}", path);
				using (var sw = new StreamWriter(path)) {
					PrintReport(sw);
				}

				if (recording != null)	{
					// write telemetry data to file
					var telemetryPath = Path.Combine(profileLogDir, "telemetry-" + id + ".flm");
					Telemetry.Session.SaveRecordingToFile(recording, telemetryPath);
//					Telemetry.Parser.ParseFile(telemetryPath, telemetryPath + ".txt");
				}
			}

			// print to console 
			PrintReport(System.Console.Out);
		}

		private static PerformanceFrameData GetPerformanceFrameData()
		{
			if (sCurrentPerformanceFrameData == null)
			{
				#if PLATFORM_MONOTOUCH
				switch (IOSDeviceHardware.Version)
				{
					case IOSDeviceHardware.IOSHardware.iPhone:
					case IOSDeviceHardware.IOSHardware.iPhone3G:
					case IOSDeviceHardware.IOSHardware.iPhone3GS:
					case IOSDeviceHardware.IOSHardware.iPhone4:
					case IOSDeviceHardware.IOSHardware.iPhone4RevA:
					case IOSDeviceHardware.IOSHardware.iPhone4CDMA:
					case IOSDeviceHardware.IOSHardware.iPodTouch1G:
					case IOSDeviceHardware.IOSHardware.iPodTouch2G:
					case IOSDeviceHardware.IOSHardware.iPodTouch3G:
					case IOSDeviceHardware.IOSHardware.iPodTouch4G:
					case IOSDeviceHardware.IOSHardware.iPad:
					case IOSDeviceHardware.IOSHardware.iPad3G:
						sCurrentPerformanceFrameData = sSlowPerformanceFrameData;
						break;
					default:
						sCurrentPerformanceFrameData = sDefaultPerformanceFrameData;
						break;
				}
				#else
				sCurrentPerformanceFrameData = sDefaultPerformanceFrameData;
				#endif
			}
			return sCurrentPerformanceFrameData;
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
			public Telemetry.Span		Span;
		};

		private static Stopwatch sGlobalTimer = Stopwatch.StartNew();
		private static Dictionary<string, TimeSpan> sLoadMilestones = new Dictionary<string, TimeSpan>();

		private static Dictionary<string, Section> sSections = new Dictionary<string, Section>();
		// ordered list of sections
		private static List<Section> sSectionList = new List<Section>();
		private static int sFrameCount  = 0;
		public static int MaxNumberOfFramesPrintedInHistory = 1000;

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

		class PerformanceFrameData
		{
			public PerformanceFrameData(double fastFrame, double slowFrame, double autoProfileFrame)
			{
				FastFrame = fastFrame;
				SlowFrame = slowFrame;
				AutoProfileFrame = autoProfileFrame;
			}
			public double	FastFrame;
			public double	SlowFrame;
			public double	AutoProfileFrame;
		}

		private static PerformanceFrameData sCurrentPerformanceFrameData;
		private static PerformanceFrameData sDefaultPerformanceFrameData = new PerformanceFrameData(fastFrame: 1000.0/60.0, slowFrame: 1000.0/15.0, autoProfileFrame: 1000.0/45.0);
		private static PerformanceFrameData sSlowPerformanceFrameData = new PerformanceFrameData(fastFrame: 1000.0/30.0, slowFrame: 1000.0/10.0, autoProfileFrame: 1000.0/20.0);
	}
}

