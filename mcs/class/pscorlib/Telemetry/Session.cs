using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using Amf;
using System.Net.Sockets;

namespace Telemetry
{
	public static class Session
	{
		public const long 	Frequency = 1000000;
		public const int 	MinTimeSpan = 5;
		public const string Version = "3,2";
		public const int 	Meta = 293228;

		public static bool Connected
		{
			get { return (sOutput != null);}
		}

		public static long BeginSpan()
		{
			return Stopwatch.GetTimestamp();
		}

		public static void EndSpan(Amf3String name, long beginTime)
		{
			if (sOutput == null) return;

			// compute delta and span
			int span;
			int delta = TimeDelta(beginTime, out span);
			if (delta < 0)
				return;

			// write span
			sOutput.WriteObjectHeader(Protocol.Span.ClassDef);
			sOutput.Write(name);
			sOutput.Write(span);
			sOutput.Write(delta);
		}

		public static void EndSpan(string name, long beginTime)
		{
			if (sOutput == null) return;

			// compute delta and span
			int span;
			int delta = TimeDelta(beginTime, out span);
			if (delta < 0)
				return;

			// write span
			sOutput.WriteObjectHeader(Protocol.Span.ClassDef);
			sOutput.Write(name);
			sOutput.Write(span);
			sOutput.Write(delta);
		}

		public static void EndSpanValue(Amf3String name, long beginTime, object value)
		{
			if (sOutput == null) return;

			// compute delta and span
			int span;
			int delta = TimeDelta(beginTime, out span);
			if (delta < 0)
				return;

			// write span
			sOutput.WriteObjectHeader(Protocol.SpanValue.ClassDef);
			sOutput.Write(name);
			sOutput.Write(span);
			sOutput.Write(delta);
			sOutput.Write(value);
		}

		public static void EndSpanValue(string name, long beginTime, object value)
		{
			if (sOutput == null) return;

			// compute delta and span
			int span;
			int delta = TimeDelta(beginTime, out span);
			if (delta < 0)
				return;

			// write span
			sOutput.WriteObjectHeader(Protocol.SpanValue.ClassDef);
			sOutput.Write(name);
			sOutput.Write(span);
			sOutput.Write(delta);
			sOutput.Write(value);
		}

		public static void WriteTime(Amf3String name)
		{
			if (sOutput == null) return;

			// compute delta
			int delta = TimeDelta();

			sOutput.WriteObjectHeader(Protocol.Time.ClassDef);
			sOutput.Write(name);
			sOutput.Write(delta);
		}

		public static void WriteTime(string name)
		{
			if (sOutput == null) return;

			// compute delta
			int delta = TimeDelta();

			sOutput.WriteObjectHeader(Protocol.Time.ClassDef);
			sOutput.Write(name);
			sOutput.Write(delta);
		}

		public static void WriteValue(Amf3String name, int value)
		{
			if (sOutput == null) return;

			sOutput.WriteObjectHeader(Protocol.Value.ClassDef);
			sOutput.Write(name);
			sOutput.Write(value);
		}

		public static void WriteValue(string name, int value)
		{
			if (sOutput == null) return;

			sOutput.WriteObjectHeader(Protocol.Value.ClassDef);
			sOutput.Write(name);
			sOutput.Write(value);
		}

		public static void WriteValue(Amf3String name, string value)
		{
			if (sOutput == null) return;

			sOutput.WriteObjectHeader(Protocol.Value.ClassDef);
			sOutput.Write(name);
			sOutput.Write(value);
		}

		public static void WriteValue(string name, string value)
		{
			if (sOutput == null) return;

			sOutput.WriteObjectHeader(Protocol.Value.ClassDef);
			sOutput.Write(name);
			sOutput.Write(value);
		}

		public static void WriteValue(Amf3String name, object value)
		{
			if (sOutput == null) return;

			sOutput.WriteObjectHeader(Protocol.Value.ClassDef);
			sOutput.Write(name);
			sOutput.Write(value);
		}

		public static void WriteValue(string name, object value)
		{
			if (sOutput == null) return;

			sOutput.WriteObjectHeader(Protocol.Value.ClassDef);
			sOutput.Write(name);
			sOutput.Write(value);
		}

