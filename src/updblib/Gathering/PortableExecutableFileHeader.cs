using System;
using System.IO;

namespace UPDB.Gathering
{
    internal sealed class PortableExecutableFileHeader
    {
        public bool IsValid { get; private set; }

        public ImageDosHeader DosHeader { get; private set; }
        public ImageNtHeaders NtHeader { get; private set; }

        private PortableExecutableFileHeader()
        { }

        public static PortableExecutableFileHeader Read(string peFilePath)
        {
            var peFileHeader = new PortableExecutableFileHeader()
            {
                IsValid = false,
                DosHeader = null,
                NtHeader = null,
            };

            // Read DOS header.
            var dosHeader = ImageDosHeader.Read(peFilePath);
            if (!dosHeader.IsValid)
            {
                return peFileHeader;
            }

            // Read NT header.
            var ntHeader = ImageNtHeaders.Read(peFilePath, dosHeader.e_lfanew);
            if (!ntHeader.IsValid)
            {
                return peFileHeader;
            }

            peFileHeader.IsValid = true;
            peFileHeader.DosHeader = dosHeader;
            peFileHeader.NtHeader = ntHeader;
            return peFileHeader;
        }
    }

    internal sealed class ImageDosHeader
    {
        // From winnt.h in SDK.
        public const ushort ImageDosMagic = 0x5a4d;  // MZ

        public bool IsValid { get; private set; }

        public ushort e_magic { get; private set; }       // Magic number
        public ushort e_cblp { get; private set; }        // Bytes on last page of file
        public ushort e_cp { get; private set; }          // Pages in file
        public ushort e_crlc { get; private set; }        // Relocations
        public ushort e_cparhdr { get; private set; }     // Size of header in paragraphs
        public ushort e_minalloc { get; private set; }    // Minimum extra paragraphs needed
        public ushort e_maxalloc { get; private set; }    // Maximum extra paragraphs needed
        public ushort e_ss { get; private set; }          // Initial (relative) SS value
        public ushort e_sp { get; private set; }          // Initial SP value
        public ushort e_csum { get; private set; }        // Checksum
        public ushort e_ip { get; private set; }          // Initial IP value
        public ushort e_cs { get; private set; }          // Initial (relative) CS value
        public ushort e_lfarlc { get; private set; }      // File address of relocation table
        public ushort e_ovno { get; private set; }        // Overlay number
        public ushort[] e_res { get; private set; }       // Reserved words
        public ushort e_oemid { get; private set; }       // OEM identifier (for e_oeminfo)
        public ushort e_oeminfo { get; private set; }     // OEM information; e_oemid specific
        public ushort[] e_res2 { get; private set; }      // Reserved words
        public int e_lfanew { get; private set; }         // File address of new exe header

        private ImageDosHeader()
        { }

        internal static ImageDosHeader Read(string filePath)
        {
            var dosHeader = new ImageDosHeader()
            {
                IsValid = false,
            };

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(stream))
            {
                var e_magic = reader.ReadUInt16();
                if (e_magic != ImageDosMagic)
                {
                    return dosHeader;
                }

                dosHeader.e_magic = e_magic;
                dosHeader.e_cblp = reader.ReadUInt16();
                dosHeader.e_cp = reader.ReadUInt16();
                dosHeader.e_crlc = reader.ReadUInt16();
                dosHeader.e_cparhdr = reader.ReadUInt16();
                dosHeader.e_minalloc = reader.ReadUInt16();
                dosHeader.e_maxalloc = reader.ReadUInt16();
                dosHeader.e_ss = reader.ReadUInt16();
                dosHeader.e_sp = reader.ReadUInt16();
                dosHeader.e_csum = reader.ReadUInt16();
                dosHeader.e_ip = reader.ReadUInt16();
                dosHeader.e_cs = reader.ReadUInt16();
                dosHeader.e_lfarlc = reader.ReadUInt16();
                dosHeader.e_ovno = reader.ReadUInt16();

                dosHeader.e_res = new ushort[4];
                for (int i = 0; i < dosHeader.e_res.Length; i++)
                {
                    dosHeader.e_res[i] = reader.ReadUInt16();
                }

                dosHeader.e_oemid = reader.ReadUInt16();
                dosHeader.e_oeminfo = reader.ReadUInt16();

                dosHeader.e_res2 = new ushort[10];
                for (int i = 0; i < dosHeader.e_res2.Length; i++)
                {
                    dosHeader.e_res2[i] = reader.ReadUInt16();
                }

                dosHeader.e_lfanew = reader.ReadInt32();
            }

