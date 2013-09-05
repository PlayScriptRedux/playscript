using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using uint32_t = System.UInt32;
using int32_t = System.Int32;
using uint16_t = System.UInt16;
using int16_t = System.Int16;
using uint8_t = System.Byte;
using int8_t = System.SByte;
using Address = System.UInt32;

namespace Telemetry
{
	// method map for translating addresses to symbols to unique ids
	internal class SymbolTable
	{
		public SymbolTable()
		{
			try {
				BuildSymbolLookup();
			} catch {
				// could not build symbol information, no symbols available
				Clear();
			}
		}

		public void Clear()
		{
			mImages = null;
			mSymbolCount = 0;
			mSymbolAddressTable = null;
			mSymbolIdTable = null;
		}

		public int GetSymbolIndexFromAddress(Address addr)
		{
			return FindSymbolFromAddress(addr);
		}

		public bool GetSymbolName(int symbolIndex, out string name, out int imageIndex, out string imageName)
		{
			imageIndex = 0;
			name = imageName = null;
			// check symbol range
			if (symbolIndex < 0 || symbolIndex >= mSymbolCount) {
				return false;
			}

			int symbolId = mSymbolIdTable[symbolIndex];
			// decompose symbol id into imageindex and symbolindex
			imageIndex = (symbolId >> ImageIndexShift);
			int imageSymbolIndex = symbolId & ((1 << ImageIndexShift) - 1);
			// get symbol name
			name = mImages[imageIndex].GetSymbolName(imageSymbolIndex);
			if (name == null) {
				return false;
			}
			// get image name
			imageName = mImages[imageIndex].Name;
			return true;
		}

		#region External
		struct Dl_info
		{
			public IntPtr            dli_fname;     /* Pathname of shared object */
			public IntPtr            dli_fbase;     /* Base address of shared object */
			public IntPtr            dli_sname;     /* Name of nearest symbol */
			public IntPtr            dli_saddr;     /* Address of nearest symbol */
		};

		const uint32_t MH_MAGIC = 0xfeedface;	/* the mach magic number */
		const uint32_t MH_CIGAM = 0xcefaedfe;	/* NXSwapInt(MH_MAGIC) */
		const uint32_t MH_MAGIC_64 = 0xfeedfacf; /* the 64-bit mach magic number */
		const uint32_t MH_CIGAM_64 = 0xcffaedfe; /* NXSwapInt(MH_MAGIC_64) */

		// header for all mach binaries
		struct mach_header {
			public uint32_t    magic;        /* mach magic number identifier */
			public uint32_t    cputype;    /* cpu specifier */
			public uint32_t    cpusubtype;    /* machine specifier */
			public uint32_t    filetype;    /* type of file */
			public uint32_t    ncmds;        /* number of load commands */
			public uint32_t    sizeofcmds;    /* the size of all the load commands */
			public uint32_t    flags;        /* flags */
		}

		const int LC_SEGMENT = 0x1;   /* segment of this file to be mapped */
		const int LC_SYMTAB = 0x2;    /* link-edit stab symbol table info */

		// this is the 'base class' of all commands (segment, symtab)
		struct load_command {
			public uint32_t cmd;        /* type of load command */
			public uint32_t cmdsize;    /* total size of command in bytes */
		};

		[StructLayout (LayoutKind.Sequential)]
		unsafe struct segment_command { /* for 32-bit architectures */
			public uint32_t    cmd;        /* LC_SEGMENT */
			public uint32_t    cmdsize;    /* includes sizeof section structs */
			public fixed byte  segname_bytes[16];    /* segment name */
			public uint32_t    vmaddr;        /* memory address of this segment */
			public uint32_t    vmsize;        /* memory size of this segment */
			public uint32_t    fileoff;    /* file offset of this segment */
			public uint32_t    filesize;    /* amount to map from the file */
			public int32_t     maxprot;    /* maximum VM protection */
			public int32_t     initprot;    /* initial VM protection */
			public uint32_t    nsects;        /* number of sections in segment */
			public uint32_t    flags;        /* flags */

			public unsafe string segname
			{
				get {
					fixed (byte* segnamePtr = segname_bytes) {
						return new string((sbyte*)segnamePtr);
					}
				}
			}
		};

		struct symtab_command {
			public uint32_t    cmd;        /* LC_SYMTAB */
			public uint32_t    cmdsize;    /* sizeof(struct symtab_command) */
			public uint32_t    symoff;        /* symbol table offset */
			public uint32_t    nsyms;        /* number of symbol table entries */
			public uint32_t    stroff;        /* string table offset */
			public uint32_t    strsize;    /* string table size in bytes */
		};

		struct nlist {
			public int32_t n_strx;    /* index into the string table */
			public uint8_t n_type;        /* type flag, see below */
			public uint8_t n_sect;        /* section number or NO_SECT */
			public int16_t n_desc;        /* see <mach-o/stab.h> */
			public uint32_t n_value;    /* value of this symbol (or stab offset) */
		};


