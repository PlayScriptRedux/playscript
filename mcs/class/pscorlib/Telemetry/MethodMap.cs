using System;
using System.Collections.Generic;
using Amf;

namespace Telemetry
{
	// method map for translating addresses to symbols to unique ids
	public class MethodMap
	{
		public MethodMap(SymbolTable symbols)
		{
			// set symbol table
			mSymbols = symbols;

			// this must be method 1
			AllocMethodId("global$init");
		}

		public uint GetUnknownMethodId()
		{
			if (mUnknownMethodId == 0) {
				mUnknownMethodId = AllocMethodId("app/unknown");
			}
			return mUnknownMethodId;
		}

		public uint GetMethodId(IntPtr addr, bool storeInAddressCache = true)
		{
			// first try address lookup, incase this address has been seen before
			uint methodId;
			if (mAddressToMethodId.TryGetValue(addr, out methodId)) {
				return methodId;
			}

			// get symbol index from symbol table
			int symbolIndex = mSymbols.GetSymbolIndexFromAddress(addr);
			if (symbolIndex >= 0) {
				// lookup method id from symbol index
				if (!mSymbolIndexToMethodId.TryGetValue(symbolIndex, out methodId)) {
					// haven't seen this symbol before
					// get name of symbol
					string name;
					string imageName;
					if (mSymbols.GetSymbolName(symbolIndex, out name, out imageName)) {
						// construct full method name from symbol info
						if (name[0] == '_') {
							name = name.Substring(1);
						}
						// construct method name
						string methodName = imageName + "/" + name;
						// allocate method id from method name
						methodId = AllocMethodId(methodName);
					} else {
						methodId = GetUnknownMethodId();
					}

					// store method id in our table for this symbol
					mSymbolIndexToMethodId.Add(symbolIndex, methodId);
				}
			} else {
				methodId = GetUnknownMethodId();
			}

			if (storeInAddressCache) {
				// add to address lookup for next time
				mAddressToMethodId.Add(addr, methodId);
			}
			return methodId;
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

		public void Write()
		{
			// write method name byte array if we have data in it
			if (mMethodNames.length > 0) {
				// write sampler method names
				Session.WriteValueImmediate(sNameSamplerMethodNameMap, mMethodNames);
				mMethodNames.clear();
			}
		}

		#region Private
		private uint AllocMethodId(string name)
		{
			// allocate method id
			uint id = mNextMethodId++;
			// write method name to method name byte array we send to the server
			mMethodNames.writeUTFBytes(name);
			mMethodNames.writeByte(0);
			return id;
		}

		// symbol table to use
		private readonly 							SymbolTable 		    mSymbols;
		// map from address to method id
		private readonly Dictionary<IntPtr, uint>	mAddressToMethodId = new Dictionary<IntPtr, uint>();
		// map from symbol index to method id
		private readonly Dictionary<int, uint>		mSymbolIndexToMethodId = new Dictionary<int, uint>();
		// new method names that need to be sent
		private readonly flash.utils.ByteArray		mMethodNames = new flash.utils.ByteArray();
		// next method id to allocate
		private uint								mNextMethodId = 1;
		// special method id for unknown methods
		private uint								mUnknownMethodId = 0;

		private static readonly Amf3String sNameSamplerMethodNameMap = new Amf3String(".sampler.methodNameMap");
		#endregion
	}
}

