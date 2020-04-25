﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Xml;

namespace updbcmd
{
    internal class UpdatePackage
    {
        public static UpdatePackage RetrieveData(string updatePackageFilePath)
        {
            if (Directory.Exists(updatePackageFilePath))
            {
                throw new ArgumentException(string.Format(@"The path ""{0}"" was not a file path. It was a path to a directory.", updatePackageFilePath), nameof(updatePackageFilePath));
            }

            var updatePackageType = UpdatePackageTypeDetector.Detect(updatePackageFilePath);
            if (updatePackageType == UpdatePackageType.MSCF)
            {
                string workFolderPath = null;
                try
                {
                    workFolderPath = CreateWorkFolder();
                    MscfUpdatePackageExtractor.Extract(updatePackageFilePath, workFolderPath);

                    if (VerifyWsusScanCabExistence(workFolderPath))
                    {
                        // .msu package
                        var packageXmlFilePath = GetFilePathDirectlyUnderFolder(workFolderPath, "*.xml");
                        var packageMetadataFromXmlFile = GetPackageMetadataFromXmlFile(packageXmlFilePath);
                        var packagePropertyFilePath = GetFilePathDirectlyUnderFolder(workFolderPath, "*-pkgProperties.txt");
                        var packageMetadataFromPropertyFile = GetPackageMetadataFromPropertyFile(packagePropertyFilePath);
                    }
                    else
                    {
                        // .cab package
                        throw new NotImplementedException(string.Format(@"The CAB file type update package does not support currently. The package file path was ""{0}"".", updatePackageFilePath));
                    }
                }
                catch (Exception e)
                {
                    throw new MscfUpdatePackageDataRetrieveException(updatePackageFilePath, e);
                }
                finally
                {
                    if (workFolderPath != null && Directory.Exists(workFolderPath))
                    {
                        Directory.Delete(workFolderPath, true);
                    }
                }
            }
            else
            {
                throw new UnknownUpdatePackageTypeException(updatePackageFilePath);
            }

            return new UpdatePackage();
        }

        private static string CreateWorkFolder()
        {
            return CreateWorkFolder(Path.GetTempPath());
        }

        private static string CreateWorkFolder(string baseFolderPath)
        {
            string workFolderPath;
            while (true)
            {
                workFolderPath = Path.Combine(baseFolderPath, Path.GetRandomFileName());
                if (!Directory.Exists(workFolderPath)) break;
            }
            Directory.CreateDirectory(workFolderPath);
            Debug.WriteLine("WorkFolderPath: {0}", workFolderPath);
            return workFolderPath;
        }

        private static bool VerifyWsusScanCabExistence(string workFolderPath)
        {
            var wsusScanCabFilePath = Directory.EnumerateFiles(workFolderPath, "WSUSSCAN.cab", SearchOption.TopDirectoryOnly).FirstOrDefault();
            return wsusScanCabFilePath != null;
        }

