// List of features that are enabled / disabled to save memory in the runtime structures.
//#define ENABLE_GC_COUNTS
//#define ENABLE_USED_MEM


using System;
using System.Drawing;
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
using Android.Views;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Runtime;
using Android.OS;
#endif

namespace PlayScript
{
	public static class Profiler
	{
 		public static bool Enabled = true;
		public static bool ProfileGPU = false;
		public static bool ProfileMemory = false;			// Profile memory usage, it is very slow though...
		public static bool ProfileLoading = false;			// set to true to profile loading
		public static string LoadingEndMilestone = "interactive";
		public static bool EmitSlowFrames = false;			// emit slow frame sections
		public static bool DisableTraces = true;			// set to true to disable traces during profiling session
		public static long LastTelemetryFrameSpanStart = long.MaxValue;
		public static Dictionary<string, string> SessionData = new Dictionary<string, string>(); // additional profiling session data to be printed in reports

		public static bool FrameSkippingEnabled = false;
		public static int  NextFramesElapsed = 1;
		public static int  MaxNumberOfFramesElapsed = 0;
		private static bool sHasProfiledLoading = false;		// set to true after loading has been profiled
		private static string sFilterPrefix;

		private static SectionHistory ZeroSectionHistory = new SectionHistory();

		// if telemetryName is provided then it will be used for the name sent to telemetry when this section is entered
		public static string Begin(string name, string telemetryName = null)
		{
			if (!Enabled || filter(name))
				return name;

			Section section;
			if (!sSections.TryGetValue(name, out section)) {
				section = new Section();
				section.Name = name;
				if (telemetryName != null) {
					if (telemetryName != "") {
						// use provided telemetry name
						section.Span = new Telemetry.Span(telemetryName);
					}
				} else if (name != "swap" && name != "frame") {
					// use section name for telemetry data
					section.Span = new Telemetry.Span(name);
				}
				sSections[name] = section;
				// keep ordered list of sections
				sSectionList.Add(section);
			}

			section.Stats.Subtract(PlayScript.Stats.CurrentInstance);
#if ENABLE_GC_COUNTS
			for (int i = sGCMinGeneration ; i < sGCMaxGeneration ; ++i) {
				section.GCCounts[i] -= System.GC.CollectionCount(i);
			}
#endif
#if ENABLE_USED_MEM
			if (ProfileMemory) {
				section.CurrentUsedMemory -= mono_gc_get_used_size();
			}
#endif
			if (section.Span != null)
				section.Span.Begin();
			section.Timer.Start();
			return name;
		}
		
		public static void End(string name)
		{
			if (!Enabled || filter(name))
				return;

			Section section;
			if (!sSections.TryGetValue(name, out section)) {
				return;
			}

			section.Timer.Stop();
			section.NumberOfCalls += 1;
			if (section.Span != null)
				section.Span.End();
			section.Stats.Add(PlayScript.Stats.CurrentInstance);
#if ENABLE_GC_COUNTS
			for (int i = sGCMinGeneration ; i < sGCMaxGeneration ; ++i) {
				section.GCCounts[i] += System.GC.CollectionCount(i);
			}
#endif
#if ENABLE_USED_MEM
			if (ProfileMemory) {
				section.CurrentUsedMemory += mono_gc_get_used_size();
				if (section.CurrentUsedMemory > 0) {
					section.UsedMemory += section.CurrentUsedMemory;	// After a GC the size might actually be negative
																		// In that case, we actually can't measure the memory cost
				}
			}
			section.CurrentUsedMemory = 0;
#endif
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
			MaxNumberOfFramesElapsed = 0;
			NextFramesElapsed = 1;
		}

		// this should be called at the beginning of a frame
		public static void OnBeginFrame()
		{
			if (!Enabled)
				return;

			if (ProfileLoading && !sHasProfiledLoading) {
				// begin session for loading
				StartSession("Loading", int.MaxValue, 0);
				sHasProfiledLoading = true;
			}

			LastTelemetryFrameSpanStart = Telemetry.Session.BeginSpan();
			Profiler.Begin("frame");
		}

