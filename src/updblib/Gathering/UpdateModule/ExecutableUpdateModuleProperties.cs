using System.IO;
using System.Diagnostics;
using UPDB.Gathering.Helpers;

namespace UPDB.Gathering
{
    public enum UpdateModuleProcessorArchitecture
    {
        Unknown,  // Unknown
        I386,     // Intel 386
        Amd64,    // AMD64 (K8)
        Arm,      // ARM64 Little-Endian
        IA64,     // Intel 64
    };

    public class ExecutableUpdateModuleProperties
    {
        public string FileName { get; protected set; }
        public string OriginalFileName { get; protected set; }
        public string InternalName { get; protected set; }
        public string FileVersion { get; protected set; }
        public string FileDescription { get; protected set; }
        public string ProductName { get; protected set; }
        public string ProductVersion { get; protected set; }
        public string Language { get; protected set; }
        public string CompanyName { get; protected set; }
        public string LegalCopyright { get; protected set; }
        public string LegalTrademarks { get; protected set; }
        public UpdateModuleProcessorArchitecture ProcessorArchitecture { get; protected set; }

        public ExecutableUpdateModuleProperties(string executableUpdateModuleFilePath)
        {
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(executableUpdateModuleFilePath);
            FileName = Path.GetFileName(fileVersionInfo.FileName.Trim());
            OriginalFileName = fileVersionInfo.OriginalFilename?.Trim();
            InternalName = fileVersionInfo.InternalName?.Trim();
            FileVersion = fileVersionInfo.FileVersion?.Trim();
            FileDescription = fileVersionInfo.FileDescription?.Trim();
            ProductName = fileVersionInfo.ProductName?.Trim();
            ProductVersion = fileVersionInfo.ProductVersion?.Trim();
            Language = fileVersionInfo.Language?.Trim();
            CompanyName = fileVersionInfo.CompanyName?.Trim();
            LegalCopyright = fileVersionInfo.LegalCopyright?.Trim();
            LegalTrademarks = fileVersionInfo.LegalTrademarks?.Trim();
            ProcessorArchitecture = ReadModuleProcessorArchitecture(executableUpdateModuleFilePath);
        }

        private static UpdateModuleProcessorArchitecture ReadModuleProcessorArchitecture(string filePath)
        {
            var peFileHeader = PortableExecutableFileHeader.Read(filePath);
            var fileProcArch = peFileHeader.NtHeader.FileHeader.Machine;

            if (fileProcArch == ImageFileHeader.MachineType.AMD64)
            {
                return UpdateModuleProcessorArchitecture.Amd64;
            }
            else if (fileProcArch == ImageFileHeader.MachineType.I386)
            {
                return UpdateModuleProcessorArchitecture.I386;
            }
            else if (fileProcArch == ImageFileHeader.MachineType.ARM)
            {
                return UpdateModuleProcessorArchitecture.Arm;
            }
            else if (fileProcArch == ImageFileHeader.MachineType.IA64)
            {
                return UpdateModuleProcessorArchitecture.IA64;
            }
            return UpdateModuleProcessorArchitecture.Unknown;
        }
    }
}
