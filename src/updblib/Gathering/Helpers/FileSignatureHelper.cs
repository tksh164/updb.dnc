using System;
using System.IO;

namespace UPDB.Gathering.Helpers
{
    internal static class FileSignatureHelper
    {
        public static byte[] ReadFileSignature(string filePath, int bufferSize = 8)
        {
            var buffer = new byte[bufferSize];
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                stream.Read(buffer, 0, buffer.Length);
            }
            return buffer;
        }

        public static bool CompareFileSignature(ReadOnlySpan<byte> signature1, ReadOnlySpan<byte> signature2)
        {
            return signature1.SequenceEqual(signature2);
        }
    }
}