		// this should be called at the end of a frame
		public static void OnEndFrame()
		{
			if (!Enabled)
				return;

			if (EmitSlowFrames && LastTelemetryFrameSpanStart != long.MaxValue)
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

			sFrameCount += NextFramesElapsed;
			MaxNumberOfFramesElapsed = Math.Max(MaxNumberOfFramesElapsed, NextFramesElapsed);

			// update all sections
			foreach (Section section in sSectionList) {
				section.TotalTime += section.Timer.Elapsed;
				if (sDoReport) 
				{
					// pad with zeros if necessary
					while (section.History.Count < sFrameCount) {
						section.History.Add(ZeroSectionHistory);
					}

					var history = new SectionHistory();
					history.Time = section.Timer.Elapsed;
					history.NumberOfCalls = section.NumberOfCalls;
#if ENABLE_GC_COUNTS
					for (int i = sGCMinGeneration ; i < Profiler.sGCMaxGeneration ; ++i) {
						history.GCCounts[i] = section.GCCounts[i];
					}
#endif
					section.History.Add(history);
				}
#if ENABLE_GC_COUNTS
				for (int i = sGCMinGeneration ; i < Profiler.sGCMaxGeneration ; ++i) {
					section.GCCounts[i] = 0;
				}
#endif
				section.Timer.Reset();
				section.NumberOfCalls = 0;
			}

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
		}

		/// <summary>
		/// This should be invoked when a loading milestone has occurred
		/// </summary>
		public static void LoadMilestone(string name)
		{
			if (Enabled) {
				Console.WriteLine("Loading milestone {0} {1}", name, sGlobalTimer.Elapsed);

				// end profiling session for loading
				if ((LoadingEndMilestone!=null) && name.Contains(LoadingEndMilestone) && sReportName == "Loading") {
					EndSession();
				}
			}

			// store load complete time
			sLoadMilestones[name] = sGlobalTimer.Elapsed;
		}

		public static void StartSession(string reportName, int frameCount, int reportStartDelay = 5, string filterPrefix = null)
		{
			if (!Enabled)
				return;

			sReportName = reportName;
			sReportFrameCount = frameCount;
			sReportStartDelay = reportStartDelay;
			MaxNumberOfFramesElapsed = 0;
			NextFramesElapsed = 1;
			sFilterPrefix = filterPrefix;

			Console.WriteLine("Starting profiling session: {0} frames:{1} frameDelay:", reportName, frameCount, reportStartDelay);

			if (sReportStartDelay == 0) {
				// start report immediately
				OnStartReport();
			}

		}

		public static void EndSession()
		{
			sReportFrameCount = sFrameCount;
		}

		private static bool filter(string name)
		{
			return sFilterPrefix != null && !name.StartsWith (sFilterPrefix);
		}

		
		public static void PrintTimes(TextWriter tw)
		{
			temporaryStringBuilder.Length = 0;
			temporaryStringBuilder.Append("profiler: ");
			foreach (Section section in sSectionList) {
				if (filter (section.Name))
					continue;
				double timeToDisplay = section.TotalTime.TotalMilliseconds / sFrameCount;
				if (timeToDisplay < 0.01) {
					continue;
				}
				temporaryStringBuilder.Append(section.Name);
				temporaryStringBuilder.Append(':');
				temporaryStringBuilder.AppendFormat("{0:0.00}", timeToDisplay);
				temporaryStringBuilder.Append(' ');
			}
			var tmpString = temporaryStringBuilder.ToString ();
			tw.WriteLine(tmpString);
			//TODO : Create a 'real' redirect textwriter class so we can:
	//	consoleTextWriter = Console.Out;
	//	this.OnWrite += delegate(string text) { consoleTextWriter.Write(text); };
	//	Console.SetOut(this);
			Telemetry.Session.WriteTrace(tmpString);

		}