            dosHeader.IsValid = true;
            return dosHeader;
        }
    }

    internal sealed class ImageNtHeaders
    {
        // From winnt.h in SDK.
        public const uint ImageNtSignature = 0x00004550;  // PE\0\0

        public bool IsValid { get; private set; }

        public uint Signature { get; private set; }
        public ImageFileHeader FileHeader { get; private set; }
        public ImageOptionalHeader OptionalHeader { get; private set; }

        private ImageNtHeaders()
        { }

        internal static ImageNtHeaders Read(string filePath, int ntHeaderOffset)
        {
            var ntHeader = new ImageNtHeaders()
            {
                IsValid = false,
                Signature = 0,
                FileHeader = null,
                OptionalHeader = null,
            };

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(stream))
            {
                // Read sigunature of NT header.
                reader.BaseStream.Seek(ntHeaderOffset, SeekOrigin.Begin);  // Set position to the NT header.
                var signature = reader.ReadUInt32();
                if (signature != ImageNtSignature)
                {
                    return ntHeader;
                }

                // Read file header.
                var fileHeader = ImageFileHeader.Read(reader);

                // Read optional header magic.
                var currentPosition = reader.BaseStream.Position;  // Save current position.
                var optionalHeaderMagic = reader.ReadUInt16();
                reader.BaseStream.Seek(currentPosition, SeekOrigin.Begin);  // Restore position.

                ImageOptionalHeader optionalHeader;
                if (optionalHeaderMagic == NativeImageOptionalHeader.ImageNtOptionalHeader32Magic)
                {
                    // Read optinal header as 32-bit.
                    optionalHeader = ImageOptionalHeader32.Read(reader);
                }
                else if (optionalHeaderMagic == NativeImageOptionalHeader.ImageNtOptionalHeader64Magic)
                {
                    // Read optinal header as 64-bit.
                    optionalHeader = ImageOptionalHeader64.Read(reader);
                }
                else
                {
                    return ntHeader;
                }

                ntHeader.IsValid = true;
                ntHeader.Signature = signature;
                ntHeader.FileHeader = fileHeader;
                ntHeader.OptionalHeader = optionalHeader;
                return ntHeader;
            }
        }
    }

    internal sealed class ImageFileHeader
    {
        internal sealed class NativeImageFileHeader
        {
            // From winnt.h in SDK.
            public const ushort ImageFileMachineI386 = 0x014c;   // Intel 386.
            public const ushort ImageFileMachineAmd64 = 0x8664;  // AMD64 (K8)
            public const ushort ImageFileMachineArm64 = 0xaa64;  // ARM64 Little-Endian
            public const ushort ImageFileMachineIa64 = 0x0200;   // Intel 64

            public ushort Machine { get; private set; }
            public ushort NumberOfSections { get; private set; }
            public uint TimeDateStamp { get; private set; }  // The number of seconds elapsed since 00:00:00, January 1, 1970, UTC
            public uint PointerToSymbolTable { get; private set; }
            public uint NumberOfSymbols { get; private set; }
            public ushort SizeOfOptionalHeader { get; private set; }
            public ushort Characteristics { get; private set; }

            private NativeImageFileHeader()
            { }

            internal static NativeImageFileHeader Read(BinaryReader reader)
            {
                return new NativeImageFileHeader()
                {
                    Machine = reader.ReadUInt16(),
                    NumberOfSections = reader.ReadUInt16(),
                    TimeDateStamp = reader.ReadUInt32(),
                    PointerToSymbolTable = reader.ReadUInt32(),
                    NumberOfSymbols = reader.ReadUInt32(),
                    SizeOfOptionalHeader = reader.ReadUInt16(),
                    Characteristics = reader.ReadUInt16(),
                };
            }
        }

        public enum MachineType : ushort
        {
            I386 = NativeImageFileHeader.ImageFileMachineI386,
            AMD64 = NativeImageFileHeader.ImageFileMachineAmd64,
            ARM = NativeImageFileHeader.ImageFileMachineArm64,
            IA64 = NativeImageFileHeader.ImageFileMachineIa64,
        }

        [Flags]
        public enum CharacteristicsFlag : ushort
        {
            RelocsStripped = 0x0001,        // Relocation info stripped from file.
            ExecutableImage = 0x0002,       // File is executable  (i.e. no unresolved external references).
            LineNumsStripped = 0x0004,      // Line nunbers stripped from file.
            LocalSymsStripped = 0x0008,     // Local symbols stripped from file.
            AggresiveWsTrim = 0x0010,       // Aggressively trim working set
            LargeAddressAware = 0x0020,     // App can handle >2gb addresses
            BytesReversedLo = 0x0080,       // Bytes of machine word are reversed.
            Machine32bit = 0x0100,          // 32 bit word machine.
            DebugStripped = 0x0200,         // Debugging info stripped from file in .DBG file
            RemovableRunFromSwap = 0x0400,  // If Image is on removable media, copy and run from the swap file.
            NetRunFromSwap = 0x0800,        // If Image is on Net, copy and run from the swap file.
            System = 0x1000,                // System File.
            Dll = 0x2000,                   // File is a DLL.
            UpSystemOnly = 0x4000,          // File should only be run on a UP machine
            BytesReversedHi = 0x8000,       // Bytes of machine word are reversed.
        }

        public NativeImageFileHeader NativeHeader { get; private set; }
        public MachineType Machine { get; private set; }
        public ushort NumberOfSections { get; private set; }
        public DateTime TimeDateStamp { get; private set; }
        public uint PointerToSymbolTable { get; private set; }
        public uint NumberOfSymbols { get; private set; }
        public ushort SizeOfOptionalHeader { get; private set; }
        public CharacteristicsFlag Characteristics { get; private set; }

        private ImageFileHeader()
        { }

        internal static ImageFileHeader Read(BinaryReader reader)
        {
            // Read native image file header.
            var nativeHeader = NativeImageFileHeader.Read(reader);
            return new ImageFileHeader()
            {
                NativeHeader = nativeHeader,
                Machine = (MachineType)nativeHeader.Machine,
                NumberOfSections = nativeHeader.NumberOfSections,
                TimeDateStamp = (new DateTime(1970, 1, 1, 0, 0, 0)).AddSeconds(nativeHeader.TimeDateStamp),
                PointerToSymbolTable = nativeHeader.PointerToSymbolTable,
                NumberOfSymbols = nativeHeader.NumberOfSymbols,
                SizeOfOptionalHeader = nativeHeader.SizeOfOptionalHeader,
                Characteristics = (CharacteristicsFlag)nativeHeader.Characteristics,
            };
        }
    }

    public abstract class NativeImageOptionalHeader
    {
        public const ushort ImageNtOptionalHeader32Magic = 0x10b;
        public const ushort ImageNtOptionalHeader64Magic = 0x20b;
        public const ushort ImageRomOptionalHeaderMagic = 0x107;
    }

    public abstract class ImageOptionalHeader
    {
        public enum SubsystemType : ushort
        {
            Unknown = 0,                  // Unknown subsystem.
            Native = 1,                   // Image doesn't require a subsystem.
            WindowsGui = 2,               // Image runs in the Windows GUI subsystem.
            WindowsCui = 3,               // Image runs in the Windows character subsystem.
            Os2Cui = 5,                   // Image runs in the OS/2 character subsystem.
            PosixCui = 7,                 // Image runs in the Posix character subsystem.
            NativeWindows = 8,            // Image is a native Win9x driver.
            WindowsCeGui = 9,             // Image runs in the Windows CE subsystem.
            EfiApplication = 10,          //
            EfiBootServiceDriver = 11,    //
            EfiRuntimeDriver = 12,        //
            EfiRom = 13,                  //
            Xbox = 14,                    //
            WindowsBootApplication = 16,  //
            XboxCodeCatalog = 17,         //
        }

        [Flags]
        public enum DllCharacteristicsFlag : ushort
        {
            LibraryProcessInit = 0x0001,   // Reserved.
            LibraryProcessTerm = 0x0002,   // Reserved.
            LibraryThreadInit = 0x0004,    // Reserved.
            LibraryThreadTerm = 0x0008,    // Reserved.
            HighEntropyVa = 0x0020,        // Image can handle a high entropy 64-bit virtual address space.
            DynamicBase = 0x0040,          // DLL can move.
            ForceIntegrity = 0x0080,       // Code Integrity Image
            NxCompat = 0x0100,             // Image is NX compatible
            NoIsolation = 0x0200,          // Image understands isolation and doesn't want it
            NoSeh = 0x0400,                // Image does not use SEH.  No SE handler may reside in this image
            NoBind = 0x0800,               // Do not bind this image.
            Appcontainer = 0x1000,         // Image should execute in an AppContainer
            WdmDriver = 0x2000,            // Driver uses WDM model
            GuardCf = 0x4000,              // Image supports Control Flow Guard.
            TerminalServerAware = 0x8000,  //
        }
    }

    internal sealed class ImageOptionalHeader32 : ImageOptionalHeader
    {
        public sealed class NativeImageOptionalHeader32 : NativeImageOptionalHeader
        {
            public ushort Magic { get; private set; }
            public byte MajorLinkerVersion { get; private set; }
            public byte MinorLinkerVersion { get; private set; }
            public uint SizeOfCode { get; private set; }
            public uint SizeOfInitializedData { get; private set; }
            public uint SizeOfUninitializedData { get; private set; }
            public uint AddressOfEntryPoint { get; private set; }
            public uint BaseOfCode { get; private set; }
            public uint BaseOfData { get; private set; }
            public uint ImageBase { get; private set; }
            public uint SectionAlignment { get; private set; }
            public uint FileAlignment { get; private set; }
            public ushort MajorOperatingSystemVersion { get; private set; }
            public ushort MinorOperatingSystemVersion { get; private set; }
            public ushort MajorImageVersion { get; private set; }
            public ushort MinorImageVersion { get; private set; }
            public ushort MajorSubsystemVersion { get; private set; }
            public ushort MinorSubsystemVersion { get; private set; }
            public uint Win32VersionValue { get; private set; }
            public uint SizeOfImage { get; private set; }
            public uint SizeOfHeaders { get; private set; }
            public uint CheckSum { get; private set; }
            public ushort Subsystem { get; private set; }
            public ushort DllCharacteristics { get; private set; }
            public uint SizeOfStackReserve { get; private set; }
            public uint SizeOfStackCommit { get; private set; }
            public uint SizeOfHeapReserve { get; private set; }
            public uint SizeOfHeapCommit { get; private set; }
            public uint LoaderFlags { get; private set; }
            public uint NumberOfRvaAndSizes { get; private set; }
            //IMAGE_DATA_DIRECTORY DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];

            private NativeImageOptionalHeader32()
            { }

            internal static NativeImageOptionalHeader32 Read(BinaryReader reader)
            {
                return new NativeImageOptionalHeader32()
                {
                    // Standard fields.
                    Magic = reader.ReadUInt16(),
                    MajorLinkerVersion = reader.ReadByte(),
                    MinorLinkerVersion = reader.ReadByte(),
                    SizeOfCode = reader.ReadUInt32(),
                    SizeOfInitializedData = reader.ReadUInt32(),
                    SizeOfUninitializedData = reader.ReadUInt32(),
                    AddressOfEntryPoint = reader.ReadUInt32(),
                    BaseOfCode = reader.ReadUInt32(),
                    BaseOfData = reader.ReadUInt32(),

                    // NT additional fields.
                    ImageBase = reader.ReadUInt32(),
                    SectionAlignment = reader.ReadUInt32(),
                    FileAlignment = reader.ReadUInt32(),
                    MajorOperatingSystemVersion = reader.ReadUInt16(),
                    MinorOperatingSystemVersion = reader.ReadUInt16(),
                    MajorImageVersion = reader.ReadUInt16(),
                    MinorImageVersion = reader.ReadUInt16(),
                    MajorSubsystemVersion = reader.ReadUInt16(),
                    MinorSubsystemVersion = reader.ReadUInt16(),
                    Win32VersionValue = reader.ReadUInt32(),
                    SizeOfImage = reader.ReadUInt32(),
                    SizeOfHeaders = reader.ReadUInt32(),
                    CheckSum = reader.ReadUInt32(),
                    Subsystem = reader.ReadUInt16(),
                    DllCharacteristics = reader.ReadUInt16(),
                    SizeOfStackReserve = reader.ReadUInt32(),
                    SizeOfStackCommit = reader.ReadUInt32(),
                    SizeOfHeapReserve = reader.ReadUInt32(),
                    SizeOfHeapCommit = reader.ReadUInt32(),
                    LoaderFlags = reader.ReadUInt32(),
                    NumberOfRvaAndSizes = reader.ReadUInt32(),
                };
            }
        }

        public NativeImageOptionalHeader32 NativeHeader { get; private set; }

        public ushort Magic { get; private set; }
        public byte MajorLinkerVersion { get; private set; }
        public byte MinorLinkerVersion { get; private set; }
        public uint SizeOfCode { get; private set; }
        public uint SizeOfInitializedData { get; private set; }
        public uint SizeOfUninitializedData { get; private set; }
        public uint AddressOfEntryPoint { get; private set; }
        public uint BaseOfCode { get; private set; }
        public uint BaseOfData { get; private set; }
        public uint ImageBase { get; private set; }
        public uint SectionAlignment { get; private set; }
        public uint FileAlignment { get; private set; }
        public ushort MajorOperatingSystemVersion { get; private set; }
        public ushort MinorOperatingSystemVersion { get; private set; }
        public ushort MajorImageVersion { get; private set; }
        public ushort MinorImageVersion { get; private set; }
        public ushort MajorSubsystemVersion { get; private set; }
        public ushort MinorSubsystemVersion { get; private set; }
        public uint Win32VersionValue { get; private set; }
        public uint SizeOfImage { get; private set; }
        public uint SizeOfHeaders { get; private set; }
        public uint CheckSum { get; private set; }
        public SubsystemType Subsystem { get; private set; }
        public DllCharacteristicsFlag DllCharacteristics { get; private set; }
        public uint SizeOfStackReserve { get; private set; }
        public uint SizeOfStackCommit { get; private set; }
        public uint SizeOfHeapReserve { get; private set; }
        public uint SizeOfHeapCommit { get; private set; }
        public uint LoaderFlags { get; private set; }
        public uint NumberOfRvaAndSizes { get; private set; }

        private ImageOptionalHeader32()
        { }

        internal static ImageOptionalHeader32 Read(BinaryReader reader)
        {
            var nativeHeader = NativeImageOptionalHeader32.Read(reader);
            return new ImageOptionalHeader32()
            {
                NativeHeader = nativeHeader,

                // Standard fields.
                Magic = nativeHeader.Magic,
                MajorLinkerVersion = nativeHeader.MajorLinkerVersion,
                MinorLinkerVersion = nativeHeader.MinorLinkerVersion,
                SizeOfCode = nativeHeader.SizeOfCode,
                SizeOfInitializedData = nativeHeader.SizeOfInitializedData,
                SizeOfUninitializedData = nativeHeader.SizeOfUninitializedData,
                AddressOfEntryPoint = nativeHeader.AddressOfEntryPoint,
                BaseOfCode = nativeHeader.BaseOfCode,
                BaseOfData = nativeHeader.BaseOfData,

                // NT additional fields.
                ImageBase = nativeHeader.ImageBase,
                SectionAlignment = nativeHeader.SectionAlignment,
                FileAlignment = nativeHeader.FileAlignment,
                MajorOperatingSystemVersion = nativeHeader.MajorOperatingSystemVersion,
                MinorOperatingSystemVersion = nativeHeader.MinorOperatingSystemVersion,
                MajorImageVersion = nativeHeader.MajorImageVersion,
                MinorImageVersion = nativeHeader.MinorImageVersion,
                MajorSubsystemVersion = nativeHeader.MajorSubsystemVersion,
                MinorSubsystemVersion = nativeHeader.MinorSubsystemVersion,
                Win32VersionValue = nativeHeader.Win32VersionValue,
                SizeOfImage = nativeHeader.SizeOfImage,
                SizeOfHeaders = nativeHeader.SizeOfHeaders,
                CheckSum = nativeHeader.CheckSum,
                Subsystem = (SubsystemType)nativeHeader.Subsystem,
                DllCharacteristics = (DllCharacteristicsFlag)nativeHeader.DllCharacteristics,
                SizeOfStackReserve = nativeHeader.SizeOfStackReserve,
                SizeOfStackCommit = nativeHeader.SizeOfStackCommit,
                SizeOfHeapReserve = nativeHeader.SizeOfHeapReserve,
                SizeOfHeapCommit = nativeHeader.SizeOfHeapCommit,
                LoaderFlags = nativeHeader.LoaderFlags,
                NumberOfRvaAndSizes = nativeHeader.NumberOfRvaAndSizes,
            };
        }
    }

    internal sealed class ImageOptionalHeader64 : ImageOptionalHeader
    {
        public sealed class NativeImageOptionalHeader64 : NativeImageOptionalHeader
        {
            public ushort Magic { get; private set; }
            public byte MajorLinkerVersion { get; private set; }
            public byte MinorLinkerVersion { get; private set; }
            public uint SizeOfCode { get; private set; }
            public uint SizeOfInitializedData { get; private set; }
            public uint SizeOfUninitializedData { get; private set; }
            public uint AddressOfEntryPoint { get; private set; }
            public uint BaseOfCode { get; private set; }
            public uint BaseOfData { get; private set; }
            public ulong ImageBase { get; private set; }
            public uint SectionAlignment { get; private set; }
            public uint FileAlignment { get; private set; }
            public ushort MajorOperatingSystemVersion { get; private set; }
            public ushort MinorOperatingSystemVersion { get; private set; }
            public ushort MajorImageVersion { get; private set; }
            public ushort MinorImageVersion { get; private set; }
            public ushort MajorSubsystemVersion { get; private set; }
            public ushort MinorSubsystemVersion { get; private set; }
            public uint Win32VersionValue { get; private set; }
            public uint SizeOfImage { get; private set; }
            public uint SizeOfHeaders { get; private set; }
            public uint CheckSum { get; private set; }
            public ushort Subsystem { get; private set; }
            public ushort DllCharacteristics { get; private set; }
            public ulong SizeOfStackReserve { get; private set; }
            public ulong SizeOfStackCommit { get; private set; }
            public ulong SizeOfHeapReserve { get; private set; }
            public ulong SizeOfHeapCommit { get; private set; }
            public uint LoaderFlags { get; private set; }
            public uint NumberOfRvaAndSizes { get; private set; }
            //IMAGE_DATA_DIRECTORY DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];

            private NativeImageOptionalHeader64()
            { }

            internal static NativeImageOptionalHeader64 Read(BinaryReader reader)
            {
                return new NativeImageOptionalHeader64()
                {
                    // Standard fields.
                    Magic = reader.ReadUInt16(),
                    MajorLinkerVersion = reader.ReadByte(),
                    MinorLinkerVersion = reader.ReadByte(),
                    SizeOfCode = reader.ReadUInt32(),
                    SizeOfInitializedData = reader.ReadUInt32(),
                    SizeOfUninitializedData = reader.ReadUInt32(),
                    AddressOfEntryPoint = reader.ReadUInt32(),
                    BaseOfCode = reader.ReadUInt32(),
                    BaseOfData = reader.ReadUInt32(),

                    // NT additional fields.
                    ImageBase = reader.ReadUInt64(),
                    SectionAlignment = reader.ReadUInt32(),
                    FileAlignment = reader.ReadUInt32(),
                    MajorOperatingSystemVersion = reader.ReadUInt16(),
                    MinorOperatingSystemVersion = reader.ReadUInt16(),
                    MajorImageVersion = reader.ReadUInt16(),
                    MinorImageVersion = reader.ReadUInt16(),
                    MajorSubsystemVersion = reader.ReadUInt16(),
                    MinorSubsystemVersion = reader.ReadUInt16(),
                    Win32VersionValue = reader.ReadUInt32(),
                    SizeOfImage = reader.ReadUInt32(),
                    SizeOfHeaders = reader.ReadUInt32(),
                    CheckSum = reader.ReadUInt32(),
                    Subsystem = reader.ReadUInt16(),
                    DllCharacteristics = reader.ReadUInt16(),
                    SizeOfStackReserve = reader.ReadUInt64(),
                    SizeOfStackCommit = reader.ReadUInt64(),
                    SizeOfHeapReserve = reader.ReadUInt64(),
                    SizeOfHeapCommit = reader.ReadUInt64(),
                    LoaderFlags = reader.ReadUInt32(),
                    NumberOfRvaAndSizes = reader.ReadUInt32(),
                };
            }
        }

        public NativeImageOptionalHeader64 NativeHeader { get; private set; }

        public ushort Magic { get; private set; }
        public byte MajorLinkerVersion { get; private set; }
        public byte MinorLinkerVersion { get; private set; }
        public uint SizeOfCode { get; private set; }
        public uint SizeOfInitializedData { get; private set; }
        public uint SizeOfUninitializedData { get; private set; }
        public uint AddressOfEntryPoint { get; private set; }
        public uint BaseOfCode { get; private set; }
        public uint BaseOfData { get; private set; }
        public ulong ImageBase { get; private set; }
        public uint SectionAlignment { get; private set; }
        public uint FileAlignment { get; private set; }
        public ushort MajorOperatingSystemVersion { get; private set; }
        public ushort MinorOperatingSystemVersion { get; private set; }
        public ushort MajorImageVersion { get; private set; }
        public ushort MinorImageVersion { get; private set; }
        public ushort MajorSubsystemVersion { get; private set; }
        public ushort MinorSubsystemVersion { get; private set; }
        public uint Win32VersionValue { get; private set; }
        public uint SizeOfImage { get; private set; }
        public uint SizeOfHeaders { get; private set; }
        public uint CheckSum { get; private set; }
        public SubsystemType Subsystem { get; private set; }
        public DllCharacteristicsFlag DllCharacteristics { get; private set; }
        public ulong SizeOfStackReserve { get; private set; }
        public ulong SizeOfStackCommit { get; private set; }
        public ulong SizeOfHeapReserve { get; private set; }
        public ulong SizeOfHeapCommit { get; private set; }
        public uint LoaderFlags { get; private set; }
        public uint NumberOfRvaAndSizes { get; private set; }

        private ImageOptionalHeader64()
        { }

        internal static ImageOptionalHeader64 Read(BinaryReader reader)
        {
            var nativeHeader = NativeImageOptionalHeader64.Read(reader);
            return new ImageOptionalHeader64()
            {
                NativeHeader = nativeHeader,

                // Standard fields.
                Magic = nativeHeader.Magic,
                MajorLinkerVersion = nativeHeader.MajorLinkerVersion,
                MinorLinkerVersion = nativeHeader.MinorLinkerVersion,
                SizeOfCode = nativeHeader.SizeOfCode,
                SizeOfInitializedData = nativeHeader.SizeOfInitializedData,
                SizeOfUninitializedData = nativeHeader.SizeOfUninitializedData,
                AddressOfEntryPoint = nativeHeader.AddressOfEntryPoint,
                BaseOfCode = nativeHeader.BaseOfCode,
                BaseOfData = nativeHeader.BaseOfData,

                // NT additional fields.
                ImageBase = nativeHeader.ImageBase,
                SectionAlignment = nativeHeader.SectionAlignment,
                FileAlignment = nativeHeader.FileAlignment,
                MajorOperatingSystemVersion = nativeHeader.MajorOperatingSystemVersion,
                MinorOperatingSystemVersion = nativeHeader.MinorOperatingSystemVersion,
                MajorImageVersion = nativeHeader.MajorImageVersion,
                MinorImageVersion = nativeHeader.MinorImageVersion,
                MajorSubsystemVersion = nativeHeader.MajorSubsystemVersion,
                MinorSubsystemVersion = nativeHeader.MinorSubsystemVersion,
                Win32VersionValue = nativeHeader.Win32VersionValue,
                SizeOfImage = nativeHeader.SizeOfImage,
                SizeOfHeaders = nativeHeader.SizeOfHeaders,
                CheckSum = nativeHeader.CheckSum,
                Subsystem = (SubsystemType)nativeHeader.Subsystem,
                DllCharacteristics = (DllCharacteristicsFlag)nativeHeader.DllCharacteristics,
                SizeOfStackReserve = nativeHeader.SizeOfStackReserve,
                SizeOfStackCommit = nativeHeader.SizeOfStackCommit,
                SizeOfHeapReserve = nativeHeader.SizeOfHeapReserve,
                SizeOfHeapCommit = nativeHeader.SizeOfHeapCommit,
                LoaderFlags = nativeHeader.LoaderFlags,
                NumberOfRvaAndSizes = nativeHeader.NumberOfRvaAndSizes,
            };
        }
    }
}