		public static void WriteTrace(string trace)
		{
			WriteValue(sNameTrace, trace);
		}

		public static void WriteSWFStats(string name, int width, int height, int frameRate, int version, int size)
		{
			WriteValue(".swf.name", name);
			WriteValue(".swf.rate", (int)(Frequency / frameRate));
			WriteValue(".swf.vm", 3);
			WriteValue(".swf.width", width);
			WriteValue(".swf.height", height);
			WriteValue(".swf.playerversion", version);
			WriteValue(".swf.size", size);
		}
		
		private static void OnBeginSession()
		{
			var appName = PlayScript.Player.ApplicationClass.Name;
			var swfVersion = 21;
			var swfSize = 4 * 1024 * 1024;

			// write telemetry version
			WriteValue(".tlm.version", Session.Version);
			WriteValue(".tlm.meta", Session.Meta);

			// write player info
			WriteValue(".player.version", "11,8,800,94");
			WriteValue(".player.airversion", "3.8.0.910");
			WriteValue(".player.type", "Air");
			WriteValue(".player.debugger", true); 
			WriteValue(".player.global.date", new _root.Date().getTime());
			WriteValue(".player.instance", 0);
			WriteValue(".player.scriptplayerversion", swfVersion);

			// write platform info
			WriteValue(".platform.capabilities", "&M=Adobe%20Macintosh&R=1680x1050&COL=color&AR=1.0&OS=Mac%20OS%2010.7.4&ARCH=x86&L=en&PR32=t&PR64=t&LS=en;ja;fr;de;es;it;pt;pt-PT;nl;sv;nb;da;fi;ru;pl;zh-Hans;zh-Hant;ko;ar;cs;hu;tr");
			WriteValue(".platform.cpucount", 4);
			WriteValue(".platform.gpu.kind", "opengles2");
			WriteValue(".platform.gpu.vendor", "Imagination Technologies");
			WriteValue(".platform.gpu.renderer", "PowerVR SGX 535");
			WriteValue(".platform.gpu.version", "OpenGL ES 2.0 IMGSGX535-63.24");
			WriteValue(".platform.gpu.shadinglanguageversion", "OpenGL ES GLSL ES 1.0");
			WriteValue(".platform.3d.driverinfo", "OpenGL Vendor=Imagination Technologies Version=OpenGL ES 2.0 IMGSGX535-63.24 Renderer=PowerVR SGX 535 GLSL=OpenGL ES GLSL ES 1.0");

			// write memory stats
			WriteValue(".mem.total", 8 * 1024);
			WriteValue(".mem.used", 4 * 1024);
			WriteValue(".mem.managed", 0);
			WriteValue(".mem.managed.used", 0);
			WriteValue(".mem.telemetry.overhead", 0);

			// write telemetry categories
			WriteValue(".tlm.category.enable",  "3D");
			WriteValue(".tlm.category.enable",  "sampler");
			WriteValue(".tlm.category.disable", "displayobjects");
			WriteValue(".tlm.category.enable",  "alloctraces");
			WriteValue(".tlm.category.disable", "allalloctraces");
			WriteValue(".tlm.category.enable",  "customMetrics");

			WriteValue(".network.loadmovie", "app:/" + appName );
			WriteValue(".rend.display.mode", "auto");

			// SWF startup timestamp
			WriteTime(".swf.start");
			WriteSWFStats(appName, 800, 600, 60, swfVersion, swfSize);
			WriteMemoryStats();

			// start detailed metrics
			WriteValue(".tlm.detailedMetrics.start", true);

			Flush();
		}

		private static void OnEndSession()
		{
			WriteValue(".tlm.date", new _root.Date().getTime());
			WriteValue(".tlm.optimize.exit3DStandbyModeTime", 0);
			WriteValue(".tlm.optimize.selectionEnd", false);
			WriteValue(".tlm.optimize.selectionStart", false);
			WriteValue(".tlm.optimize.startedIn3DStandbyMode", false);
			WriteValue(".tlm.optimize.threeDStandbyModeHasExited", false);

			Flush();
		}

