using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using Amf;
using System.Net.Sockets;

#if PLATFORM_MONOMAC
using MonoMac.OpenGL;
using MonoMac.AppKit;
#elif PLATFORM_MONOTOUCH
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;
#elif PLATFORM_MONODROID
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;
using StringName = OpenTK.Graphics.ES20.All;
#endif

namespace Telemetry
{
	public static class Session
	{
		// global enable flag for telemetry, if this is false no connections or recordings will be made
		public static bool 	 Enabled = true;

		// categories enable flags 
		public static bool   CategoryEnabled3D = false;
		public static bool   CategoryEnabledCPU = false;
		public static bool   CategoryEnabledSampler = false;
		public static bool   CategoryEnabledTrace = true;
		public static bool   CategoryEnabledAllocTraces = false;
		public static bool   CategoryEnabledAllAllocTraces = false;
		public static bool   CategoryEnabledDisplayObjects = false;
		public static bool   CategoryEnabledCustomMetrics = true;

		// sampling rate (in milliseconds)
		public static int	 SamplerRate = 1;
		// maximum callstack length to capture
		public static int    SamplerMaxCallStackDepth = 256;
		// sampler start delay
		// unfortunately there is some thread contention during startup (GC?) that needs this workaround for now
		public static int    SamplerStartDelay = 0;

		// default hostname used for starting network sessions if none is provided to Connect()
		public static string DefaultHostName = "localhost";

		// default port used for starting sessions
		public static int 	 DefaultPort = 7934;

		// configuration file name
		public const string  ConfigFileName = "telemetry.cfg";

		// frequency for all timing values (microseconds)
		public const int 	Frequency = 1000000;

		// minimum span length that can be written (any spans shorter than this will be discarded)
		public const int 	MinTimeSpan = 500;

		// telemetry version
		public const string Version = "3,2";


		// returns true if a session is active
		public static bool Connected
		{
			get { return (sLog != null);}
		}

		public static long BeginSpan()
		{
			return Stopwatch.GetTimestamp();
		}

		public static void EndSpan(object name, long beginTime)
		{
			if (!Connected) return;

			// get current time (in nano-seconds)
			long time = Stopwatch.GetTimestamp();

			// get span length (in nano-seconds)
			long span = (time - beginTime);

			// skip spans that are too short
			if (span < MinTimeSpan) {
				return;
			}

			// write entry
			sLog.AddEntry(time, span, name, null);
		}

		public static void EndSpanValue(object name, long beginTime, object value)
		{
			if (!Connected) return;

			// get current time (in nano-seconds)
			long time = Stopwatch.GetTimestamp();

			// get span length (in nano-seconds)
			long span = (time - beginTime);

			// skip spans that are too short
			if (span < MinTimeSpan) {
				return;
			}

			// write entry
			sLog.AddEntry(time, span, name, value);
		}

		public static void WriteTime(object name)
		{
			if (!Connected) return;

			// get current time (in nano-seconds)
			long time = Stopwatch.GetTimestamp();

			// write entry
			sLog.AddEntry(time, Log.EntryTime, name, null);
		}

		public static void WriteValue(object name, object value)
		{
			if (!Connected) return;

			// write entry
			sLog.AddEntry(0, Log.EntryValue, name, value);
		}

		// this forces the value object to be serialized now so it can be reused
		public static void WriteValueImmediate(object name, object value)
		{
			if (!Connected) return;

			// write value to log
			sLog.WriteValueImmediate(name, value);
		}

		public static void WriteTrace(string trace)
		{
			if (!Connected) return;
			if (!CategoryEnabledTrace) return;

			long time = Stopwatch.GetTimestamp();
			sLog.AddEntry(time, 0, sNameTrace, trace);
		}

		public static void WriteObjectAlloc(int id, int size, string type)
		{
			if (!Connected) return;
			if (!CategoryEnabledAllocTraces) return;

			var alloc = new Protocol.Memory_objectAllocation();
			alloc.id = id;
			alloc.size = size;
			alloc.stackid = (sMethodMap!=null) ? sMethodMap.GetCallStackId() : 0;  
			alloc.time = sLog.GetTime();
			alloc.type = type; 
			WriteValue(".memory.newObject", alloc);
		}

