using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

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
        public static UpdateModule RetrieveData(string updateModuleFilePath)
        {
            var updateModuleFileType = UpdateModuleFileTypeDetector.Detect(updateModuleFilePath);
            if (updateModuleFileType == UpdateModuleFileType.Executable)
            {
                throw new NotImplementedException(string.Format(@"Not suppoerted module file type ""{0}""", updateModuleFilePath));
            }
            else if (updateModuleFileType == UpdateModuleFileType.Xml)
            {
                throw new NotImplementedException(string.Format(@"Not suppoerted module file type ""{0}""", updateModuleFilePath));
            }
            else
            {
                throw new NotImplementedException(string.Format(@"Not suppoerted module file type ""{0}""", updateModuleFilePath));
            }
            return new UpdateModule();
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
