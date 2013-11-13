using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;

#if PLATFORM_MONOMAC
//using MonoMac.OpenGL;
using MonoMac.Foundation;
#elif PLATFORM_MONOTOUCH
using MonoTouch.Foundation;
using MonoTouch.UIKit;
//using OpenTK.Graphics;
//using OpenTK.Graphics.ES20;
#elif PLATFORM_MONODROID
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;
#endif

namespace PlayScript
{
	#if PLATFORM_MONOTOUCH
	// Somehow MonoTouch does not give access to the HardwareProperty
	// Code from this: http://stackoverflow.com/questions/10889695/how-do-i-get-the-ios-device-and-version-in-monotouch
	public class IOSDeviceHardware
	{
		public const string HardwareProperty = "hw.machine";

		public enum IOSHardware {
			iPhone,
			iPhone3G,
			iPhone3GS,
			iPhone4,
			iPhone4RevA,
			iPhone4CDMA,
			iPhone4S,
			iPhone5GSM,
			iPhone5CDMAGSM,
			iPodTouch1G,
			iPodTouch2G,
			iPodTouch3G,
			iPodTouch4G,
			iPodTouch5G,
			iPad,
			iPad3G,
			iPad2,
			iPad2GSM,
			iPad2CDMA,
			iPad2RevA,
			iPadMini,
			iPadMiniGSM,
			iPadMiniCDMAGSM,
			iPad3,
			iPad3CDMA,
			iPad3GSM,
			iPad4,
			iPad4GSM,
			iPad4CDMAGSM,
			iPhoneSimulator,
			iPhoneRetinaSimulator,
			iPadSimulator,
			iPadRetinaSimulator,
			Unknown
		}

		[DllImport(MonoTouch.Constants.SystemLibrary)]
		static internal extern int sysctlbyname([MarshalAs(UnmanagedType.LPStr)] string property, IntPtr output, IntPtr oldLen, IntPtr newp, uint newlen);

		private static IOSHardware? sCachedVersion;
		private static IOSHardware RetrieveVersion()
		{
			var pLen = Marshal.AllocHGlobal(sizeof(int));
			sysctlbyname(IOSDeviceHardware.HardwareProperty, IntPtr.Zero, pLen, IntPtr.Zero, 0);

			var length = Marshal.ReadInt32(pLen);

			if (length == 0) {
				Marshal.FreeHGlobal(pLen);

				return IOSHardware.Unknown;
			}

			var pStr = Marshal.AllocHGlobal(length);
			sysctlbyname(IOSDeviceHardware.HardwareProperty, pStr, pLen, IntPtr.Zero, 0);

			var hardwareStr = Marshal.PtrToStringAnsi(pStr);

			Marshal.FreeHGlobal(pLen);
			Marshal.FreeHGlobal(pStr);

			if (hardwareStr == "iPhone1,1") return IOSHardware.iPhone;
			if (hardwareStr == "iPhone1,2") return IOSHardware.iPhone3G;
			if (hardwareStr == "iPhone2,1") return IOSHardware.iPhone3GS;
			if (hardwareStr == "iPhone3,1") return IOSHardware.iPhone4;
			if (hardwareStr == "iPhone3,2") return IOSHardware.iPhone4RevA;
			if (hardwareStr == "iPhone3,3") return IOSHardware.iPhone4CDMA;
			if (hardwareStr == "iPhone4,1") return IOSHardware.iPhone4S;
			if (hardwareStr == "iPhone5,1") return IOSHardware.iPhone5GSM;
			if (hardwareStr == "iPhone5,2") return IOSHardware.iPhone5CDMAGSM;

			if (hardwareStr == "iPad1,1") return IOSHardware.iPad;
			if (hardwareStr == "iPad1,2") return IOSHardware.iPad3G;
			if (hardwareStr == "iPad2,1") return IOSHardware.iPad2;
			if (hardwareStr == "iPad2,2") return IOSHardware.iPad2GSM;
			if (hardwareStr == "iPad2,3") return IOSHardware.iPad2CDMA;
			if (hardwareStr == "iPad2,4") return IOSHardware.iPad2RevA;
			if (hardwareStr == "iPad2,5") return IOSHardware.iPadMini;
			if (hardwareStr == "iPad2,6") return IOSHardware.iPadMiniGSM;
			if (hardwareStr == "iPad2,7") return IOSHardware.iPadMiniCDMAGSM;
			if (hardwareStr == "iPad3,1") return IOSHardware.iPad3;
			if (hardwareStr == "iPad3,2") return IOSHardware.iPad3CDMA;
			if (hardwareStr == "iPad3,3") return IOSHardware.iPad3GSM;
			if (hardwareStr == "iPad3,4") return IOSHardware.iPad4;
			if (hardwareStr == "iPad3,5") return IOSHardware.iPad4GSM;
			if (hardwareStr == "iPad3,6") return IOSHardware.iPad4CDMAGSM;

			if (hardwareStr == "iPod1,1") return IOSHardware.iPodTouch1G;
			if (hardwareStr == "iPod2,1") return IOSHardware.iPodTouch2G;
			if (hardwareStr == "iPod3,1") return IOSHardware.iPodTouch3G;
			if (hardwareStr == "iPod4,1") return IOSHardware.iPodTouch4G;
			if (hardwareStr == "iPod5,1") return IOSHardware.iPodTouch5G;

			if (hardwareStr == "i386" || hardwareStr=="x86_64")
			{
				if (UIDevice.CurrentDevice.Model.Contains("iPhone"))
				{
					return IOSHardware.iPhoneSimulator;
				}
				else
				{
					return IOSHardware.iPadSimulator;
				}
			}

			return IOSHardware.Unknown;
		}

		public static IOSHardware Version {
			get {
				if (sCachedVersion.HasValue == false) {
					sCachedVersion = RetrieveVersion();
				}
				return sCachedVersion.Value;
			}
		}
	}

	#endif
}