		public static void WriteSWFStats(string name, int width, int height, int frameRate, int version, int size)
		{
			if (!Connected) return;

			WriteValue(".swf.name", name);
			WriteValue(".swf.rate", (int)(Frequency / frameRate));
			WriteValue(".swf.vm", 3);
			WriteValue(".swf.width", width);
			WriteValue(".swf.height", height);
			WriteValue(".swf.playerversion", version);
			WriteValue(".swf.size", size);
		}

		private static void WriteCategoryEnabled(string category, bool enabled)
		{
			if (!Connected) return;

			WriteValue(enabled ? ".tlm.category.enable" : ".tlm.category.disable", category);
		}
		
		private static void BeginSession(Stream stream, bool autoCloseStream = true)
		{
			// get application name
			var assembly = System.Reflection.Assembly.GetEntryAssembly(); 
			var appName = assembly.GetName().Name;
			var swfVersion = 21;
			var swfSize = 4 * 1024 * 1024;

			if (CategoryEnabledSampler || CategoryEnabledAllocTraces) {
				if (sSymbols == null) {
					// allocate symbol table for method map
					sSymbols = new SymbolTable();
				}
				// create method map if we need it
				sMethodMap = new MethodMap(sSymbols);
			}

			// create AMF writer from stream
			sLog = new Log(stream, autoCloseStream);

			// write telemetry version
			WriteValue(".tlm.version", Session.Version);
			WriteValue(".tlm.meta", (double)0.0);
			WriteValue(".tlm.date", new _root.Date().getTime());

			// write player info
			WriteValue(".player.version", "11,8,800,94");
//			WriteValue(".player.airversion", "3.8.0.910");
			WriteValue(".player.type", "PlayScript");
			WriteValue(".player.debugger", flash.system.Capabilities.isDebugger); 
			WriteValue(".player.global.date", new _root.Date().getTime());
			WriteValue(".player.instance", 0);
			WriteValue(".player.scriptplayerversion", swfVersion);

			// write platform info
			WriteValue(".platform.cpucount", System.Environment.ProcessorCount);
			WriteValue(".platform.capabilities", flash.system.Capabilities.serverString);

#if PLATFORM_MONOMAC
			WriteValue(".platform.gpu.kind", "opengl");
#else
			WriteValue(".platform.gpu.kind", "opengles2");
#endif

#if PLATFORM_MONOMAC || PLATFORM_MONOTOUCH || PLATFORM_MONODROID
			// write gpu info
			WriteValue(".platform.gpu.vendor", GL.GetString(StringName.Vendor));
			WriteValue(".platform.gpu.renderer", GL.GetString(StringName.Renderer));
			WriteValue(".platform.gpu.version", GL.GetString(StringName.Version));
			WriteValue(".platform.gpu.shadinglanguageversion", GL.GetString(StringName.ShadingLanguageVersion));
			WriteValue(".platform.gpu.extensions", GL.GetString(StringName.Extensions));
#endif

			// write memory stats
			WriteValue(".mem.total", 8 * 1024);
			WriteValue(".mem.used", 4 * 1024);
			WriteValue(".mem.managed", 0);
			WriteValue(".mem.managed.used", 0);
			WriteValue(".mem.telemetry.overhead", 0);

			// write telemetry categories
			WriteCategoryEnabled("3D", CategoryEnabled3D);
			WriteCategoryEnabled("sampler", CategoryEnabledSampler);
			WriteCategoryEnabled("displayobjects", CategoryEnabledDisplayObjects);
			WriteCategoryEnabled("alloctraces", CategoryEnabledAllocTraces);
			WriteCategoryEnabled("allalloctraces", CategoryEnabledAllAllocTraces);
			WriteCategoryEnabled("customMetrics", CategoryEnabledCustomMetrics);

			WriteValue(".network.loadmovie", "app:/" + appName );
			WriteValue(".rend.display.mode", "auto");

			// SWF startup timestamp
			WriteTime(".swf.start");

			// write swf stats
			WriteSWFStats(appName, (int)flash.system.Capabilities.screenResolutionX, (int)flash.system.Capabilities.screenResolutionY, 60, swfVersion, swfSize);

			// write memory stats
			WriteMemoryStats();

			// start categories
			if (CategoryEnabledAllocTraces) {
				WriteValue(".tlm.category.start", "alloctraces");
			}

			if (CategoryEnabledCustomMetrics) {
				WriteValue(".tlm.category.start", "customMetrics");
			}

			if (CategoryEnabledSampler) {
				WriteValue(".tlm.category.start", "sampler");
			}

			// enable 'advanced telemetry'
			WriteValue(".tlm.detailedMetrics.start", true);

			Flush();

			if (CategoryEnabledSampler) {
				// start sampler
				sSampler = new Sampler(sLog.StartTime, sLog.Divisor, SamplerRate, SamplerMaxCallStackDepth, SamplerStartDelay);
			}
		}