		public static void PrintFullTimes(TextWriter tw)
		{
			foreach (Section section in sSectionList.OrderBy(a => -a.TotalTime)) {
				if (filter (section.Name))
					continue;
				var total = section.TotalTime;
				if (total.TotalMilliseconds < 0.01) {
					// Skip negligible times
					section.Skipped = true;
					continue;
				}

				var callCount = section.History.Select(a=>a.NumberOfCalls).Sum();
				if (callCount == 0) {
					// Skip no calls
					section.Skipped = true;
					continue;
				}

				var average = total.TotalMilliseconds / sFrameCount;
				var averagePerCall = total.TotalMilliseconds / callCount;

				// get history in milliseconds, sorted
				var history = section.History.Where(a=>a.NumberOfCalls > 0).Select(a => a.Time.TotalMilliseconds).OrderBy(a => a).ToList();

				long usedMem = 0;
#if ENABLE_USED_MEM
				usedMem = section.UsedMemory / 1024;
#endif

				// do we have a history?
				if (history.Count > 0) {
					// get min/max/median
					var minTime = history.First();
					var maxTime = history.Last();
					var medianTime = history[history.Count / 2];

					// Gather all the GCCounts from the history
					int GC0 = 0, GC1 = 0;

#if ENABLE_GC_COUNTS
					for (int i = sGCMinGeneration ; i < sGCMaxGeneration ; ++i)
					{
						section.GCCounts[i] = section.History.Sum(a => a.GCCounts[i]);
					}

					GC0 = section.GCCounts[sGCMinGeneration];
					GC1 = section.GCCounts[sGCMaxGeneration-1]
#endif

					tw.WriteLine("{0,-40} total:{1,6} average:{2,6:0.00}ms average/call:{3,6:0.00}ms min:{4,6:0.00}ms max:{5,6:0.00}ms median:{6,6:0.00}ms #GC0: {7} #GC1: {8} UsedMem: {9}Kb",
					             section.Name,
					             total,
					             average,
					             averagePerCall,
					             minTime,
					             maxTime,
					             medianTime,
					             GC0,
					             GC1,
					             usedMem
					             );
				} else {

					tw.WriteLine("{0,-40} total:{1,6} average/frame:{2,6:0.00}ms average/call:{3,6:0.00}ms UsedMem: {4}Kb",
				             section.Name,
				             total,
				             average,
				             averagePerCall,
					         usedMem
				             );
				}
			}
		}

		public static void PrintHistory(TextWriter tw)
		{
			tw.Write("{0,4} ", "");

			var sortedSections = sSectionList.OrderBy(a => -a.TotalTime);
			char c = 'a';	// Shorthand so we can reference numbers more easily
			foreach (Section section in sortedSections)
			{
				if (section.Skipped || filter(section.Name)) {
					continue;
				}

				tw.Write("{0,12}{1}{2} ", section.Name, ' ', c);
				++c;

				// pad history with zeros if necessary
				while (section.History.Count < sFrameCount) {
					section.History.Add(new SectionHistory());
				}
			}
			tw.WriteLine();

			tw.WriteLine("---------------------------");
			int[] gcCounts = new int[sGCMaxGeneration];
			for (int frame=0; frame < sFrameCount; frame++)
			{
				tw.Write("{0,4}:", frame);
				for (int i = sGCMinGeneration ; i < sGCMaxGeneration ; ++i) {
					gcCounts[i] = 0;
				}

				c = 'a';	// Shorthand so we can reference numbers more easily
				foreach (Section section in sortedSections) 
				{
					if (section.Skipped || filter(section.Name)) {
						continue;
					}

					var history = section.History[frame];
					string collect = "";
#if ENABLE_GC_COUNTS
					for (int i = sGCMinGeneration ; i < sGCMaxGeneration ; ++i) {
						if (history.GCCounts[i] > 0) {
							collect += i.ToString();
						}
						gcCounts[i] += history.GCCounts[i];
					}
#endif
					string numberOfCalls = (history.NumberOfCalls > 1) ? history.NumberOfCalls.ToString() + "x" : "";
					tw.Write("{3,6}{0,6:0.00}{1,3}{2} ", history.Time.TotalMilliseconds, (collect != string.Empty) ? "*" + collect : "", c, numberOfCalls);
					++c;
				}
				if (gcCounts[sGCMaxGeneration - 1] > 0) {
					tw.Write("    <=== GC max occurred");
				}
				tw.WriteLine();
			}
		}

