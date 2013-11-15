// Copyright 2013 Zynga Inc.
//	
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//		
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.IO;
using System.Linq;

using flash.display;
using flash.utils;

#if PLATFORM_MONOMAC
using MonoMac.Foundation;
using MonoMac.AppKit;
#elif PLATFORM_MONOTOUCH
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#elif PLATFORM_MONODROID
using Android.App;
#endif


namespace PlayScript
{
	public delegate string ResourcePathConverter(string origPath);

	public class Player
	{
		public string Title
		{
			get; set;
		}

		public Player(RectangleF bounds)
		{
			// clear title
			Title = "";
			
			// construct flash stage
			mStage = new flash.display.Stage ((int)bounds.Width, (int)bounds.Height);
		}

		// the main class of the player (will be loaded after player initializes)
		public static System.Type 	ApplicationClass {get; set;}

		// arguments to the main class (usually form command line)
		public static string[] 		ApplicationArgs {get; set;}

		public static int           ApplicationLoadDelay {get;set;}

		// desired content scale for application (1.0=nonretina, 2.0=retina)
		public static double?       ApplicationContentScale {get;set;}

		public static ResourcePathConverter RemotePathConverter {get;set;}

		// if true, any local .json will attempt to be loaded as AMF if a file exists with the ".json.amf.z" extension
		public static bool LoadJsonAsAmf {get; set;}

		// maximum time spent running event loop until present is done
		public static double MaxRunTimeUntilPresent = 100.0;

		// time to sleep between frames if no present occurs
		public static int SleepTimeBetweenFrames = 1;

		static Player()
		{
			// add resource directories in static constructor
			AddResourceDirectory("");
			#if PLATFORM_MONOMAC || PLATFORM_MONOTOUCH
			AddResourceDirectory(NSBundle.MainBundle.ResourcePath + "/src/");
			AddResourceDirectory(NSBundle.MainBundle.ResourcePath);
			#elif PLATFORM_MONODROID
			AddResourceDirectory("file:///android_asset");
			#endif
		}

		// this method returns all classes with the [SWF] attribute
		// it will also optionally load all assemblies in the application directory
		public static List<System.Type> GetAllSWFClasses(bool loadAllAssemblies = false)
		{
			if (loadAllAssemblies)
			{
				// load all assemblies in the application directory
				// this may need to be done if the assembly is not referenced by the main app
				string path = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
				foreach (string dll in System.IO.Directory.GetFiles(path, "*.dll"))
				{
					Console.WriteLine("Loading assembly {0}", dll);
					Assembly.LoadFile(dll);
				}
			}

			var list = new List<System.Type>();
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (var type in assembly.GetTypes())
				{
					if (Attribute.IsDefined(type, typeof(_root.SWFAttribute)))
				    {
						list.Add(type);
					}
				}
			}
			return list;
		}

		public DisplayObject LoadSWFClassByName(string name, bool loadAllAssemblies = false)
		{
			name = name.ToLowerInvariant();
			foreach (var type in GetAllSWFClasses(loadAllAssemblies))
			{
				// this does a simple fuzzy match
				if (type.Name.ToLowerInvariant().Contains(name))
				{
					return LoadClass(type);
				}
			}
			// not found
			return null;
		}

		// returns (optional) desired size of object from [SWF] attributes
		public static System.Drawing.Size? GetSWFDesiredSize(System.Type type)
		{
			if (type != null) {
				foreach (var attr in type.GetCustomAttributes(true)) {
					var swfAttr = attr as _root.SWFAttribute;
					if (swfAttr != null) {
						return swfAttr.GetDesiredSize();
					}
				}
			}
			return null;
		}
		
		public DisplayObject LoadClass(System.Type type)
		{
			// construct instance of type
			// we set the global stage so that it will be set during this display object's constructor
			DisplayObject.constructorStage = mStage;
			DisplayObject.constructorLoaderInfo = new LoaderInfo();
			DisplayObject displayObject = null;

			try {
				displayObject = System.Activator.CreateInstance(type) as DisplayObject;
			} catch (Exception e)
			{
				Console.WriteLine("CreateInstance {0} Exception: {1}", type, e.ToString());
				if (e.InnerException != null)
					Console.WriteLine("InnerException: {0}", e.InnerException.ToString());

			}
			DisplayObject.constructorLoaderInfo = null;
			DisplayObject.constructorStage = null;

			if (displayObject != null) {

				// resize object to size of stage
				displayObject.width  = mStage.stageWidth;
				displayObject.height = mStage.stageHeight;

				// add display object to stage
				mStage.addChild(displayObject);

				// set title
				Title = displayObject.GetType().ToString();
			}

			return displayObject;
		}