		[DllImport ("__Internal", EntryPoint="dladdr")]
		extern static int dladdr(IntPtr addr, ref Dl_info info);

		//        extern uint32_t                    _dyld_image_count(void)                              __OSX_AVAILABLE_STARTING(__MAC_10_1, __IPHONE_2_0);
		[DllImport ("__Internal", EntryPoint="_dyld_image_count")]
		extern static uint _dyld_image_count();

		//        extern const struct mach_header*   _dyld_get_image_header(uint32_t image_index)         __OSX_AVAILABLE_STARTING(__MAC_10_1, __IPHONE_2_0);
		[DllImport ("__Internal", EntryPoint="_dyld_get_image_header")]
		extern static IntPtr _dyld_get_image_header(uint image_index);

		//        extern intptr_t                    _dyld_get_image_vmaddr_slide(uint32_t image_index)   __OSX_AVAILABLE_STARTING(__MAC_10_1, __IPHONE_2_0);
		[DllImport ("__Internal", EntryPoint="_dyld_get_image_vmaddr_slide")]
		extern static IntPtr _dyld_get_image_vmaddr_slide(uint image_index);

		//        extern const char*                 _dyld_get_image_name(uint32_t image_index)           __OSX_AVAILABLE_STARTING(__MAC_10_1, __IPHONE_2_0);
		[DllImport ("__Internal", EntryPoint="_dyld_get_image_name")]
		extern static IntPtr _dyld_get_image_name(uint image_index);
		#endregion

		#region Private

		enum SpecialSymbol
		{
			SegmentStart,
			SegmentEnd,
			Total
		};

		struct ImageSymbolInfo
		{
			public uint            Index;
			public string 		   FullName;
			public string 		   Name;
			public IntPtr 		   Slide;
			public segment_command LinkEdit;
			public symtab_command  SymbolTable;

			public unsafe string GetSymbolName(int symbolIndex)
			{
				// check symbol range
				if (symbolIndex < 0) {
					return null;
				}

				if (symbolIndex >= SymbolTable.nsyms) {
					// handle special symbols
					SpecialSymbol specialSymbol = (SpecialSymbol)(symbolIndex - (int)SymbolTable.nsyms);
					switch (specialSymbol) {
						case SpecialSymbol.SegmentStart:
							return "_start";
						case SpecialSymbol.SegmentEnd:
							return null;
						default:
							// out of range?
							return null;
					}
				}

				IntPtr baseaddr = Slide + (int)(LinkEdit.vmaddr - LinkEdit.fileoff);
				IntPtr straddr = baseaddr + (int)SymbolTable.stroff;
				IntPtr symaddr = baseaddr + (int)SymbolTable.symoff;
				nlist n = (nlist)Marshal.PtrToStructure (symaddr + symbolIndex * sizeof(nlist), typeof(nlist));
				var symbolName =  Marshal.PtrToStringAnsi (straddr + n.n_strx);
				return symbolName;
			}
		};

		private const int ImageIndexShift = 22;

		private void AddSymbol(Address address, uint imageIndex, uint symbolIndex)
		{
			// store info in symbol table
			mSymbolAddressTable[mSymbolCount] = address;
			mSymbolIdTable[mSymbolCount]      = (int)((imageIndex << ImageIndexShift) | symbolIndex);
			mSymbolCount++;
		}

		private unsafe void AddImageSymbols(ref ImageSymbolInfo image)
		{
			IntPtr baseaddr = image.Slide + (int)(image.LinkEdit.vmaddr - image.LinkEdit.fileoff);
			IntPtr symaddr = baseaddr + (int)image.SymbolTable.symoff;
			for (int symbolIndex=0; symbolIndex < image.SymbolTable.nsyms; symbolIndex++) {
				nlist n = (nlist)Marshal.PtrToStructure (symaddr + symbolIndex * sizeof(nlist), typeof(nlist));
				// see if this is a valid named symbol using magic flags
				if ((n.n_value != 0) && (n.n_type&0xE0) == 0 && (n.n_type&0xE) == 0xE) {
					// get address of symbol
					IntPtr address = image.Slide + (int)n.n_value;
					AddSymbol((Address)address, image.Index, (uint)symbolIndex);
				}
			}
		}

		private unsafe void AddImageSectionsAsSymbols(ref ImageSymbolInfo image)
		{
			// get image header
			IntPtr ptr = _dyld_get_image_header (image.Index);
			var hdr = (mach_header)Marshal.PtrToStructure (ptr, typeof(mach_header));
			// check header magic number
			if (hdr.magic != MH_MAGIC) {
				// unsupported header type
				return;
			}
			// get pointer to first load command
			ptr += sizeof(mach_header);
			// parse image load commands
			for (int j=0; j < hdr.ncmds; j++) {
				// get load_command
				var lc = (load_command)Marshal.PtrToStructure (ptr, typeof(load_command));
				if (lc.cmd == LC_SEGMENT) {
					var segment = (segment_command)Marshal.PtrToStructure (ptr, typeof(segment_command));

					// get segment start and end
					Address segmentStart = (Address)(image.Slide + (int)segment.vmaddr);
					Address segmentEnd   = segmentStart + segment.filesize;

					// mark the start and end of this image using special symbols
					AddSymbol(segmentStart, image.Index, image.SymbolTable.nsyms + (uint)SpecialSymbol.SegmentStart);
					AddSymbol(segmentEnd, image.Index, image.SymbolTable.nsyms + (uint)SpecialSymbol.SegmentEnd);
				} 
				// next command
				ptr += (int)lc.cmdsize;
			}
		}


