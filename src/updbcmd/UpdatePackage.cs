using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Linq;

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

    internal enum UpdatePackageType
    {
        Unknown,   // Unknown update package type
        MSCF,      // .msu, .cab
    };

    internal class UnknownUpdatePackageTypeException : Exception
    {
        public string UpdatePackageFilePath { get; private set; }

        public UnknownUpdatePackageTypeException(string updatePackageFilePath)
            : this(updatePackageFilePath, null)
        { }

        public UnknownUpdatePackageTypeException(string updatePackageFilePath, Exception innerException)
            : base(string.Format(@"The package type of ""{0}"" was unknown.", updatePackageFilePath), innerException)
        {
            UpdatePackageFilePath = updatePackageFilePath;
        }
    }

    internal class ExternalCommandException : Exception
    {
        public Process Process { get; private set; }
        public string OutputData { get; private set; }
        public string ErrorData { get; private set; }

        public ExternalCommandException(Process process, string outputData, string errorData)
            : this(process, outputData, errorData, null)
        { }

        public ExternalCommandException(Process process, string outputData, string errorData, Exception innerException)
            : base(string.Format(@"The command-line ""{0} {1}"" was abnormally exit with {2}", process.StartInfo.FileName, process.StartInfo.Arguments, process.ExitCode), innerException)
        {
            Process = process;
            OutputData = outputData;
            ErrorData = errorData;
        }
    }

    internal class MscfUpdatePackageDataRetrieveException : Exception
    {
        public string UpdatePackageFilePath { get; private set; }

        public MscfUpdatePackageDataRetrieveException(string updatePackageFilePath)
            : this(updatePackageFilePath, null)
        { }

        public MscfUpdatePackageDataRetrieveException(string updatePackageFilePath, Exception innerException)
            : base(string.Format(@"Could not retrieve the data from update package ""{0}"".", updatePackageFilePath), innerException)
        {
            UpdatePackageFilePath = updatePackageFilePath;
        }
    }
}