        private static string GetFilePathDirectlyUnderFolder(string workFolderPath, string fileNamePattern)
        {
            var filePath = Directory.EnumerateFiles(workFolderPath, fileNamePattern, SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (filePath == null)
            {
                throw new UpdatePackageCriticalFileNotFoundException(Path.Combine(workFolderPath, fileNamePattern));
            }
            return filePath;
        }

        private static PackageMetadataFromXmlFile GetPackageMetadataFromXmlFile(string packageXmlFilePath)
        {
            var rootXmlDoc = new XmlDocument();
            rootXmlDoc.Load(packageXmlFilePath);
            var nsManager = new XmlNamespaceManager(rootXmlDoc.NameTable);
            nsManager.AddNamespace("u", "urn:schemas-microsoft-com:unattend");
            return new PackageMetadataFromXmlFile()
            {
                PackageName = GetXmlAttributeValue(rootXmlDoc, nsManager, "/u:unattend/u:servicing/u:package/u:assemblyIdentity", "name"),
                PackageVersion = GetXmlAttributeValue(rootXmlDoc, nsManager, "/u:unattend/u:servicing/u:package/u:assemblyIdentity", "version"),
                PackageLanguage = GetXmlAttributeValue(rootXmlDoc, nsManager, "/u:unattend/u:servicing/u:package/u:assemblyIdentity", "language"),
                PackageProcessorArchitecture = GetXmlAttributeValue(rootXmlDoc, nsManager, "/u:unattend/u:servicing/u:package/u:assemblyIdentity", "processorArchitecture"),
                InnerCabFileLocation = GetXmlAttributeValue(rootXmlDoc, nsManager, "/u:unattend/u:servicing/u:package/u:source", "location"),
            };
        }

        private static string GetXmlAttributeValue(XmlDocument rootXmlDoc, XmlNamespaceManager nsManager, string nodeXPath, string attributeName)
        {
            var node = rootXmlDoc.SelectSingleNode(nodeXPath, nsManager);
            if (node == null) throw new UpdatePackageXmlNodeNotFoundException(nodeXPath);
            var attributeValue = node?.Attributes[attributeName]?.Value;
            if (attributeValue == null) throw new UpdatePackageXmlAttributeNotFoundException(nodeXPath, attributeName);
            return attributeValue;
        }

        internal class PackageMetadataFromXmlFile
        {
            public string PackageName { get; set; }
            public string PackageVersion { get; set; }
            public string PackageLanguage { get; set; }
            public string PackageProcessorArchitecture { get; set; }
            public string InnerCabFileLocation { get; set; }
        }

        private static PackageMetadataFromPropertyFile GetPackageMetadataFromPropertyFile(string packagePropertyFilePath)
        {
            var lines = File.ReadAllLines(packagePropertyFilePath, Encoding.UTF8);
            var properties = new Dictionary<string, string>(lines.Length);
            foreach (var line in lines)
            {
                (var key, var value) = ExtractKeyValue(line);
                properties.Add(key, value);
            }
            return new PackageMetadataFromPropertyFile()
            {
                ApplicabilityInfo = properties["ApplicabilityInfo"],
                AppliesTo = properties["Applies to"],
                BuildDate = properties["Build Date"],
                Company = properties["Company"],
                FileVersion = properties["File Version"],
                InstallationType = properties["Installation Type"],
                InstallerEngine = properties["Installer Engine"],
                InstallerVersion = properties["Installer Version"],
                KBArticleNumber = properties["KB Article Number"],
                Language = properties["Language"],
                PackageType = properties["Package Type"],
                ProcessorArchitecture = properties["Processor Architecture"],
                ProductName = properties["Product Name"],
                SupportLink = properties["Support Link"],
            };
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

        internal class PackageMetadataFromPropertyFile
        {
            public string ApplicabilityInfo { get; set; }
            public string AppliesTo { get; set; }
            public string BuildDate { get; set; }
            public string Company { get; set; }
            public string FileVersion { get; set; }
            public string InstallationType { get; set; }
            public string InstallerEngine { get; set; }
            public string InstallerVersion { get; set; }
            public string KBArticleNumber { get; set; }
            public string Language { get; set; }
            public string PackageType { get; set; }
            public string ProcessorArchitecture { get; set; }
            public string ProductName { get; set; }
            public string SupportLink { get; set; }
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

            private static bool CompareByteArray(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
            {
                return a1.SequenceEqual(a2);
            }
        }

        internal class MscfUpdatePackageExtractor
        {
            private const string CommandFilePath = @"C:\Windows\System32\expand.exe";

            public static void Extract(string updatePackageFilePath, string destinationFolderPath)
            {
                var commandParameter = string.Format(@"-f:* ""{0}"" ""{1}""", updatePackageFilePath, destinationFolderPath);
                ExecuteExternalCommand(CommandFilePath, commandParameter);
            }

            private static void ExecuteExternalCommand(string commandFilePath, string commandParameter = null)
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo()
                {
                    FileName = commandFilePath,
                    Arguments = commandParameter ?? string.Empty,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                using (Process process = Process.Start(processStartInfo))
                {
                    StringBuilder outputData = new StringBuilder();
                    process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                        outputData.Append(e.Data);
                    };
                    StringBuilder errorData = new StringBuilder();
                    process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
                        errorData.Append(e.Data);
                    };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new ExternalCommandException(process, outputData.ToString(), errorData.ToString());
                    }
                }
            }
        }
    }
}