		private unsafe void GetImageSymbolInfo(uint imageIndex, ref ImageSymbolInfo image)
		{
			// get image index
			image.Index = imageIndex;
			// get image name
			image.FullName = Marshal.PtrToStringAnsi (_dyld_get_image_name(imageIndex));
			/// get short name
			image.Name = System.IO.Path.GetFileNameWithoutExtension(image.FullName);
			// get image 'slide'
			image.Slide = _dyld_get_image_vmaddr_slide (imageIndex);

			// get image header
			IntPtr ptr = _dyld_get_image_header (imageIndex);
			var hdr = (mach_header)Marshal.PtrToStructure (ptr, typeof(mach_header));
			// check header magic number
			if (hdr.magic != MH_MAGIC) {
				// unsupported header type
				return;
			}

			// get pointer to first load command
			ptr += sizeof(mach_header);
			// parse image load commands
			for (int j=0; j < hdr.ncmds; j++) {
				// get load_command
				var lc = (load_command)Marshal.PtrToStructure (ptr, typeof(load_command));
				if (lc.cmd == LC_SEGMENT) {
					var segment = (segment_command)Marshal.PtrToStructure (ptr, typeof(segment_command));
					// find linkedit segment
					if (segment.segname.Contains ("LINKEDIT")) {
						image.LinkEdit = segment;
					}
				} else if (lc.cmd == LC_SYMTAB) {
					// symtab command 
					image.SymbolTable = (symtab_command)Marshal.PtrToStructure (ptr, typeof(symtab_command));
				}
				// next command
				ptr += (int)lc.cmdsize;
			}
		}

		private void BuildSymbolLookup()
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();

			uint totalSymbols = 0;
			// build information on all loaded images
			mImages = new ImageSymbolInfo[_dyld_image_count ()];
			for (uint i=0; i < mImages.Length; i++) {
				GetImageSymbolInfo(i, ref mImages[i]);
				totalSymbols += mImages[i].SymbolTable.nsyms + (uint)SpecialSymbol.Total;
			}

			// create symbol lookup table
			mSymbolCount = 0;
			mSymbolAddressTable = new Address[totalSymbols];
			mSymbolIdTable      = new int[totalSymbols];

			// add symbol info from all images
			for (uint i=0; i < mImages.Length; i++) {
//				int count = mSymbolCount;
				AddImageSymbols(ref mImages[i]);
				AddImageSectionsAsSymbols(ref mImages[i]);
//				Console.WriteLine("Telemetry: Image[{0,3}] Symbols:{1,6} Name:{2}", i, mSymbolCount - count, mImages[i].Name);
			}

			// sort symbols by address
			Array.Sort (mSymbolAddressTable, mSymbolIdTable, 0, mSymbolCount);

			Console.WriteLine("Telemetry: Built symbol table (SymbolCount:{0} Time:{1})", mSymbolCount, sw.Elapsed);
		}


		private static int BinarySearch(Address[] table, int count, Address key)
		{
			// set start and end 
			int start = 0;
			int end   = count;
			for (;;) {
				// find midpoint
 				int mid = (start + end) >> 1;
				if (mid == end)
					break;

				// test key against range
				if (key < table[mid]) {
					end = mid;
				} else 	if (key > table[mid+1]) {
					start = (mid + 1);
				} else {
					// found it!
					return mid;
				}
			}

			if (key >= table[start] && key < table[start+1]) {
				return start;
			} else {
				// not found
				return -1;
			}
		}

		private static int LinearSearch(Address[] table, int count, Address key)
		{
			for (int i=0; i < (count-1); i++) {
				if (key < table[i]) {
					return (i-1);
				}
			}
			return -1;
		}

		private int FindSymbolFromAddress(Address address)
		{
			if (mSymbolAddressTable == null) {
				// lookup table hasnt been built
				return -1;
			}

			return BinarySearch(mSymbolAddressTable, mSymbolCount, (Address)address);
		}

		// array of parsed image infos
		private ImageSymbolInfo[]				mImages;
		// conut of symbols
		private int 							mSymbolCount;
		// the address of each symbol, sorted
		private Address[]						mSymbolAddressTable;
		// the identifier for each symbol
		//  it is the (image index << ImageIndexShift) | (symbol index);
		private int[]							mSymbolIdTable;
		#endregion
	}
}

