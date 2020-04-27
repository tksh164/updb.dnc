using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace UPDB.Gathering
{
    public class UpdatePackagePropertiesFromPropertyFile
    {
        public string ApplicabilityInfo { get; protected set; }
        public string AppliesTo { get; protected set; }
        public string BuildDate { get; protected set; }
        public string Company { get; protected set; }
        public string FileVersion { get; protected set; }
        public string InstallationType { get; protected set; }
        public string InstallerEngine { get; protected set; }
        public string InstallerVersion { get; protected set; }
        public string KBArticleNumber { get; protected set; }
        public string Language { get; protected set; }
        public string PackageType { get; protected set; }
        public string ProcessorArchitecture { get; protected set; }
        public string ProductName { get; protected set; }
        public string SupportLink { get; protected set; }

        internal UpdatePackagePropertiesFromPropertyFile(string packagePropertyFilePath)
        {
            var lines = File.ReadAllLines(packagePropertyFilePath, Encoding.UTF8);
            var properties = new Dictionary<string, string>(lines.Length);
            foreach (var line in lines)
            {
                (var key, var value) = ExtractKeyValue(line);
                properties.Add(key, value);
            }
            ApplicabilityInfo = properties["ApplicabilityInfo"];
            AppliesTo = properties["Applies to"];
            BuildDate = properties["Build Date"];
            Company = properties["Company"];
            FileVersion = properties["File Version"];
            InstallationType = properties["Installation Type"];
            InstallerEngine = properties["Installer Engine"];
            InstallerVersion = properties["Installer Version"];
            KBArticleNumber = properties["KB Article Number"];
            Language = properties["Language"];
            PackageType = properties["Package Type"];
            ProcessorArchitecture = properties["Processor Architecture"];
            ProductName = properties["Product Name"];
            SupportLink = properties["Support Link"];
        }

        private static (string Key, string Value) ExtractKeyValue(string line)
        {
            var parts = line.Split("=", 2, StringSplitOptions.None);
            if (parts.Length < 2) throw new ArgumentOutOfRangeException(nameof(line), line, "Unexpected line format detected as package property file line.");
            var trimChars = new char[] { ' ', '"' };
            var key = parts[0].Trim(trimChars);
            var value = parts[1].Trim(trimChars);
            return (key, value);
        }
    }
}