		public static object LoadResource(string path, string mimeType = null)
		{
			var ext = Path.GetExtension(path).ToLowerInvariant();

			if (path.StartsWith("http:") || path.StartsWith("https:")) {
				// We probably want to create a lower level at some point, no need to go through all this
				flash.net.URLRequest urlRequest = new flash.net.URLRequest(path);
				flash.net.URLLoader urlLoader = new flash.net.URLLoader(urlRequest);
				urlLoader.dataFormat = flash.net.URLLoaderDataFormat.BINARY;
				urlLoader.load(urlRequest, true);		// We want the result synchronously before leaving this function
				if (urlLoader.bytesLoaded == 0) {
					// If empty, we consider this an error for the moment
					Console.WriteLine("Add better error handling for Player.LoadResource() - Path: " + path);
					return new flash.display.Bitmap(new flash.display.BitmapData(32,32));
				}

				flash.utils.ByteArray dataAsByteArray = urlLoader.data as flash.utils.ByteArray;
				if (dataAsByteArray == null)
				{
					throw new NotImplementedException();	// This case should not actually happen, did we miss something in the URL loader implementation?
				}

				if (mimeType != null && mimeType.StartsWith("image/"))
					return new flash.display.Bitmap(flash.display.BitmapData.loadFromByteArray(dataAsByteArray));

				switch (ext)
				{
				case ".bmp":
				case ".png":
				case ".jpg":
				case ".jpeg":
				case ".atf":
				case ".tif":
					// load as bitmap
					return new flash.display.Bitmap(flash.display.BitmapData.loadFromByteArray(dataAsByteArray));

				default:
					throw new NotImplementedException("HTTP loader for " + ext);
				}
			}

			// handle byte arrays
			if (mimeType == "application/octet-stream")
			{
				// load as byte array
				return flash.utils.ByteArray.loadFromPath(path);
			}

			switch (ext)
			{
				case ".swf":
					// loading of swf's is not supported, so create a dummy sprite
					return new flash.display.Sprite();
				case ".bmp":
				case ".png":
				case ".jpg":
				case ".jpeg":
				case ".atf":
				case ".tif":
					// load as bitmap
					return new flash.display.Bitmap(flash.display.BitmapData.loadFromPath(path));
				case ".jxr":
				{
					// TODO we dont support jxr loading yet so rename them to png instead
					// you will have to manually convert your jxr images to png
					var rename = Path.ChangeExtension(path, ".png");
					return new flash.display.Bitmap(flash.display.BitmapData.loadFromPath(rename));
				}

				case ".json":
				{
					
#if PLATFORM_MONOTOUCH || PLATFORM_MONOMAC
					var newPath = PlayScript.Player.ResolveResourcePath(path);
					var jsonText = System.IO.File.ReadAllText(newPath);
#elif PLATFORM_MONODROID
					var jsonText = "";
				    Stream stream;
				    try
				    {
					    stream = Application.Context.Assets.Open(path);
				    }
				    // if cannot load as assets, try loading it as plain file
				    // disable the ex defined but not used "warning as error"
					#pragma warning disable 0168
					catch (Java.IO.FileNotFoundException ex)
					#pragma warning restore 0168
					{
					    stream = File.OpenRead(path);
					}
					using (StreamReader sr = new System.IO.StreamReader(stream))
					{
						jsonText = sr.ReadToEnd();
						sr.Close();
					}
#else
					var jsonText = "";
#endif
					return _root.JSON.parse(jsonText);
				}

				default:
					throw new NotImplementedException("Loader for " + ext);
			}
		}

		public static string ToRemotePath(string path)
		{
			if (RemotePathConverter != null)
				return RemotePathConverter(path);
			else
				return path;
		}

