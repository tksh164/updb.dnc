using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace UPDB.Gathering
{
    public class UpdatePackagePropertiesFromPropertyFile
    {
        public string ApplicabilityInfoProperty { get; protected set; }
        public string AppliesToProperty { get; protected set; }
        public string BuildDateProperty { get; protected set; }
        public string CompanyProperty { get; protected set; }
        public string FileVersionProperty { get; protected set; }
        public string InstallationTypeProperty { get; protected set; }
        public string InstallerEngineProperty { get; protected set; }
        public string InstallerVersionProperty { get; protected set; }
        public string KBArticleNumberProperty { get; protected set; }
        public string LanguageProperty { get; protected set; }
        public string PackageTypeProperty { get; protected set; }
        public string ProcessorArchitectureProperty { get; protected set; }
        public string ProductNameProperty { get; protected set; }
        public string SupportLinkProperty { get; protected set; }

        internal UpdatePackagePropertiesFromPropertyFile(string packagePropertyFilePath)
        {
            var lines = File.ReadAllLines(packagePropertyFilePath, Encoding.UTF8);
            var properties = new Dictionary<string, string>(lines.Length);
            foreach (var line in lines)
            {
                (var key, var value) = ExtractKeyValue(line);
                properties.Add(key, value);
            }
            ApplicabilityInfoProperty = properties["ApplicabilityInfo"];
            AppliesToProperty = properties["Applies to"];
            BuildDateProperty = properties["Build Date"];
            CompanyProperty = properties["Company"];
            FileVersionProperty = properties["File Version"];
            InstallationTypeProperty = properties["Installation Type"];
            InstallerEngineProperty = properties["Installer Engine"];
            InstallerVersionProperty = properties["Installer Version"];
            KBArticleNumberProperty = properties["KB Article Number"];
            LanguageProperty = properties["Language"];
            PackageTypeProperty = properties["Package Type"];
            ProcessorArchitectureProperty = properties["Processor Architecture"];
            ProductNameProperty = properties["Product Name"];
            SupportLinkProperty = properties["Support Link"];
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
