using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using UPDB.Gathering.Helpers;
using System.Collections.Generic;

namespace UPDB.Gathering
{
    public enum UpdatePackageType
    {
        Unknown,   // Unknown update package type
        MSCF,      // .msu, .cab
    };

    public class UpdatePackage
    {
        public string UpdatePackageFielPath { get; protected set; }
        public byte[] FielHash { get; protected set; }
        public UpdatePackageType UpdatePackageType { get; protected set; }
        public UpdatePackageMetadataFromXmlFile PropertiesFromXmlFile { get; protected set; }
        public UpdatePackageMetadataFromPropertyFile PropertiesFromPropertyFile { get; protected set; }
        public List<UpdateModule> UpdateModules { get; protected set; }

        private UpdatePackage()
        { }

        public static UpdatePackage RetrieveData(string updatePackageFilePath)
        {
            if (Directory.Exists(updatePackageFilePath))
            {
                throw new ArgumentException(string.Format(@"The path ""{0}"" was not a file path. It was a path to a directory.", updatePackageFilePath), nameof(updatePackageFilePath));
            }

            var package = new UpdatePackage();
            var updatePackageType = DetectUpdatePackageType(updatePackageFilePath);
            if (updatePackageType == UpdatePackageType.MSCF)
            {
                string workFolderPath = null;
                try
                {
                    workFolderPath = CreateWorkFolder();
                    ExtractMscfUpdatePackageFile(updatePackageFilePath, workFolderPath);

                    if (VerifyWsusScanCabExistence(workFolderPath))
                    {
                        // .msu package

                        var packageXmlFilePath = GetFilePathDirectlyUnderFolder(workFolderPath, "*.xml");
                        package.PropertiesFromXmlFile = new UpdatePackageMetadataFromXmlFile(packageXmlFilePath);

                        var packagePropertyFilePath = GetFilePathDirectlyUnderFolder(workFolderPath, "*-pkgProperties.txt");
                        package.PropertiesFromPropertyFile = new UpdatePackageMetadataFromPropertyFile(packagePropertyFilePath);

                        var innerCabFilePath = GetInnerCabFilePath(package.PropertiesFromXmlFile.InnerCabFileLocation, workFolderPath);
                        var innerCabWorkFolderPath = CreateWorkFolder(workFolderPath);
                        ExtractMscfUpdatePackageFile(innerCabFilePath, innerCabWorkFolderPath);

                        package.UpdateModules = RetrieveUpdateModules(innerCabWorkFolderPath);
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

            package.UpdatePackageFielPath = updatePackageFilePath;
            package.FielHash = FileHashHelper.ComputeFileHash(updatePackageFilePath);
            package.UpdatePackageType = updatePackageType;

            return package;
        }

        private static readonly byte[] MscfSignature = new byte[] { 0x4d, 0x53, 0x43, 0x46 };  // M, S, C, F

        private static UpdatePackageType DetectUpdatePackageType(string updatePackageFilePath)
        {
            var signature = FileSignatureHelper.ReadFileSignature(updatePackageFilePath);
            if (FileSignatureHelper.CompareFileSignature(MscfSignature, signature.AsSpan(0, MscfSignature.Length)))
            {
                return UpdatePackageType.MSCF;
            }
            return UpdatePackageType.Unknown;
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
                workFolderPath = Path.Combine(baseFolderPath, Path.GetRandomFileName().Substring(0, 3));
                if (!Directory.Exists(workFolderPath)) break;
            }
            Directory.CreateDirectory(workFolderPath);
            Debug.WriteLine("WorkFolderPath: {0}", workFolderPath);
            return workFolderPath;
        }

        private static void ExtractMscfUpdatePackageFile(string updatePackageFilePath, string destinationFolderPath)
        {
            const string commandFilePath = @"C:\Windows\System32\expand.exe";
            var commandParameter = string.Format(@"-f:* ""{0}"" ""{1}""", updatePackageFilePath, destinationFolderPath);

            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                FileName = commandFilePath,
                Arguments = commandParameter,
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

        private static string GetInnerCabFilePath(string innerCabFileLocation, string workFolderPath)
        {
            const string placeholderKeyword = "%ConfigSetRoot%";
            if (!innerCabFileLocation.Contains(placeholderKeyword, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    string.Format(@"The inner CAB file location did not contain the placeholder keyword {0}. The inner CAB file location was ""{1}"".", placeholderKeyword, innerCabFileLocation),
                    nameof(innerCabFileLocation));
            }
            return innerCabFileLocation.Replace(placeholderKeyword, workFolderPath, StringComparison.OrdinalIgnoreCase);
        }

        private static List<UpdateModule> RetrieveUpdateModules(string innerCabWorkFolderPath)
        {
            var moduleFolderEnumOptions = new EnumerationOptions()
            {
                AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
                BufferSize = 0,
                IgnoreInaccessible = false,
                MatchCasing = MatchCasing.CaseInsensitive,
                MatchType = MatchType.Simple,
                ReturnSpecialDirectories = false,
                RecurseSubdirectories = false,
            };
            var moduleFileEnumOptions = new EnumerationOptions()
            {
                AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
                BufferSize = 0,
                IgnoreInaccessible = false,
                MatchCasing = MatchCasing.CaseInsensitive,
                MatchType = MatchType.Simple,
                ReturnSpecialDirectories = false,
                RecurseSubdirectories = true,
            };
            var updateModules = new List<UpdateModule>();
            foreach (var moduleFolderPath in Directory.EnumerateDirectories(innerCabWorkFolderPath, "*", moduleFolderEnumOptions))
            {
                foreach (var moduleFilePath in Directory.EnumerateFiles(moduleFolderPath, "*", moduleFileEnumOptions))
                {
                    updateModules.Add(UpdateModule.RetrieveData(moduleFilePath));
                }
            }
            return updateModules;
        }
    }
}
