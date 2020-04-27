using System.IO;
using System.Security.Cryptography;

namespace UPDB.Gathering.Helpers
{
    internal static class FileHashHelper
    {
        public static byte[] ComputeFileHash(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                SHA1CryptoServiceProvider sha1Provider = new SHA1CryptoServiceProvider();
                return sha1Provider.ComputeHash(stream);
            }
        }
    }
}
