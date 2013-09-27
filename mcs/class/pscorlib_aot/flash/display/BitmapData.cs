using System;
using System.Collections.Generic;
using flash.display;
using flash.geom;
using flash.utils;
using flash.text;

#if PLATFORM_MONOMAC
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
using MonoMac.AppKit;
#elif PLATFORM_MONOTOUCH
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;
using MonoTouch.UIKit;
#elif PLATFORM_MONODROID
using Android.Graphics;
using Android.App;
using Java.Nio;
using Java.IO;
#endif

namespace flash.display
{
	public partial class BitmapData
	{

#if PLATFORM_MONOMAC || PLATFORM_MONOTOUCH
		private const string DEFAULT_FONT = "Verdana";
		private static Dictionary<string,bool> sHasFont = new Dictionary<string,bool> ();
#else
		// WHAT IS THE DEFAULT FONT FOR ANDROID?
#endif

		// Partial class implementation of draw() in c# to allow use of unsafe code..
		public unsafe void draw(IBitmapDrawable source, Matrix matrix = null, ColorTransform colorTransform = null, string blendMode = null, 
		                 Rectangle clipRect = null, Boolean smoothing = false) {

#if PLATFORM_MONOMAC || PLATFORM_MONOTOUCH

			if (source is flash.text.TextField)
			{
				flash.text.TextField tf = source as flash.text.TextField;
				flash.text.TextFormat format = tf.defaultTextFormat;

				// $$TODO figure out how to get rid of this extra data copy
				var sizeToDraw = (width * height)<<2;
				if(sizeToDraw==0)
					return;

				string fontName = format.font;
				float fontSize = (format.size is double) ? (float)(double)format.size : ((format.size is int) ? (float)(int)format.size : 10);

				// Check if the font is installed?
				bool hasFont = true;
				if (!sHasFont.TryGetValue(fontName, out hasFont)) {
					UIFont font = UIFont.FromName(fontName, 10);
					sHasFont[fontName] = hasFont = font != null;
					if (font != null) 
						font.Dispose();
				}
				if (!hasFont) {
					fontName = DEFAULT_FONT;
				}

				fixed (uint* data = mData) {

					using (CGBitmapContext context = new CGBitmapContext(new IntPtr(data), width, height, 8, 4 * width, 
					                                                     CGColorSpace.CreateDeviceRGB(), 
					                                                     CGImageAlphaInfo.PremultipliedLast))
					{
						uint tfColor = format.color != null ? (uint)(format.color) : 0;
						float r = (float)((tfColor >> 16) & 0xFF) / 255.0f;
						float g = (float)((tfColor >> 8) & 0xFF) / 255.0f;
						float b = (float)((tfColor >> 0) & 0xFF) / 255.0f;
						float a = (float)(tf.alpha);
						CGColor color = new CGColor(r, g, b, a);
						context.SetFillColor(color);
						context.SetStrokeColor(color);
						context.SelectFont(fontName, fontSize, CGTextEncoding.MacRoman);
						context.SetAllowsAntialiasing( ((tf.antiAliasType as string) == flash.text.AntiAliasType.ADVANCED) );

						double x = matrix.tx;
						double y = matrix.ty;

						// invert y because the CG origin is bottom,left
						y = height - tf.textHeight - y;

						// align text
						switch (format.align)
						{
							case TextFormatAlign.LEFT:
							// no adjustment required
							break;
							case TextFormatAlign.CENTER:
							// center x
							x += width / 2;
							x -= tf.textWidth / 2;
							break;
							case TextFormatAlign.RIGHT:
							// right justify x
							x += width;
							x -= tf.textWidth;
							break;
							default:
							throw new System.NotImplementedException();
						}

						// draw text
						context.ShowTextAtPoint((float)x, (float)y, tf.text );
					}

				}
			}
			else

#elif PLATFORM_MONODROID

			if ( source is flash.text.TextField )
			{
				var tf:flash.text.TextField = source as flash.text.TextField;
				var format:flash.text.TextFormat = tf.defaultTextFormat;

				// $$TODO figure out how to get rid of this extra data copy
				var data = new byte[width * height * 4];
				System.Buffer.BlockCopy(mData, 0, data, 0, data.Length);

				var config:Android.Graphics.Bitmap.Config = Android.Graphics.Bitmap.Config.Argb8888;
				var bitmap:Android.Graphics.Bitmap = Android.Graphics.Bitmap.CreateBitmap(width, height, config);

				var canvas:Canvas = new Canvas(bitmap);
				var x:Number = matrix.tx;
				var y:Number = matrix.ty;

				// invert y because the CG origin is bottom,left
				// y = height - tf.textHeight - y;

				// align text
				switch (format.align)
				{
					case TextFormatAlign.LEFT:
					// no adjustment required
					break;
					case TextFormatAlign.CENTER:
					// center x
					x += width / 2;
					x -= tf.textWidth / 2;
					break;
					case TextFormatAlign.RIGHT:
					// right justify x
					x += width;
					x -= tf.textWidth;
					break;
					default:
					throw new System.NotImplementedException();
				}

				var paint:Paint = new Paint(PaintFlags.AntiAlias);

				paint.Color = Color.Black;
				paint.TextSize = float(Number(format.size));
				paint.SetTypeface( Typeface.Create(format.font, TypefaceStyle.Normal) );
				paint.TextAlign = Paint.Align.Center;			

				canvas.DrawText(tf.text, float(x), float(y), paint);

				mData = new uint[ bitmap.Width * bitmap.Height ];
				var buffer = new int[ bitmap.Width * bitmap.Height ];
				bitmap.GetPixels(buffer, 0, width, 0, 0, width, height);

				for (var i:int = 0; i < buffer.Length; i++)
				{

					mData[i] = uint(buffer[i]);
				}
			}

			else
#endif

			{
				_root.trace_fn.trace("NotImplementedWarning: BitmapData.draw()");
			}
		}

	}
}