		public static void OnBeginFrame()
		{
			// emit a timecode for '.enter'
			WriteTime(sNameEnter);

			// begin a .exit span
			sSpanExit.Begin();

			// swf frame must be written here
			WriteValue(sNameSwfFrame, 0);
		}

		public static void OnEndFrame()
		{
			// end .exit
			sSpanExit.End();

			// 	add any additional telemetry processing here which will be counted as "overhead"
			sSpanTlmDoPlay.Begin();
			sSpanTlmDoPlay.End();

			Flush();
		}

		public static void OnResize(int width, int height)
		{
			var rect = new Telemetry.Protocol.Rect();
			rect.xmin = 0;
			rect.ymin = 0;
			rect.xmax = width;
			rect.ymax = height;
			WriteValue(".player.view.resize", rect);
		}

		public static void WriteMemoryStats()
		{
			// memory stats
			// TODO: figure these out
			WriteValue(".mem.total", 8 * 1024);
			WriteValue(".mem.used", 4 * 1024);
			WriteValue(".mem.managed", 0);
			WriteValue(".mem.managed.used", 0);
			WriteValue(".mem.bitmap", 0);
			WriteValue(".mem.bitmap.display", 0);
			WriteValue(".mem.script", 0);
			WriteValue(".mem.network.shared", 0);
			WriteValue(".mem.telemetry.overhead", 0);
		}

		public static bool Connect(string host = "localhost", int port = 7934, int bufferSize = 256 * 1024)
		{
			if (sOutput != null) {
				// already connected...
				return true;
			}

			try
			{
				// attempt connection
				sClient  = new TcpClient(host, port);
				sBuffer  = new BufferedStream(sClient.GetStream(), bufferSize);
				sOutput  = new Amf3Writer(sBuffer);

				// start session
				OnBeginSession();

				return true;
			}
			catch 
			{
				// error connecting to telemetry server
				// if you get here in the debugger then just continue
				sClient = null;
				sBuffer = null;
				sOutput = null;
				return false;
			}
		}

		public static void Disconnect()
		{
			OnEndSession();

			if (sBuffer!=null)
				sBuffer.Close();
			if (sClient != null)
				sClient.Close();

			sClient = null;
			sBuffer = null;
			sOutput = null;
		}

		public static void Flush()
		{
			if (sBuffer != null) {
				sBuffer.Flush();
			}
		}

		#region Private

		// computes the delta time since the last marker and updates the marker position
		private static int TimeDelta()
		{
			// get current ticks
			long time = Stopwatch.GetTimestamp();

			// get delta since our last marker
			int delta = ((int)(time - sLastMarkerTime)) / sDivisor;

			// update marker
			sLastMarkerTime = time;

			// return delta
			return delta;
		}

		// computes the delta time since the last marker and updates the marker position
		// spanLength will contain the time since beginTime (in microseconds)
		private static int TimeDelta(long beginTime, out int spanLength)
		{
			// get current ticks
			long time = Stopwatch.GetTimestamp();

			// get delta since our last marker
			int delta = ((int)(time - sLastMarkerTime)) / sDivisor;

			// get span length (elapsed time since begin)
			spanLength = ((int)(time - beginTime)) / sDivisor;

			// skip spans that are too short
			if (spanLength < MinTimeSpan) {
				return -1;
			}

			// update marker
			sLastMarkerTime = time;

			// return delta
			return delta;
		}


		private static TcpClient 	  sClient;
		private static BufferedStream sBuffer;
		private static Amf3Writer 	  sOutput;

		private static long 		  sLastMarkerTime = Stopwatch.GetTimestamp();
		private static int  		  sDivisor = (int)(Stopwatch.Frequency / Frequency);

		private static readonly Amf3String     sNameTrace   = new Amf3String(".trace");
		private static readonly Amf3String     sNameSwfFrame  = new Amf3String(".swf.frame");
		private static readonly Amf3String     sNameEnter  = new Amf3String(".enter");
//		private static readonly Span 		   sSpanAsActions = new Span(".as.actions");
		private static readonly Span 		   sSpanExit      = new Span(".exit");
		private static readonly Span 		   sSpanTlmDoPlay = new Span(".tlm.doplay");
		#endregion
	}
}