		private static void EndSession()
		{
			try
			{
				if (Connected) {
					Flush();
				}
			} catch {
			}

			try
			{
				// stop sampler
				if (sSampler != null) {
					sSampler.Dispose();
					sSampler = null;
				}
			} catch {
			}

			try
			{
				// close log
				if (sLog != null) {
					sLog.Close();
					sLog = null;
				}
			} catch {
			}
		}

		public static void OnBeginFrame()
		{
			if (!Connected) return;

			// emit a timecode for '.enter'
			WriteTime(sNameEnter);

			// swf frame must be written here
			WriteValue(sNameSwfFrame, 0);

			// begin a .exit span
			sSpanExit.Begin();
		}

		public static void OnEndFrame()
		{
			if (!Connected) return;

			// end .exit
			if (sSpanExit.IsInSpan) {
				sSpanExit.End();

				sSpanTelemetry.Begin();
				Flush();
				sSpanTelemetry.End();
			}
		}

		public static void OnResize(int width, int height)
		{
			if (!Connected)	return;

			var rect = new Telemetry.Protocol.Rect();
			rect.xmin = 0;
			rect.ymin = 0;
			rect.xmax = width;
			rect.ymax = height;
			WriteValue(".player.view.resize", rect);
		}

		public static void WriteMemoryStats()
		{
			if (!Connected)	return;

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

		// starts a telemetry session and writes telemetry data over the network
		public static bool Connect()
		{
			return Connect(DefaultHostName, DefaultPort);
		}

		// starts a telemetry session and writes telemetry data over the network
		public static bool Connect(string hostname, int port, bool loadRemoteConfig = true)
		{
			if (!Enabled) {
				// telemetry is not enabled
				Console.WriteLine("Telemetry: did not connect, telemetry not enabled");
				return false;
			}

			if (Connected) {
				// already connected...
				Console.WriteLine("Telemetry: already connected, disconnect first");
				return true;
			}

			if (loadRemoteConfig) {
				// load remote configuration from telemetry tool before connecting
				LoadRemoteConfig(hostname, port);
			}

			Console.WriteLine("Telemetry: connecting to {0}:{1}...", hostname, port);
			try
			{
				// open network connection
				var client = new TcpClient(hostname, port);
				var buffered = new BufferedStream(client.GetStream());
				// begin session
				BeginSession(buffered);
				Console.WriteLine("Telemetry: connected!");
				return true;
			}
			catch 
			{
				Console.WriteLine("Telemetry: connection failed");
				return false;
			}
		}

		// starts a telemetry session and writes telemetry data to a file
		public static bool ConnectToFile(string outputFilePath)
		{
			if (!Enabled) {
				// telemetry is not enabled
				Console.WriteLine("Telemetry: could not write to file, telemetry not enabled");
				return false;
			}

			if (Connected) {
				// already connected...
				Console.WriteLine("Telemetry: already connected, disconnect first");
				return true;
			}

			try
			{
				// open file stream
				var fs = File.Open(outputFilePath, FileMode.Create);
				var buffered = new BufferedStream(fs);
				// begin session
				BeginSession(buffered);
				Console.WriteLine("Telemetry: writing to file {0}", outputFilePath);
				return true;
			}
			catch
			{
				Console.WriteLine("Telemetry: could not open file for writing: {0}", outputFilePath);
				return false;
			}
		}

		// disconnects the current session
		public static void Disconnect()
		{
			if (!Connected) {
				return ;
			}

			EndSession();
		}

		// begins recording telemetry data to a growable memory buffer
		public static bool StartRecording(int bufferCapacity = 256 * 1024)
		{
			if (!Enabled) {
				// telemetry is not enabled
				Console.WriteLine("Telemetry: did not record, telemetry not enabled");
				return false;
			}

			if (Connected) {
				// already connected...
				Console.WriteLine("Telemetry: could not record, already connected");
				return false;
			}

			// record to a new memory stream
			sRecording = new MemoryStream(bufferCapacity);

			Console.WriteLine("Telemetry: began recording to memory buffer");

			// start session (dont auto-close recording stream)
			BeginSession(sRecording, false);
			return true;
		}

		// ends a recording session and returns memory stream containing the data
		public static MemoryStream EndRecording()
		{
			var recording = sRecording;
			if (recording != null) {
				// stop recording
				EndSession();

				sRecording = null;
				Console.WriteLine("Telemetry: end recording");
			}
			// return recording
			return recording;
		}

		// sends a recording (stored in a stream) over the network
		public static void SendRecordingOverNetwork(Stream stream)
		{
			SendRecordingOverNetwork(stream, DefaultHostName, DefaultPort);
		}

		// sends a recording (stored in a stream) over the network
		public static void SendRecordingOverNetwork(Stream stream, string hostname, int port)
		{
			if (!Enabled) {
				// telemetry is not enabled
				Console.WriteLine("Telemetry: did not send recording, telemetry not enabled");
				return;
			}

			try {
				Console.WriteLine("Telemetry: network sending {0} bytes to {1}:{2}", stream.Length, hostname, port);
				using (var client = new TcpClient(hostname, port)) {
					using (var ns = client.GetStream()) {
						stream.Position = 0;

						var buffer = new byte[16 * 1024];
						int count;
						// read from input stream
						while ( (count=stream.Read(buffer, 0, buffer.Length)) > 0)
						{
							// write to network stream
							ns.Write(buffer, 0, count);
							ns.Flush();
							// we can't feed data too fast or else the server will ignore it 
							System.Threading.Thread.Sleep(250);
						}
					}
				}
				Console.WriteLine("Telemetry: network send completed!");
			} catch {
				Console.WriteLine("Telemetry: network send failed");
			}
		}

		// saves a recording (stored in a stream) to a file
		public static void SaveRecordingToFile(Stream stream, string outputFilePath)
		{
			if (!Enabled) {
				// telemetry is not enabled
				Console.WriteLine("Telemetry: did not save recording, telemetry not enabled");
				return;
			}

			try {
				Console.WriteLine("Telemetry: writing {0} bytes to file {1}", stream.Length, outputFilePath);
				using (var fs = File.Open(outputFilePath, FileMode.Create)) {
					stream.Position = 0;
					stream.CopyTo(fs);
				}
				Console.WriteLine("Telemetry: file write completed!");
			} catch {
				Console.WriteLine("Telemetry: file write failed");
			}		
		}

		// flushes all accumulated data to the output stream (over the network or to a file)
		public static void Flush()
		{
			if (sLog != null) {

				try
				{
					if ((sSampler != null) && (sMethodMap!=null)) {
						// write sampler data
						sSampler.Write(sMethodMap);
					}

					if (sMethodMap != null) {
						// write method map 
						sMethodMap.Write();
					}

					// flush output stream (to network or file)
					sLog.Flush();
				}
				catch 
				{
					// error writing to log? connection closed?
					sLog = null;

					EndSession();
				}
			}
		}

		public static bool LoadRemoteConfig()
		{
			if (!Enabled) return false;
			return LoadRemoteConfig(DefaultHostName, DefaultPort);
		}

		public static bool LoadRemoteConfig(string hostname, int port)
		{
			if (!Enabled) return false;

			try {
				Console.WriteLine("Telemetry: fetching remote config from {0}:{1}", hostname, port);
				using (var client = new TcpClient(hostname, port)) {
					using (var ns = client.GetStream()) {
						// write config request
						ns.Write(System.Text.Encoding.UTF8.GetBytes("*MC1*"));
						using (var sr = new StreamReader(ns)) {
							string configText = sr.ReadToEnd();
							Console.WriteLine("Telemetry: fetch remote config completed!");
							ParseConfig(configText);
							return true;
						}
					}
				}
			} catch {
				Console.WriteLine("Telemetry: fetch remote config failed");
				return false;
			}

		}

		public static bool LoadLocalConfig(string configPath)
		{
			if (!Enabled) return false;

			try {
				string path = PlayScript.Player.TryResolveResourcePath(configPath);
				if (path == null) {
					// config not found
					return false;
				}

				// read all config lines
				var configText = File.ReadAllText(path);
				ParseConfig(configText);
				return true;
			} catch {
				// exception
				return false;
			}
		}

		// the session init loads the telemetry configuration and connects
		public static void Init(bool needLocalConfig = false)
		{
			if (!Enabled) return;

			// load configuration
			if (!needLocalConfig || LoadLocalConfig(ConfigFileName)) {
				// if we have configuration, then connect
				Connect();
			}
		}

		#region Private
		private static bool ParseConfigBool(string value)
		{
			// get everything before comma
			int comma = value.IndexOf(',');
			if (comma >= 0) {
				value = value.Substring(0, comma);
			}

			value = value.ToLowerInvariant();
			return value == "true" || value == "1" || value == "on";
		}

		private static bool ParseConfig(string configText)
		{
			var lines = configText.Split('\n');
			foreach (var line in lines) {
				var split = line.Split(new char[] {'='}, 2);
				if (split.Length == 2) {
					var name = split[0].Trim();
					var value = split[1].Trim();
					switch (name) {
					case "TelemetryAddress":
						{
							var split2 = value.Split(new char[] {':'}, 2);
							// get hostname
							DefaultHostName = split2[0];
							if (split2.Length >= 2) {
								// get port
								int.TryParse(split2[1], out DefaultPort);
							}
							break;
						}
					case "SamplerEnabled":
						CategoryEnabledSampler = ParseConfigBool(value);
						break;
					case "Stage3DCapture":
						CategoryEnabled3D = ParseConfigBool(value);
						break;
					case "DisplayObjectCapture":
						CategoryEnabledDisplayObjects = ParseConfigBool(value);
						break;
					case "ScriptObjectAllocationTraces":
						CategoryEnabledAllocTraces = ParseConfigBool(value);
						break;
					case "CPUCapture":
						CategoryEnabledCPU = ParseConfigBool(value);
						break;
					default:
						break;
					}
				}
			}
			return true;
		}


		// telemetry log
		private static Log		 	  sLog;

		// current recording (to memory)
		private static MemoryStream   sRecording;

		// sampler for profiling 
		private static Sampler 		  sSampler = null;

		// method map for translating addresses to symbols to ids
		private static MethodMap 	  sMethodMap = null;

		// symbol table (for use by method map)
		private static SymbolTable 	  sSymbols = null;

		private static readonly Amf3String     sNameTrace   = new Amf3String(".trace");
		private static readonly Amf3String     sNameSwfFrame  = new Amf3String(".swf.frame");
		private static readonly Amf3String     sNameEnter  = new Amf3String(".enter");
		private static readonly Span 		   sSpanExit      = new Span(".exit");
		private static readonly Span 		   sSpanTelemetry = new Span(".player.workerpoll");
		#endregion
	}
}

