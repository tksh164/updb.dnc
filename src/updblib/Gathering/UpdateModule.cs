using System;
using System.IO;
using UPDB.Gathering.Helpers;

namespace UPDB.Gathering
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
        public byte[] FielHash { get; protected set; }
        public UpdateModuleFileType UpdateModuleFileType { get; protected set; }
        public long FileSize { get; protected set; }
        public DateTime LastModifiedDateTimeUtc { get; protected set; }
        public ExecutableUpdateModuleProperties ExecutableModuleProperties { get; protected set; }

        public static UpdateModule RetrieveData(string updateModuleFilePath)
        {
            if (Directory.Exists(updateModuleFilePath))
            {
                throw new ArgumentException(string.Format(@"The path ""{0}"" was not a file path. It was a path to a directory.", updateModuleFilePath), nameof(updateModuleFilePath));
            }

            var module = new UpdateModule();
            try
            {
                var updateModuleFileType = DetectUpdateModuleFileType(updateModuleFilePath);
                if (updateModuleFileType == UpdateModuleFileType.Executable)
                {
                    module.ExecutableModuleProperties = new ExecutableUpdateModuleProperties(updateModuleFilePath);
                }
                else if (updateModuleFileType == UpdateModuleFileType.Xml)
                {
                    throw new NotImplementedException(string.Format(@"Not suppoerted module file type ""{0}""", updateModuleFilePath));
                }
                else
                {
                    throw new NotImplementedException(string.Format(@"Not suppoerted module file type ""{0}""", updateModuleFilePath));
                }

                var fileInfo = new FileInfo(updateModuleFilePath);
                module.UpdateModuleFileType = updateModuleFileType;
                module.FileSize = fileInfo.Length;
                module.LastModifiedDateTimeUtc = fileInfo.LastWriteTimeUtc;

                module.UpdateModuleFilePath = updateModuleFilePath;
                module.FielHash = FileHashHelper.ComputeFileHash(updateModuleFilePath);
            }
            catch (Exception e)
            {
                throw new UpdateModuleDataRetrieveException(updateModuleFilePath, e);
            }

            return module;
        }

        private static readonly byte[] ExecutableFileSignature = new byte[] { 0x4d, 0x5a };  // M, Z
        private static readonly byte[] XmlFileSignature = new byte[] { 0x3c, 0x3f, 0x78, 0x6d, 0x6c, 0x20 };  // <?xml

        private static UpdateModuleFileType DetectUpdateModuleFileType(string updateModuleFilePath)
        {
            var signature = FileSignatureHelper.ReadFileSignature(updateModuleFilePath);
            if (FileSignatureHelper.CompareFileSignature(ExecutableFileSignature, signature.AsSpan(0, ExecutableFileSignature.Length)))
            {
                return UpdateModuleFileType.Executable;
            }
            else if (FileSignatureHelper.CompareFileSignature(XmlFileSignature, signature.AsSpan(0, XmlFileSignature.Length)))
            {
                return UpdateModuleFileType.Xml;
            }
            return UpdateModuleFileType.Other;
        }
    }
}
