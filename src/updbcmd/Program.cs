using System;
using UPDB.Gathering;

namespace updbcmd
{
    class Program
    {
        static void Main(string[] args)
        {
            var trimChars = new char[] { ' ', '\t', '"', '\'' };
            while (true)
            {
                var filePath = Console.ReadLine();
                if (filePath != null) filePath = filePath.Trim(trimChars);
                if (string.IsNullOrWhiteSpace(filePath)) break;
                var updatePackage = UpdatePackage.RetrieveData(filePath);
            }
        }
    }
}