		public static void PrintStats(TextWriter tw)
		{
			foreach (Section section in sSectionList) {
				if (filter (section.Name))
					continue;
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


		private static void PrintHistogram(TextWriter tw, double bucketSize, List<SectionHistory> history, double minRange, double maxRange, double splitThreshold)
		{
			var counts = new List<int>();
			var callCounts = new List<int>();
			foreach (var h in history) {
				var time = h.Time.TotalMilliseconds;
				if (time >= minRange && time <= maxRange) {
					// find bucket
					int i = (int)Math.Floor( (time - minRange) / bucketSize);

					// resize counts
					while (i >= counts.Count) {
						counts.Add (0);
						callCounts.Add (0);
					}

					// increment histogram
					counts[i]++;
					callCounts[i] += h.NumberOfCalls;
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
						tw.WriteLine("{0,4}ms->{1,4}ms {2,5} {3,5} {4,5:0.0}% {5}", 
						             startTime, endTime, counts[i], callCounts[i], percent, new string('=', (int)Math.Ceiling(percent) ) );
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
				if (filter (section.Name))
					continue;

				var history = section.History.Where(h=>h.NumberOfCalls > 0).OrderBy(h => h.Time.TotalMilliseconds).ToList();
				if (history.Count == 0)
					continue;

				// skip if it was called once or less
				var callCount = history.Select(a=>a.NumberOfCalls).Sum();
				if (callCount <= 1)
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

		public static bool GetAverageClampedInMs (string key, double minimumClampedValue, out double value)
		{
			Section section;
			if ((sFrameCount == 0) || (sSections.TryGetValue(key, out section) == false)
			    || (section.History.Count == 0))
			{
				value = 0.0;
				return false;
			}

			double sum = 0;
			for (int frame=0; frame < sFrameCount; frame++)
			{
				var history = section.History[frame];
				double milliseconds = Math.Max(history.Time.TotalMilliseconds, minimumClampedValue);
				sum += milliseconds;
			}
			sum /= sFrameCount;
			value = sum;

			return true;
		}

		public static Dictionary<string, TimeSpan> LoadMilestones
		{
			get { return sLoadMilestones; }
		}

		#region Private
		private static double GetFpsFromMs(double value)
		{
			return (value <= 0.0) ? double.NaN : (1000.0 / value);
		}

		private static void PrintAverageClamped(TextWriter tw, string key, double minimumClampedValue)
		{
			double sum;
			if (GetAverageClampedInMs (key, minimumClampedValue, out sum)) {
				tw.WriteLine ("Avg (clamped):{0,6:0.00}ms - {1, 12:0.0} fps        min {2:0.00}ms",
				              sum, GetFpsFromMs (sum), minimumClampedValue);
			}
		}

		private static void PrintAverageNWorst(TextWriter tw, string key, int NWorst)
		{
			Section section;
			if (sSections.TryGetValue(key, out section) == false)
			{
				return;
			}
			List<double> history = section.History.Select(a => a.Time.TotalMilliseconds).OrderBy(a => a).ToList();
			double sum = 0;
			for (int frame = Math.Max(0, sFrameCount - NWorst) ; frame < sFrameCount ; frame++)
			{
				sum += history[frame];
			}
			sum /= (double)NWorst;
			tw.WriteLine("Avg {2, 3} worst:{0,6:0.00}ms - {1, 12:0.0} fps", sum, GetFpsFromMs(sum), NWorst);
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

		private static void PrintReport(TextWriter tw, bool full)
		{
			tw.WriteLine("******** Profiling report *********");
			tw.WriteLine("ReportName:    {0}", sReportName);

			#if PLATFORM_MONOTOUCH
			tw.WriteLine("Device:        {0}", UIDevice.CurrentDevice.Name);
			tw.WriteLine("Model:         {0}", IOSDeviceHardware.Version.ToString());
			tw.WriteLine("SystemVersion: {0}", UIDevice.CurrentDevice.SystemVersion);
			tw.WriteLine("Screen Size:   {0}", UIScreen.MainScreen.Bounds);
			tw.WriteLine("Screen Scale:  {0}", UIScreen.MainScreen.Scale);
			#elif PLATFORM_MONODROID
			// Note: stock Android does not provide a way to set the device's name,
			// so won't be as meaningful as iOS.
			tw.WriteLine("Device:        {0}", Build.Display);
			tw.WriteLine("Model:         {0}", Build.Model);
			tw.WriteLine("SystemVersion: {0}", Build.VERSION.Release);

			Context ctx = Android.App.Application.Context;
			IWindowManager wm = ctx.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
			Display display = wm.DefaultDisplay;
			DisplayMetrics metrics = new DisplayMetrics();
			display.GetMetrics(metrics);

			tw.WriteLine("Screen Size:   {0}x{1}", metrics.WidthPixels, metrics.HeightPixels);
			tw.WriteLine("Screen Scale:  {0}", metrics.ScaledDensity);
			#endif
			tw.WriteLine("************* Loading *************");
			PrintLoading(tw);

			tw.WriteLine("************* Session *************");

			tw.WriteLine("Total Frames:  {0}", sReportFrameCount);
			tw.WriteLine("Real Frames:   {0}", sFrameCount);
			tw.WriteLine("Total Time:    {0}", sReportTime.Elapsed);
			if (FrameSkippingEnabled)
			{
				tw.WriteLine("Frame Skipping:Yes - Max Frame skipped: {0}", MaxNumberOfFramesElapsed - 1);		// -1 because the first frame is not really skipped.
			}
			else
			{
				tw.WriteLine("Frame Skipping:No");
			}
			PerformanceFrameData performanceFrameData = GetPerformanceFrameData();
			PrintAverageClamped(tw, "frame", performanceFrameData.FastFrame);
			PrintPercentile(tw, "frame", 95);
			PrintPercentageOfFrames(tw, "frame", "% Fast Frames", a => (a <= performanceFrameData.FastFrame), string.Format("<={0:0.00}ms", performanceFrameData.FastFrame));
			PrintPercentageOfFrames(tw, "frame", "% Slow frames", a => (a >= performanceFrameData.SlowFrame), string.Format(">={0:0.00}ms", performanceFrameData.SlowFrame));
			PrintAverageNWorst(tw, "frame", 10);
#if ENABLE_GC_COUNTS
			for (int i = sGCMinGeneration ; i < sGCMaxGeneration ; ++i)
			{
				tw.WriteLine("GC {0} Count:      {1}", i, sReportGCCounts[i]);
			}
#endif

			if (SessionData != null) {
				tw.WriteLine("********* Session Settings ********");
				// write extra data about session
				foreach (var kvp in SessionData) {
					tw.WriteLine("{0}:  {1}", kvp.Key, kvp.Value);
				}
			}

			tw.WriteLine("*********** Timing (ms) ***********");
			PrintFullTimes(tw);

			tw.WriteLine("*********** Histogram ************");
			PrintHistograms(tw);

			tw.WriteLine("***** Dynamic Runtime Stats ******");
			PrintStats(tw);

			if (full)
			{
				tw.WriteLine("************ History *************");
				PrintHistory(tw);
			}

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

#if ENABLE_GC_COUNTS
			for (int i = sGCMinGeneration ; i < sGCMaxGeneration ; ++i)
			{
				sReportGCCounts[i] = System.GC.CollectionCount(i);
			}
#endif

			// start global timer
			sReportTime = Stopwatch.StartNew();

			if (DisableTraces) {
				// disable traces while profiling
				_root.TraceConfig.enable = false;
			}
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
		/// This is called when a profiling report is ended
		/// </summary>
		private static void OnEndReport()
		{
			// re-enable traces after profiling
			_root.TraceConfig.enable = true;

			sReportTime.Stop();

#if ENABLE_GC_COUNTS
			for (int i = sGCMinGeneration ; i < sGCMaxGeneration ; ++i) {
				sReportGCCounts[i] = System.GC.CollectionCount(i) - sReportGCCounts[i];
			}
#endif

			var recording = Telemetry.Session.EndRecording();
			if (recording != null) {
				// send telemetry data over network
				Telemetry.Session.SendRecordingOverNetwork(recording);
			}

			var profileLogDir = GetProfileLogDir();
			if (profileLogDir != null) {
				string id = DateTime.Now.ToString("u").Replace(' ', '-').Replace(':', '-');
				var path = System.IO.Path.Combine(profileLogDir, "profile-" + id + ".log");
				Console.WriteLine("Writing profiling report to: {0}", path);
				using (var sw = new StreamWriter(path)) {
					PrintReport(sw, true);
				}

				if (recording != null)	{
					// write telemetry data to file
					var telemetryPath = System.IO.Path.Combine(profileLogDir, "telemetry-" + id + ".flm");
					Telemetry.Session.SaveRecordingToFile(recording, telemetryPath);
//					Telemetry.Parser.ParseFile(telemetryPath, telemetryPath + ".txt");
				}
			}

			// print to console (not the full version though)
			PrintReport(System.Console.Out, false);

			InvokeReportDelegate ();
		}

		private static ScreenData GetScreenData ()
		{
#if PLATFORM_MONOTOUCH
			return new ScreenData (UIScreen.MainScreen.Bounds,
			                       UIScreen.MainScreen.Scale);
#elif PLATFORM_MONODROID
			Context ctx = Android.App.Application.Context;
			IWindowManager wm = ctx.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
			Display display = wm.DefaultDisplay;
			DisplayMetrics metrics = new DisplayMetrics();
			display.GetMetrics(metrics);

			return new ScreenData (new RectangleF(0, 0, metrics.WidthPixels, metrics.HeightPixels),
			                       metrics.ScaledDensity);
#else
			return new ScreenData (RectangleF.Empty, 0);
#endif
		}

		public static void InvokeReportDelegate ()
		{
			if (ReporterDelegate == null) {
				return;
			}

			var pd = new ProfilerData();

			pd.ReportName = sReportName;
			pd.Screen = GetScreenData();

			#if PLATFORM_MONOTOUCH
			pd.Device = UIDevice.CurrentDevice.Name;
			pd.Model = IOSDeviceHardware.Version.ToString ();
			pd.SystemVersion = UIDevice.CurrentDevice.SystemVersion;
			#elif PLATFORM_MONODROID
			// Note: stock Android does not provide a way to set the device's name,
			// so won't be as meaningful as iOS.
			pd.Device = Build.Display;
			pd.Model = Build.Model;
			pd.SystemVersion = Build.VERSION.Release;

			#endif

			// loading section
			pd.Loading = new LoadingData (PlayScript.Player.Offline);
			foreach (var milestone in sLoadMilestones) {
				pd.Loading.AddMilestone(milestone.Key, milestone.Value);
			}

			// session section
			pd.Session = new SessionData();
			pd.Session.TotalFrames = sReportFrameCount;
			pd.Session.TotalTime = sReportTime.Elapsed;

			PerformanceFrameData performanceFrameData = GetPerformanceFrameData();

			string key = "frame";
			double minimumClampedValue = performanceFrameData.FastFrame;
			double sum;

			if (GetAverageClampedInMs ("frame", minimumClampedValue, out sum)) {
				pd.Session.AverageFrameRateInMs = (float)sum;
				pd.Session.AverageFrameRateInFps = (float)GetFpsFromMs (sum);
				pd.Session.AverageMinimumFrameRate = (float)minimumClampedValue;
			}

			Section section;
			if (sSections.TryGetValue(key, out section) == true) {
				List<double> history = section.History.Select(a => a.Time.TotalMilliseconds).OrderBy(a => a).ToList();
				int percentile = 95;
				int index = (history.Count * percentile) / 100;
				double pTime = history[index];

				pd.Session.NinetyFivePercentInMiliseconds = pTime;
				pd.Session.NinetyFivePercentInFramesPerSecond = GetFpsFromMs (pTime);
			}

			// TODO: convert this to a method. See PrintPercentageOfFrames
			if (sSections.TryGetValue(key, out section) == true) {
				int matchingFrames = 0;
				for (int frame = 0 ; frame < sFrameCount ; frame++)
				{
					var history = section.History[frame];
					if (history.Time.TotalMilliseconds <= performanceFrameData.FastFrame)
					{
						matchingFrames++;
					}
				}

				pd.Session.FastFramePercentage = (double)(100.0 * matchingFrames) / (double)sFrameCount;
			}

			// TODO: convert this to a method. See PrintPercentageOfFrames
			if (sSections.TryGetValue(key, out section) == true) {
				int matchingFrames = 0;
				for (int frame = 0 ; frame < sFrameCount ; frame++)
				{
					var history = section.History[frame];
					if (history.Time.TotalMilliseconds >= performanceFrameData.SlowFrame)
					{
						matchingFrames++;
					}
				}

				pd.Session.SlowFramePercentage = (double)(100.0 * matchingFrames) / (double)sFrameCount;
			}

			// TODO: missing in ProfilerData
//			PrintAverageNWorst(tw, "frame", 10);

#if ENABLE_GC_COUNT
			// TODO: use a loop instead. Search for sGCMinGeneration and sGCMaxGeneration
			pd.Session.GarbageCollection0Count = sReportGCCounts[0];
			pd.Session.GarbageCollection1Count = sReportGCCounts[1];
#endif

			var t = new TimingData();
			t.Name = "frame";
			t.TotalTime = TimeSpan.Parse("00:01:58.8786507");
			t.Average = 36.58;
			t.Minimum = 9.37;
			t.Maximum = 915.04;
			t.Median = 30.64;
			t.GarbageCollection0Count = 453;
			t.UsedMemoryInKiloBytes = 0;
			pd.Timings.Add(t);

			t = new TimingData();
			t.Name = "enterFrame";
			t.TotalTime = TimeSpan.Parse("00:01:43.6920595");
			t.Average = 31.91;
			t.Minimum = 7.45;
			t.Maximum = 912.19;
			t.Median = 25.52;
			t.GarbageCollection0Count = 431;
			t.UsedMemoryInKiloBytes = 0;
			pd.Timings.Add(t);


			ReporterDelegate (pd);
		}

		public static PerformanceFrameData GetPerformanceFrameData()
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

		public delegate void ReportDelegate (ProfilerData report);
		public static ReportDelegate ReporterDelegate { get; set; }

		public class SectionHistory
		{
			public TimeSpan Time;
			public int		NumberOfCalls;
#if ENABLE_GC_COUNTS
			public int[]	GCCounts = new int[Profiler.sGCMaxGeneration];
#endif
		};

		// info for a single section
		public class Section
		{
			public string               Name;
			public Stopwatch 			Timer = new Stopwatch();
			public TimeSpan				TotalTime;
			public List<SectionHistory>	History = new List<SectionHistory>();
			public Stats 				Stats = new PlayScript.Stats();	
#if ENABLE_GC_COUNTS
			public int[]				GCCounts = new int[Profiler.sGCMaxGeneration];
#endif
#if ENABLE_USED_MEM
			public Int64				UsedMemory;
			public Int64				CurrentUsedMemory;
#endif
			public Telemetry.Span		Span;
			public bool					Skipped;
			public int					NumberOfCalls;
		};

		private static Stopwatch sGlobalTimer = Stopwatch.StartNew();
		private static Dictionary<string, TimeSpan> sLoadMilestones = new Dictionary<string, TimeSpan>();

		private static Dictionary<string, Section> sSections = new Dictionary<string, Section>();
		// ordered list of sections
		private static List<Section> sSectionList = new List<Section>();
		private static int sFrameCount  = 0;

		// the frequency to print profiiling info
		private static int sPrintFrameCount  = 60;

		// report handling
		private static bool sDoReport = false;
		private static int  sReportStartDelay = 0;
		private static int  sReportFrameCount = 0;
		private static string sReportName;
		private static Stopwatch sReportTime;
		private static StringBuilder temporaryStringBuilder = new StringBuilder();
		private const int sGCMinGeneration = 0;
		private readonly static int sGCMaxGeneration = System.GC.MaxGeneration + 1;		// +1 as it seems that's System.GC.MaxGeneration is the index to get oldest collections of the oldest generation
#if ENABLE_GC_COUNT
		private static int[] sReportGCCounts = new int[sGCMaxGeneration];
#endif

#if ENABLE_USED_MEM
		[DllImport ("__Internal", EntryPoint="mono_gc_get_used_size")]
		extern static Int64 mono_gc_get_used_size ();
#endif

		#endregion

		public class PerformanceFrameData
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
#if PLATFORM_MONOTOUCH
		private static PerformanceFrameData sSlowPerformanceFrameData = new PerformanceFrameData(fastFrame: 1000.0/60.0, slowFrame: 1000.0/10.0, autoProfileFrame: 1000.0/20.0);
#endif
	}
}

