using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace updblib.Gathering
{
    public enum UpdateModuleFileType
    {
        Other,       // Other update module type
        Executable,  // .exe, .dll
        Xml,         // .xml
    };

    public class UpdateModule
    {
        public string UpdateModuleFilePath { get; protected set; }
        public UpdateModuleFileType UpdateModuleFileType { get; protected set; }
        public long FileSize { get; protected set; }
        public string OriginalFileName { get; protected set; }
        public string FileVersion { get; protected set; }
        public DateTime LastModifiedDateTimeUtc { get; protected set; }
        public string CompanyName { get; protected set; }
        public string FileDescription { get; protected set; }
        public string FileName { get; protected set; }
        public string InternalName { get; protected set; }
        public string Language { get; protected set; }
        public string LegalCopyright { get; protected set; }
        public string LegalTrademarks { get; protected set; }
        public string ProductName { get; protected set; }
        public string ProductVersion { get; protected set; }

        public static UpdateModule RetrieveData(string updateModuleFilePath)
        {
            if (Directory.Exists(updateModuleFilePath))
            {
                throw new ArgumentException(string.Format(@"The path ""{0}"" was not a file path. It was a path to a directory.", updateModuleFilePath), nameof(updateModuleFilePath));
            }

            var updateModuleFileType = UpdateModuleFileTypeDetector.Detect(updateModuleFilePath);

            var result = new UpdateModule()
            {
                UpdateModuleFilePath = updateModuleFilePath,
                UpdateModuleFileType = updateModuleFileType,
            };

            if (updateModuleFileType == UpdateModuleFileType.Executable)
            {
                var fileInfo = new FileInfo(updateModuleFilePath);
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(updateModuleFilePath);
                result.FileSize = fileInfo.Length;
                result.OriginalFileName = fileVersionInfo.OriginalFilename.Trim();
                result.FileVersion = fileVersionInfo.FileVersion.Trim();
                result.LastModifiedDateTimeUtc = fileInfo.LastWriteTimeUtc;
                result.CompanyName = fileVersionInfo.CompanyName.Trim();
                result.FileDescription = fileVersionInfo.FileDescription.Trim();
                result.FileName = Path.GetFileName(fileVersionInfo.FileName.Trim());
                result.InternalName = fileVersionInfo.InternalName.Trim();
                result.Language = fileVersionInfo.Language.Trim();
                result.LegalCopyright = fileVersionInfo.LegalCopyright.Trim();
                result.LegalTrademarks = fileVersionInfo.LegalTrademarks.Trim();
                result.ProductName = fileVersionInfo.ProductName.Trim();
                result.ProductVersion = fileVersionInfo.ProductVersion.Trim();
            }
            else if (updateModuleFileType == UpdateModuleFileType.Xml)
            {
                throw new NotImplementedException(string.Format(@"Not suppoerted module file type ""{0}""", updateModuleFilePath));
            }
            else
            {
                throw new NotImplementedException(string.Format(@"Not suppoerted module file type ""{0}""", updateModuleFilePath));
            }
            return result;
        }

        internal class UpdateModuleFileTypeDetector
        {
            private static readonly byte[] ExecutableFileSignature = new byte[] { 0x4d, 0x5a };  // M, Z
            private static readonly byte[] XmlFileSignature = new byte[] { 0x3c, 0x3f, 0x78, 0x6d, 0x6c, 0x20 };  // <?xml

            public static UpdateModuleFileType Detect(string filePath)
            {
                var signature = ReadSignature(filePath);
                if (CompareByteArray(ExecutableFileSignature, signature.AsSpan(0, ExecutableFileSignature.Length)))
                {
                    return UpdateModuleFileType.Executable;
                }
                else if (CompareByteArray(XmlFileSignature, signature.AsSpan(0, XmlFileSignature.Length)))
                {
                    return UpdateModuleFileType.Xml;
                }
                return UpdateModuleFileType.Other;
            }

            private static byte[] ReadSignature(string filePath)
            {
                const int bufferSize = 8;
                var buffer = new byte[bufferSize];
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    stream.Read(buffer, 0, buffer.Length);
                }
                return buffer;
            }

            private static bool CompareByteArray(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
            {
                return a1.SequenceEqual(a2);
            }
        }
    }
}
