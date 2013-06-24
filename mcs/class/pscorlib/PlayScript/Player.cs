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
using System.Drawing;
using System.Reflection;
using System.IO;

using flash.display;

#if PLATFORM_MONOMAC
using MonoMac.Foundation;
using MonoMac.AppKit;
#elif PLATFORM_MONOTOUCH
using MonoTouch.Foundation;
using MonoTouch.UIKit;
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
		}

		static Player()
		{
			// add resource directories in static constructor
			AddResourceDirectory("");
			#if PLATFORM_MONOMAC || PLATFORM_MONOTOUCH 
			AddResourceDirectory(NSBundle.MainBundle.ResourcePath + "/src/");
			AddResourceDirectory(NSBundle.MainBundle.ResourcePath);
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
			// get desired size of object from [SWF] attributes
			System.Drawing.Size? desiredSize = GetSWFDesiredSize(type);

			// construct instance of type
			// we set the global stage so that it will be set during this display object's constructor
			DisplayObject.constructorStage = mStage;
			DisplayObject.constructorLoaderInfo = new LoaderInfo();
			DisplayObject displayObject = System.Activator.CreateInstance(type) as DisplayObject;
			DisplayObject.constructorLoaderInfo = null;
			DisplayObject.constructorStage = null;

			if (displayObject != null) {

				// TODO: this size may need to be set before the display object constructor executes
				if (desiredSize.HasValue) {
					// resize object to desired size
					displayObject.width  = desiredSize.Value.Width;
					displayObject.height = desiredSize.Value.Height;
				} else {
					// resize object to size of stage
					displayObject.width  = mStage.stageWidth;
					displayObject.height = mStage.stageHeight;
				}

				// add display object to stage
				mStage.addChild(displayObject);

				// set title
				Title = displayObject.GetType().ToString();
			}

			return displayObject;
		}

		public static object LoadResource(string path, string mimeType = null)
		{
			if (path.StartsWith("http:") || path.StartsWith("https:")) {
				// TODO: support loading via http
				return new flash.display.Bitmap(new flash.display.BitmapData(32,32));
			}

			// handle byte arrays
			if (mimeType == "application/octet-stream")
			{
				// load as byte array
				return flash.utils.ByteArray.loadFromPath(path);
			}

			var ext = Path.GetExtension(path).ToLowerInvariant();
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
			foreach (UITouch touch in touches) {
				var p = GetPosition(touch);
				//Console.WriteLine ("touches-began {0}", p);

				mStage.mouseX = p.X;
				mStage.mouseY = p.Y;

				var te = new flash.events.TouchEvent(flash.events.TouchEvent.TOUCH_BEGIN, true, false, 0, true, p.X, p.Y, 1.0, 1.0, 1.0 );
				mStage.dispatchEvent (te);

				var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_DOWN, true, false, p.X, p.Y, mStage);
				mStage.dispatchEvent (me);
			}
		}
		
		public void OnTouchesMoved (NSSet touches, UIEvent evt)
		{
			foreach (UITouch touch in touches) {
				var p = GetPosition(touch);
				//Console.WriteLine ("touches-moved {0}", p);

				mStage.mouseX = p.X;
				mStage.mouseY = p.Y;

				var te = new flash.events.TouchEvent(flash.events.TouchEvent.TOUCH_MOVE, true, false, 0, true, p.X, p.Y, 1.0, 1.0, 1.0 );
				mStage.dispatchEvent (te);

				var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_MOVE, true, false, p.X, p.Y, mStage);
				mStage.dispatchEvent (me);
			}
		}
		
		public void OnTouchesEnded (NSSet touches, UIEvent evt)
		{
			foreach (UITouch touch in touches) {
				var p = GetPosition(touch);

				//Console.WriteLine ("touches-ended {0}", p);

				mStage.mouseX = p.X;
				mStage.mouseY = p.Y;

				var te = new flash.events.TouchEvent(flash.events.TouchEvent.TOUCH_END, true, false, 0, true, p.X, p.Y, 1.0, 1.0, 1.0 );
				mStage.dispatchEvent (te);

				var me = new flash.events.MouseEvent(flash.events.MouseEvent.MOUSE_UP, true, false, p.X, p.Y, mStage);
				mStage.dispatchEvent (me);

			}
		}
#endif
		
		public void OnResize (RectangleF bounds)
		{
			// Reset The Current Viewport
			//GL.Viewport (0, 0, (int)bounds.Size.Width, (int)bounds.Size.Height);
			mStage.onResize((int)bounds.Size.Width, (int)bounds.Size.Height);
		}
		
		public void OnFrame()
		{
			//GL.ClearColor (1,0,1,0);
			//GL.Clear (ClearBufferMask.ColorBufferBit);

			// dispatch scroll wheel events 
			// these are done here because they must be done smoothly and incrementally, spread out over time
			DispatchScrollWheelEvents();
			
			// stage enter frame
			mStage.onEnterFrame ();
			
			// update all timer objects
			flash.utils.Timer.advanceAllTimers();
			
			// stage exit frame
			mStage.onExitFrame ();
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
		#if PLATFORM_MONOMAC || PLATFORM_MONOTOUCH 
		public static string WebCacheLoadPath = NSBundle.MainBundle.ResourcePath + "/webcache/";
		#else
		public static string WebCacheLoadPath = "./webcache/";
		#endif
		public static string WebCacheStorePath = null;

		public static string LoadWebResponseFromCache(string hash)
		{
			if (Offline && WebCacheLoadPath != null) {
				var path = 	WebCacheLoadPath + hash + ".response.txt"; 
				if (File.Exists(path)) {
					return System.IO.File.ReadAllText(path);
				} else {
					return null;
				}
			}
			return null;
		}

		public static void StoreWebResponseIntoCache(string hash, string response)
		{
			if (WebCacheStorePath != null) {
				if (!System.IO.Directory.Exists(WebCacheStorePath)) {
					System.IO.Directory.CreateDirectory(WebCacheStorePath);
				}
				var path = 	WebCacheStorePath + hash + ".response.txt"; 
				System.IO.File.WriteAllText(path, response);
			}
		}


		private int 					mFrameCount = 0;
		private flash.display.Stage    mStage;
		private float mScrollDelta;

		private static List<string> sResourceDirectories = new List<string>();
	}
}
		
