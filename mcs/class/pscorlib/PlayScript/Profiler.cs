using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace PlayScript
{
	public static class Profiler
	{
		private static Dictionary<string, Stopwatch> sSections = new Dictionary<string, Stopwatch>();
		private static int sFrameCount  = 0;
		
		public static void Begin(string section)
		{
			if (!sSections.ContainsKey(section)) {
				sSections[section] = new System.Diagnostics.Stopwatch();
			}
			sSections[section].Start();
		}
		
		public static void End(string section)
		{
			sSections[section].Stop();
		}
		
		public static void OnFrame()
		{
			sFrameCount++;
			if (sFrameCount > 60) {
				var str = "times: ";
				foreach (string i in sSections.Keys.ToArray()) {
					str += i + ":";
					if (sSections[i] != null) {
						str += (sSections[i].Elapsed.TotalMilliseconds / sFrameCount).ToString("0.00");
					}
					str += " ";
					
					sSections[i].Reset();
				}
				
				Console.WriteLine(str);
				sFrameCount = 0;
			}
		}

	}
}

