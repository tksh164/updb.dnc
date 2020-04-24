using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace updbcmd
{
    internal class UpdatePackage
    {
        public static UpdatePackage RetrieveData(string updatePackageFilePath)
        {
            if (Directory.Exists(updatePackageFilePath))
            {
                throw new ArgumentException(string.Format("The path '{0}' does not the file path. It is the path to the directory.", updatePackageFilePath), nameof(updatePackageFilePath));
            }

            var updatePackageType = UpdatePackageTypeDetector.Detect(updatePackageFilePath);
            return new UpdatePackage();
        }

        internal enum UpdatePackageType
        {
            Unknown,   // Unknown update package type
            MSCF,      // .msu, .cab
        };

        internal class UpdatePackageTypeDetector
        {
            private static readonly byte[] MscfSignature = new byte[] { 0x4D, 0x53, 0x43, 0x46 };  // M, S, C, F

            public static UpdatePackageType Detect(string filePath)
            {
                var signature = ReadSignature(filePath);
                if (CompareByteArray(MscfSignature, signature.AsSpan(0, MscfSignature.Length)))
                {
                    return UpdatePackageType.MSCF;
                }
                return UpdatePackageType.Unknown;
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

            public static bool CompareByteArray(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
            {
                return a1.SequenceEqual(a2);
            }
        }
    }
}