		public static string ResolveResourcePath(string path)
		{
			string altPath = TryResolveResourcePath(path);
			if (altPath == null)
			{
				throw new System.IO.FileNotFoundException("File does not exist: " + path + "\nMake sure that it has been added with build action of BundledResource, or add a new search directory with AddResourceDirectory()\n" );
			}
			return altPath;
		}

		private static string normalizePath(string path)
		{
			string[] pathParts = path.Split(Path.DirectorySeparatorChar);
			List<string> result = new List<string>();
			int skip = 0;

			for(int i = pathParts.Length - 1; i >= 0; i--)
			{
				string p = pathParts[i];
				if(p == "" || p == ".")
				{
					continue;
				}

				if(p == "..")
				{
					skip++;
					continue;
				}

				if(skip != 0)
				{
					skip--;
					continue;
				}

				result.Insert(0, p);
			}

			string separator = new string(Path.DirectorySeparatorChar, 1);
			return string.Join(separator, result);
		}

		public static string TryResolveResourcePath(string path)
		{
			if (File.Exists(path))
			{ 
				// found file at this location
				return path;
			}

			// remove unneeded prefixes
			var prefixes = new string[] {"file://", "/../", "../../", "../"};
			foreach (var prefix in prefixes) 
			{
				if (path.StartsWith(prefix))
				{
					path = path.Substring(prefix.Length);
					break;
				}
			}

#if PLATFORM_MONODROID
			string npath = normalizePath(path);
			try {
				Application.Context.Assets.Open(npath);
			} 
			catch (Java.IO.FileNotFoundException e)
			{
			    Console.WriteLine("File does not exists for " + path + " " + e.Message);
			    return null;
			}

			return npath;
#else

			// try all resource directories 
			foreach (var dir in sResourceDirectories)
			{
				var altPath = Path.Combine(dir, path);
				if (File.Exists(altPath))
				{ 
					// found file at this location
					return altPath;
				}
			}

			return null;
#endif
		}

		public static void AddResourceDirectory(string dir)
		{
			if (!sResourceDirectories.Contains(dir)) {
				sResourceDirectories.Add(dir);
			}
		}

		public void OnKeyDown (uint charCode, uint keyCode)
		{
			mSpanPlayerKeyDown.Begin();
			var ke = new flash.events.KeyboardEvent(flash.events.KeyboardEvent.KEY_DOWN, true, false, charCode, keyCode);
			mStage.dispatchEvent (ke);
			mSpanPlayerKeyDown.End();
		}
		
		public void OnKeyUp (uint charCode, uint keyCode)
		{
			mSpanPlayerKeyUp.Begin();
			var ke = new flash.events.KeyboardEvent(flash.events.KeyboardEvent.KEY_UP, true, false, charCode, keyCode);
			mStage.dispatchEvent (ke);
			mSpanPlayerKeyUp.End();
		}
		
