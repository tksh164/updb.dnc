using System;

namespace updbcmd
{
    class Program
    {
        static void Main(string[] args)
        {
            char[] trimChars = new char[] { ' ', '\t', '"' };
            while (true)
            {
                var filePath = Console.ReadLine().Trim(trimChars);
                if (string.IsNullOrWhiteSpace(filePath)) break;
                Console.WriteLine(filePath);
            }
        }
    }
}
