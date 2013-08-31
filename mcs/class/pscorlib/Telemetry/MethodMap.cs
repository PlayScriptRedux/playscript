using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using Amf;

namespace Telemetry
{
	// method map for translating addresses to symbols to unique ids
	public class MethodMap
	{
		// retrieves method id from provided code address
		public uint GetMethodId(IntPtr addr, bool storeInCache = true)
		{
			uint id;
			// lookup method id from address first
			if (mAddrToId.TryGetValue(addr, out id)) {
				return id;
			}

			// lookup symbol using dladdr
			var symbol = new Dl_info();
			dladdr(addr, ref symbol);

			// get address of symbol name
			IntPtr namePtr = symbol.dli_sname;
			// lookup method id from symbol name
			if (!mNameToId.TryGetValue(namePtr, out id)) {

				// not found, allocate a new id
				id = (uint)(mNameToId.Count+1);

				// get symbol name as string
				var name = Marshal.PtrToStringAnsi(namePtr);
				if (name == null) {
					name = "<null>";
				}

				// construct name with class
				name = "app/" + name;

				// write method name
				mMethodNames.writeUTFBytes(name);
				mMethodNames.writeByte(0);

				// add symbol name to method id
				mNameToId.Add(namePtr, id);
			}

			if (storeInCache) {
				// keep address to method id mapping
				// we shouldn't do this from leaf functions, only from callsites
				mAddrToId.Add(addr, id);
			}
			return id;
		}

		// resolves a whole callstack to an array of method ids
		public uint[] GetCallStack(IntPtr[] data, int offset, int count)
		{
			// lookup address to method ids
			uint[] callstack = new uint[count];
			for (int i=0; i < count; i++) {
				callstack[i] = GetMethodId(data[offset++], true );
			}
			return callstack;
		}

		public int GetCallStackId()
		{
			//TODO: this should get a stack id for the current stack frame
			return 0;
		}

		public void Write(Log log)
		{
			// write method name byte array if we have data in it
			if (mMethodNames.length > 0) {
				// write sampler method names
				var writer = log.WriteValueHeader(sNameSamplerMethodNameMap);
				writer.Write(mMethodNames);
				mMethodNames.clear();
			}
		}

		#region External
		struct Dl_info
		{
			public IntPtr            dli_fname;     /* Pathname of shared object */
			public IntPtr            dli_fbase;     /* Base address of shared object */
			public IntPtr            dli_sname;     /* Name of nearest symbol */
			public IntPtr            dli_saddr;     /* Address of nearest symbol */
		};

		[DllImport ("__Internal", EntryPoint="dladdr")]
		extern static int dladdr(IntPtr addr, ref Dl_info info);
		#endregion

		#region Private
		// address to method id
		private readonly Dictionary<IntPtr, uint> mAddrToId = new Dictionary<IntPtr, uint>();
		// method name to method id
		private readonly Dictionary<IntPtr, uint> mNameToId = new Dictionary<IntPtr, uint>();
		// new method names that need to be sent
		private readonly flash.utils.ByteArray    mMethodNames = new flash.utils.ByteArray();

		private static readonly Amf3String sNameSamplerMethodNameMap = new Amf3String(".sampler.methodNameMap");
		#endregion
	}
}