		public void OnMouseDown (PointF p, uint buttonMask)
		{
			mStage.mouseX = p.X;
			mStage.mouseY = p.Y;
			
			// dispatch touch event
			mSpanPlayerTouch.Begin();
			var te = new flash.events.TouchEvent(flash.events.TouchEvent.TOUCH_BEGIN, true, false, 0, true, p.X, p.Y, 1.0, 1.0, 1.0 );
			mStage.dispatchEvent (te);
			mSpanPlayerTouch.End();

			// dispatch mouse event
			mSpanPlayerMouseDown.Begin();
			var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_DOWN, true, false, p.X, p.Y, mStage);
			mStage.dispatchEvent (me);
			mSpanPlayerMouseDown.End();
		}
		
		public void OnMouseUp (PointF p, uint buttonMask)
		{
			mStage.mouseX = p.X;
			mStage.mouseY = p.Y;
			
			// dispatch touch event
			mSpanPlayerTouch.Begin();
			var te = new flash.events.TouchEvent(flash.events.TouchEvent.TOUCH_END, true, false, 0, true, p.X, p.Y, 1.0, 1.0, 1.0 );
			mStage.dispatchEvent (te);
			mSpanPlayerTouch.End();

			// dispatch mouse event
			mSpanPlayerMouseUp.Begin();
			var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_UP, true, false, p.X, p.Y, mStage);
			mStage.dispatchEvent (me);
			mSpanPlayerMouseUp.End();
		}
		
		public void OnMouseMoved(PointF p, uint buttonMask)
		{
			mStage.mouseX = p.X;
			mStage.mouseY = p.Y;
			
			// dispatch touch event
			mSpanPlayerTouch.Begin();
			var te = new flash.events.TouchEvent(flash.events.TouchEvent.TOUCH_MOVE, true, false, 0, true, p.X, p.Y, 1.0, 1.0, 1.0 );
			mStage.dispatchEvent (te);
			mSpanPlayerTouch.End();

			// dispatch mouse event
			mSpanPlayerMouseMove.Begin();
			var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_MOVE, true, false, p.X, p.Y, mStage);
			mStage.dispatchEvent (me);
			mSpanPlayerMouseMove.End();
		}

		public void OnScrollWheel(PointF p, float delta)
		{
			if (delta != 0.0f)
			{
				mStage.mouseX = p.X;
				mStage.mouseY = p.Y;

				// accumulate the deltas here since flash only accepts integer deltas
				mScrollDelta += delta;

				// Console.WriteLine("OnScrollWheel {0} {1}", delta, mScrollDelta);
			}
		}

		private void DispatchScrollWheelEvents()
		{
			// determine delta to dispatch
			// since flash only accepts integer deltas we dispatch incrementally one delta at a time
			int intDelta = 0;
			if (mScrollDelta < -5.0)	{
				intDelta = -5;
			} else if (mScrollDelta > 5.0)	{
				intDelta =  5;
			} else 	if (mScrollDelta < -1.0)	{
				intDelta = -1;
			} else 	if (mScrollDelta >  1.0)	{
				intDelta =  1;
			}

			if (intDelta != 0)
			{
				// Console.WriteLine("dispatch scroll delta {0} {1}", intDelta, mScrollDelta);
				
				// dispatch mouse wheel event
				mSpanPlayerMouseMove.Begin();
				var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_WHEEL, true, false, mStage.mouseX, mStage.mouseY, mStage);
				me.delta = intDelta;
				mStage.dispatchEvent (me);
				mSpanPlayerMouseMove.End();

				// subtract off of accumulated delta
				mScrollDelta -= (float)intDelta;
			}
		}

#if PLATFORM_MONOTOUCH
		private System.Drawing.PointF GetPosition(UITouch touch)
		{
			var p = touch.LocationInView(touch.View);

			// convert point to pixels
			var scale = touch.View.ContentScaleFactor;
			p.X *= scale;
			p.Y *= scale;
			return p;
		}

		// TODO: at some point we'll have a platform agnostic notion of touches to pass to the player, for now we use UITouch
		public void OnTouchesBegan (NSSet touches, UIEvent evt)
		{
			mMouseDown = true;		// This is used so we can send a MOUSE_UP event 

			foreach (UITouch touch in touches) {
				var p = GetPosition(touch);
				//Console.WriteLine ("touches-began {0}", p);

				var te = new flash.events.TouchEvent(flash.events.TouchEvent.TOUCH_BEGIN, true, false, 0, true, p.X, p.Y, 1.0, 1.0, 1.0 );
				mStage.dispatchEvent (te);

				// Mouse events can be deactivated if a gesture was recognized
				if (mDeactivateMouseEvents == false) {
					mStage.mouseX = p.X;
					mStage.mouseY = p.Y;
					var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_DOWN, true, false, p.X, p.Y, mStage);
					mStage.dispatchEvent (me);
				}
			}
		}

		public void OnTouchesMoved (NSSet touches, UIEvent evt)
		{
			foreach (UITouch touch in touches) {
				var p = GetPosition(touch);
				//Console.WriteLine ("touches-moved {0}", p);

				var te = new flash.events.TouchEvent(flash.events.TouchEvent.TOUCH_MOVE, true, false, 0, true, p.X, p.Y, 1.0, 1.0, 1.0 );
				mStage.dispatchEvent (te);

				// Mouse events can be deactivated if a gesture was recognized
				if (mDeactivateMouseEvents == false) {
					mStage.mouseX = p.X;
					mStage.mouseY = p.Y;
					var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_MOVE, true, false, p.X, p.Y, mStage);
					mStage.dispatchEvent (me);
				}
			}
		}

		public void OnTouchesEnded (NSSet touches, UIEvent evt)
		{
			mMouseDown = false;

			foreach (UITouch touch in touches) {
				var p = GetPosition(touch);

				//Console.WriteLine ("touches-ended {0}", p);

				var te = new flash.events.TouchEvent(flash.events.TouchEvent.TOUCH_END, true, false, 0, true, p.X, p.Y, 1.0, 1.0, 1.0 );
				mStage.dispatchEvent (te);

				// Mouse events can be deactivated if a gesture was recognized
				if (mDeactivateMouseEvents == false) {
					mStage.mouseX = p.X;
					mStage.mouseY = p.Y;
					// Mouse up events can be deactivated is a gesture was recognized before and we had to already send a mouse up
					// to avoid having the mouse up at the end of gesture (with the full delta range between the two)
					if (mSkipNextMouseUp == false) {
						var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_UP, true, false, p.X, p.Y, mStage);
						mStage.dispatchEvent (me);
					}
				}
			}

			if (mDeactivateMouseEvents == false) {
				mSkipNextMouseUp = false;
			}
		}
