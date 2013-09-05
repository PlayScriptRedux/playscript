using System;
using System.Collections.Generic;
using Amf;
using Address = System.UInt32;

namespace Telemetry
{
	// method map for translating addresses to symbols to unique ids
	internal class MethodMap
	{
		// this bit is set on all method ids that are the top of stack
		// this means that callstacks should not extend beyond this point
		const uint TopOfStackFlag = ((uint)1 << 31);

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
#if PLATFORM_MONOTOUCH
				mUnknownMethodId = AllocMethodId("$/<unknown>");
#else
				mUnknownMethodId = AllocMethodId("$/<jit>");
#endif
			}
			return mUnknownMethodId;
		}

		public uint GetMethodId(Address addr, out bool isTopOfStack, bool storeInAddressCache = true)
		{
			// first try address lookup, incase this address has been seen before
			uint methodId;
			if (mAddressToMethodId.TryGetValue(addr, out methodId)) {
				// set terminal boolean
				isTopOfStack = (methodId & TopOfStackFlag) != 0;
				return methodId & ~TopOfStackFlag;
			}

			// get symbol index from symbol table
			int symbolIndex = mSymbols.GetSymbolIndexFromAddress(addr);
			if (symbolIndex >= 0) {
				// lookup method id from symbol index
				if (!mSymbolIndexToMethodId.TryGetValue(symbolIndex, out methodId)) {
					// haven't seen this symbol before
					// get name of symbol
					string name;
					int imageIndex;
					string imageName;
					if (mSymbols.GetSymbolName(symbolIndex, out name, out imageIndex, out imageName)) {
						// construct method from symbol info
						methodId = CreateMethodId(name, imageIndex, imageName);
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
			// set terminal boolean
			isTopOfStack = (methodId & TopOfStackFlag) != 0;
			return methodId & ~TopOfStackFlag;
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

		private uint CreateMethodId(string name, int imageIndex, string imageName)
		{
			// construct full method name from symbol info
			if (name[0] == '_') {
				name = name.Substring(1);
			}

			// this is set to true for 'top of stack' methods
			bool isTopOfStack = false;

#if PLATFORM_MONOMAC
			// handle top of stack
			if (name == "NSApplicationMain") {
				isTopOfStack = true;
			}
#elif PLATFORM_MONOTOUCH
			// handle top of stack
			if (name == "UIApplicationMain" || name.StartsWith("PlayScript_Application_")) {
				isTopOfStack = true;
			}
#endif

			if (imageIndex > 0) {
				// classify as a built-in library
				imageName = "flash." + imageName;
			}

			// class is global by default
			string className = "$";

			// handle obj-c symbols
			if ((name[0] == '-'  || name[0] == '+') && name[1] == '[') {
				int spaceIndex = name.IndexOf(' ');
				int endIndex = name.LastIndexOf(']');
				if (spaceIndex > 0) {
					className = name.Substring(2, spaceIndex - 2);
					if (endIndex > 0) {
						name = name.Substring(spaceIndex + 1, endIndex - spaceIndex - 1);
					} else {
						name = name.Substring(spaceIndex + 1);
					}
				}
			} else {
				int index = name.IndexOf("__");
				if (index > 0) {
					className = name.Substring(0, index);
					name = name.Substring(index + 2);
				}
			}

			// construct method name
			string methodName = imageName + "::" + className + "/" + name;

			// allocate method id from method name
			uint methodId = AllocMethodId(methodName);

			if (isTopOfStack) {
				// set flag on method id
				methodId |= TopOfStackFlag;
			}
			return methodId;
		}


		// symbol table to use
		private readonly 							SymbolTable 		    mSymbols;
		// map from address to method id
		private readonly Dictionary<Address, uint>	mAddressToMethodId = new Dictionary<Address, uint>();
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

