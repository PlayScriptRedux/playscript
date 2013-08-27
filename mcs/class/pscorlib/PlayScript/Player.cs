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

			Telemetry.Session.Connect();
		}

		// the main class of the player (will be loaded after player initializes)
		public static System.Type 	ApplicationClass {get; set;}

		// arguments to the main class (usually form command line)
		public static string[] 		ApplicationArgs {get; set;}

		public static int           ApplicationLoadDelay {get;set;}

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
					var newPath = PlayScript.Player.ResolveResourcePath(path);
					var jsonText = System.IO.File.ReadAllText(newPath);
					return _root.JSON.parse(jsonText);
				}

				default:
					throw new NotImplementedException("Loader for " + ext);
			}
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
			try {
				Application.Context.Assets.Open(path);
			} catch (IOException e)
			{
				Console.WriteLine("File does not exists for " + path + " " + e.Message);
				return null;
			}

			return path;
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
			var ke = new flash.events.KeyboardEvent(flash.events.KeyboardEvent.KEY_DOWN, true, false, charCode, keyCode);
			mStage.dispatchEvent (ke);
		}
		
		public void OnKeyUp (uint charCode, uint keyCode)
		{
			var ke = new flash.events.KeyboardEvent(flash.events.KeyboardEvent.KEY_UP, true, false, charCode, keyCode);
			mStage.dispatchEvent (ke);
		}
		
		public void OnMouseDown (PointF p, uint buttonMask)
		{
			mStage.mouseX = p.X;
			mStage.mouseY = p.Y;
			
			// dispatch touch event
			var te = new flash.events.TouchEvent(flash.events.TouchEvent.TOUCH_BEGIN, true, false, 0, true, p.X, p.Y, 1.0, 1.0, 1.0 );
			mStage.dispatchEvent (te);
			
			// dispatch mouse event
			var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_DOWN, true, false, p.X, p.Y, mStage);
			mStage.dispatchEvent (me);
		}
		
		public void OnMouseUp (PointF p, uint buttonMask)
		{
			mStage.mouseX = p.X;
			mStage.mouseY = p.Y;
			
			// dispatch touch event
			var te = new flash.events.TouchEvent(flash.events.TouchEvent.TOUCH_END, true, false, 0, true, p.X, p.Y, 1.0, 1.0, 1.0 );
			mStage.dispatchEvent (te);
			
			// dispatch mouse event
			var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_UP, true, false, p.X, p.Y, mStage);
			mStage.dispatchEvent (me);
		}
		
		public void OnMouseMoved(PointF p, uint buttonMask)
		{
			mStage.mouseX = p.X;
			mStage.mouseY = p.Y;
			
			// dispatch touch event
			var te = new flash.events.TouchEvent(flash.events.TouchEvent.TOUCH_MOVE, true, false, 0, true, p.X, p.Y, 1.0, 1.0, 1.0 );
			mStage.dispatchEvent (te);
			
			// dispatch mouse event
			var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_MOVE, true, false, p.X, p.Y, mStage);
			mStage.dispatchEvent (me);
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
				var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_WHEEL, true, false, mStage.mouseX, mStage.mouseY, mStage);
				me.delta = intDelta;
				mStage.dispatchEvent (me);

				// subtract off of accumulated delta
				mScrollDelta -= (float)intDelta;
			}
		}

		public void OnTouchesBegan(List<flash.events.TouchEvent> touches)
		{
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
		}

		public void OnTouchesMoved (List<flash.events.TouchEvent> touches)
		{
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
		}

		public void OnTouchesEnded (List<flash.events.TouchEvent> touches)
		{
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
		}

#if PLATFORM_MONOTOUCH

		public void OnPinchRecognized(UIPinchGestureRecognizer pinchRecognizer)
		{
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
		}
#endif

		public void OnFrame(RectangleF bounds)
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
			mSpanPlayerEnterFrame.Begin();
			Profiler.Begin("enterFrame");
			mStage.onEnterFrame ();
			Profiler.End("enterFrame");
			mSpanPlayerEnterFrame.End();

			// update all timer objects
			mSpanPlayerTimer.Begin();
			Profiler.Begin("timers");
			flash.utils.Timer.advanceAllTimers();
			Profiler.End("timers");
			mSpanPlayerTimer.End();

			// stage exit frame
			mSpanPlayerExitFrame.Begin();
			mStage.onExitFrame();
			mSpanPlayerExitFrame.End();

			mFrameCount++;
		}


		// runs until graphics have been presented through Stage3D
		public void RunUntilPresent(RectangleF bounds, Action onPresent = null, int maxFrames = 1000)
		{
			Telemetry.Session.OnBeginFrame();

			bool didPresent = false;

			// set context3D callback
			flash.display3D.Context3D.OnPresent = (context) =>
			{
				didPresent = true;
				if (onPresent != null)
					onPresent();
			};

			// loop until a Stage3D present occurred
			int count = 0; 
			while (!didPresent && (count < maxFrames))
			{
				OnFrame(bounds);
				count++;
			}

			Profiler.OnFrame();

			Telemetry.Session.OnEndFrame();
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
				if (File.Exists(path)) {
					return System.IO.File.ReadAllText(path);
				} else {
					return null;
				}
			}
			return null;
		}

		public static flash.utils.ByteArray LoadBinaryWebResponseFromCache(string hash)
		{
			if (Offline && WebCachePath != null) {
				var path = 	System.IO.Path.Combine(WebCachePath, hash); 
				if (File.Exists(path)) {
					return flash.utils.ByteArray.loadFromPath(path);
				} else {
					return null;
				}
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

		private static List<string> sResourceDirectories = new List<string>();

		private readonly Telemetry.Span mSpanPlayerEnterFrame = new Telemetry.Span(".player.enterframe");
		private readonly Telemetry.Span mSpanPlayerExitFrame = new Telemetry.Span(".player.exitframe");
		private readonly Telemetry.Span mSpanPlayerTimer = new Telemetry.Span(".player.timer");
	}
}
		