#else
		public void OnTouchesBegan(List<flash.events.TouchEvent> touches)
		{
			mSpanPlayerTouch.Begin();
			foreach (flash.events.TouchEvent touch in touches) {
				mStage.dispatchEvent (touch);

				// Mouse events can be deactivated if a gesture was recognized
				if (mDeactivateMouseEvents == false) {
					mStage.mouseX = touch.localX;
					mStage.mouseY = touch.localY;
					var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_DOWN, true, false, touch.localX, touch.localY, mStage);
					mStage.dispatchEvent (me);
				}
			}
			mSpanPlayerTouch.End();
		}

		public void OnTouchesMoved (List<flash.events.TouchEvent> touches)
		{
			mSpanPlayerTouch.Begin();
			foreach (flash.events.TouchEvent touch in touches) {

				mStage.dispatchEvent (touch);

				// Mouse events can be deactivated if a gesture was recognized
				if (mDeactivateMouseEvents == false) {
					mStage.mouseX = touch.localX;
					mStage.mouseY = touch.localY;
					var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_MOVE, true, false, touch.localX, touch.localY, mStage);
					mStage.dispatchEvent (me);
				}
			}
			mSpanPlayerTouch.End();
		}

		public void OnTouchesEnded (List<flash.events.TouchEvent> touches)
		{
			mSpanPlayerTouch.Begin();
			mMouseDown = false;

			foreach (flash.events.TouchEvent touch in touches) {

				mStage.dispatchEvent (touch);

				// Mouse events can be deactivated if a gesture was recognized
				if (mDeactivateMouseEvents == false) {
					mStage.mouseX = touch.localX;
					mStage.mouseY = touch.localY;
					// Mouse up events can be deactivated is a gesture was recognized before and we had to already send a mouse up
					// to avoid having the mouse up at the end of gesture (with the full delta range between the two)
					if (mSkipNextMouseUp == false) {
						var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_UP, true, false, touch.localX, touch.localY, mStage);
						mStage.dispatchEvent (me);
					}
				}
			}

			if (mDeactivateMouseEvents == false) {
				mSkipNextMouseUp = false;
			}
			mSpanPlayerTouch.End();
		}
#endif

		public void OnPinchRecognized(flash.events.TransformGestureEvent tge)
		{
			mSpanPlayerGesture.Begin();

			if (tge.phase == "begin") {
				mDeactivateMouseEvents = true;
				if (mMouseDown) {
					// We were already sending mouse down event, so to close the loop, we are going to send a mouse up event with the last position for completion
					var me = new flash.events.MouseEvent (flash.events.MouseEvent.MOUSE_UP, true, false, mStage.mouseX, mStage.mouseY, mStage);
					mStage.dispatchEvent (me);
					mSkipNextMouseUp = true;
				}
			} else if (tge.phase == "end") {
				mDeactivateMouseEvents = false;
			} else {
				mDeactivateMouseEvents = false;
			}

			mStage.dispatchEvent(tge);
			mSpanPlayerGesture.End ();
		}

		#if PLATFORM_MONOTOUCH
		// Remove this method after refactor the rest of the project
		public void OnPinchRecognized(UIPinchGestureRecognizer pinchRecognizer)
		{
			mSpanPlayerGesture.Begin();
			var tge = new flash.events.TransformGestureEvent(flash.events.TransformGestureEvent.GESTURE_ZOOM, true, false);
			tge.scaleX = tge.scaleY = pinchRecognizer.Scale;

			switch (pinchRecognizer.State)
			{
				case UIGestureRecognizerState.Possible:
				case UIGestureRecognizerState.Began:
				tge.phase = "begin";
				mDeactivateMouseEvents = true;			// For swipe gestures, we don't want to send any mouse events at the same time
				if (mMouseDown) {
					// We were already sending mouse down event, so to close the loop, we are going to send a mouse up event with the last position for completion
					var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_UP, true, false, mStage.mouseX, mStage.mouseY, mStage);
					mStage.dispatchEvent (me);
					mSkipNextMouseUp = true;
				}
				break;

				case UIGestureRecognizerState.Changed:
				tge.phase = "update";
				break;

				case UIGestureRecognizerState.Recognized:
				//case UIGestureRecognizerState.Ended:		// Same as recognized
				tge.phase = "end";
				mDeactivateMouseEvents = false;
				break;

				case UIGestureRecognizerState.Cancelled:
				case UIGestureRecognizerState.Failed:
				mDeactivateMouseEvents = false;
				return;		// In this case, we don't even send the event
			}


			mStage.dispatchEvent(tge);
			mSpanPlayerGesture.End();
		}
		#endif

		public void OnFrame(RectangleF bounds, double maxTimeMs = 100.0)
		{
			//GL.ClearColor (1,0,1,0);
			//GL.Clear (ClearBufferMask.ColorBufferBit);

			// resize the stage every frame (this will do nothing unless size really changes)
			mStage.onResize((int)bounds.Size.Width, (int)bounds.Size.Height);

			// wait until the desired load delay (to fix some resizing issues on OSX)
			if (!mApplicationLoaded && mFrameCount >= ApplicationLoadDelay)
			{
				if (ApplicationClass != null) 
				{
					// load the application class
					LoadClass(ApplicationClass);
					mApplicationLoaded = true;
				}
			}

			// dispatch scroll wheel events 
			// these are done here because they must be done smoothly and incrementally, spread out over time
			DispatchScrollWheelEvents();

			// stage enter frame
			Profiler.Begin("enterFrame", ".player.enterframe");
			mStage.onEnterFrame ();
			Profiler.End("enterFrame");

			// update all timer objects
			Profiler.Begin("timers", ".player.timer");
			flash.utils.Timer.advanceAllTimers();
			Profiler.End("timers");

			// process loader queue
			Profiler.Begin("load", ".network.dourlrequests");
			flash.net.URLLoader.processQueue(maxTimeMs);
			Profiler.End("load");

			// stage exit frame
			Profiler.Begin("exitFrame", ".player.exitframe");
			mStage.onExitFrame();
			Profiler.End("exitFrame");

			mFrameCount++;
		}


		// runs until graphics have been presented through Stage3D
		public void RunUntilPresent(RectangleF bounds, Action onPresent = null)
		{
			Profiler.OnBeginFrame();
			Telemetry.Session.OnBeginFrame();

			// set context3D callback
			Player player = this;
			player.mDidPresent = false;
			flash.display3D.Context3D.OnPresent = (context) =>
			{
				player.mDidPresent = true;
				if (onPresent != null)
					onPresent();
			};

			// loop until a Stage3D present occurred
			var timer = Stopwatch.StartNew();
			while (!mDidPresent)
			{
				OnFrame(bounds);

				// dont let us run too long waiting for a present
				if (timer.ElapsedMilliseconds > MaxRunTimeUntilPresent) {
					break;
				} else {
					if (SleepTimeBetweenFrames > 0) {
						// sleep between frames
						Profiler.Begin("sleep", ".player.condition.wait");
						System.Threading.Thread.Sleep(SleepTimeBetweenFrames);
						Profiler.End("sleep");
					}
				}
			}

			Telemetry.Session.OnEndFrame();
			Profiler.OnEndFrame();
		}
		

		public static string CalculateMD5Hash(string input)
		{
			// step 1, calculate MD5 hash from input
			var md5 = System.Security.Cryptography.MD5.Create();
			byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
			byte[] hash = md5.ComputeHash(inputBytes);
			
			// step 2, convert byte array to hex string
			var sb = new System.Text.StringBuilder();
			for (int i = 0; i < hash.Length; i++)
			{
				sb.Append(hash[i].ToString("X2"));
			}
			return sb.ToString();
		}

		public static bool Offline = false;
		public static bool SaveToOfflineCache = false;
		public static string WebCachePath = null;

		public static string LoadTextWebResponseFromCache(string hash)
		{
			if (Offline && WebCachePath != null) {
				var path = 	System.IO.Path.Combine(WebCachePath, hash); 

#if PLATFORM_MONOTOUCH || PLATFORM_MONOMAC
				if (File.Exists(path)) {
					return System.IO.File.ReadAllText(path);
				} else {
					return null;
				}
#elif PLATFORM_MONODROID
				Stream stream;
				try{
					stream = Application.Context.Assets.Open(path);
				} catch {
					return null;
				}

				var jsonText = "";
				using (StreamReader sr = new System.IO.StreamReader(stream))
				{
					jsonText = sr.ReadToEnd();
					sr.Close();
				}
				return jsonText;
#else
				return null;
#endif
			}
			return null;
		}

		public static flash.utils.ByteArray LoadBinaryWebResponseFromCache(string hash)
		{
			if (Offline && WebCachePath != null) {
				var path = 	System.IO.Path.Combine(WebCachePath, hash); 

#if PLATFORM_MONOTOUCH || PLATFORM_MONOMAC
				if (File.Exists(path)) {
					return flash.utils.ByteArray.loadFromPath(path);
				} else {
					return null;
				}
#elif PLATFORM_MONODROID
				Stream stream = Application.Context.Assets.Open(path);
				flash.utils.ByteArray data = new flash.utils.ByteArray();
				data.readFromStream( stream );
				return data;
#endif
			}
			return null;
		}

		public static void StoreTextWebResponseIntoCache(string hash, string response)
		{
			if (SaveToOfflineCache && WebCachePath != null) {
				if (!System.IO.Directory.Exists(WebCachePath)) {
					System.IO.Directory.CreateDirectory(WebCachePath);
				}
				var path = 	System.IO.Path.Combine(WebCachePath, hash); 
				System.IO.File.WriteAllText(path, response);
			}
		}

		public static void StoreBinaryWebResponseIntoCache(string hash, flash.utils.ByteArray response)
		{
			if (SaveToOfflineCache && WebCachePath != null) {
				if (!System.IO.Directory.Exists(WebCachePath)) {
					System.IO.Directory.CreateDirectory(WebCachePath);
				}
				var path = 	System.IO.Path.Combine(WebCachePath, hash); 

				// write byte array to disk
				response.position = 0;
				using (var fs = new FileStream(path, FileMode.Create))
				{
					response.getRawStream().CopyTo(fs);
				}
				response.position = 0;
			}
		}


		private flash.display.Stage    mStage;
		private float mScrollDelta;
		private int   mFrameCount;
		private bool  mApplicationLoaded;
		private bool mDeactivateMouseEvents = false;
		private bool mMouseDown = false;
		private bool mSkipNextMouseUp = false;
		private bool mDidPresent = false;

		private static List<string> sResourceDirectories = new List<string>();

		private readonly Telemetry.Span mSpanPlayerKeyDown = new Telemetry.Span(".player.key.down");
		private readonly Telemetry.Span mSpanPlayerKeyUp = new Telemetry.Span(".player.key.up");
		private readonly Telemetry.Span mSpanPlayerMouseDown = new Telemetry.Span(".player.mouse.down");
		private readonly Telemetry.Span mSpanPlayerMouseUp = new Telemetry.Span(".player.mouse.up");
		private readonly Telemetry.Span mSpanPlayerMouseMove = new Telemetry.Span(".player.mouse.move");
		private readonly Telemetry.Span mSpanPlayerTouch = new Telemetry.Span(".player.touch");
		private readonly Telemetry.Span mSpanPlayerGesture = new Telemetry.Span(".player.gesture");
	}
}
		
