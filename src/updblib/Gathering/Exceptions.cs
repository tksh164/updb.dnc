﻿using System;
using System.Diagnostics;

namespace UPDB.Gathering
{
    public class UnknownUpdatePackageTypeException : Exception
    {
        public string UpdatePackageFilePath { get; protected set; }

        public UnknownUpdatePackageTypeException(string updatePackageFilePath)
            : this(updatePackageFilePath, null)
        { }

        public UnknownUpdatePackageTypeException(string updatePackageFilePath, Exception innerException)
            : base(string.Format(@"The package type of ""{0}"" was unknown.", updatePackageFilePath), innerException)
        {
            UpdatePackageFilePath = updatePackageFilePath;
        }
    }

    public class ExternalCommandException : Exception
    {
        public Process Process { get; protected set; }
        public string OutputData { get; protected set; }
        public string ErrorData { get; protected set; }

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

    public class MscfUpdatePackageDataRetrieveException : Exception
    {
        public string UpdatePackageFilePath { get; protected set; }

        public MscfUpdatePackageDataRetrieveException(string updatePackageFilePath)
            : this(updatePackageFilePath, null)
        { }

        public MscfUpdatePackageDataRetrieveException(string updatePackageFilePath, Exception innerException)
            : base(string.Format(@"Could not retrieve the data from update package ""{0}"".", updatePackageFilePath), innerException)
        {
            UpdatePackageFilePath = updatePackageFilePath;
        }
    }

    public class UpdatePackageCriticalFileNotFoundException : Exception
    {
        public string FilePath { get; protected set; }

        public UpdatePackageCriticalFileNotFoundException(string filePath)
            : this(filePath, null)
        { }

        public UpdatePackageCriticalFileNotFoundException(string filePath, Exception innerException)
            : base(string.Format(@"The critical file ""{0}"" did not exist in the update package file.", filePath), innerException)
        {
            FilePath = filePath;
        }
    }

    public class UpdatePackageXmlNodeNotFoundException : Exception
    {
        public string XPath { get; protected set; }

        public UpdatePackageXmlNodeNotFoundException(string xpath)
            : this(xpath, null)
        { }

        public UpdatePackageXmlNodeNotFoundException(string xpath, Exception innerException)
            : base(string.Format(@"The ""{0}"" node did not found on the XML document.", xpath), innerException)
        {
            XPath = xpath;
        }
    }

    public class UpdatePackageXmlAttributeNotFoundException : Exception
    {
        public string XPath { get; protected set; }
        public string Attribute { get; protected set; }

        public UpdatePackageXmlAttributeNotFoundException(string xpath, string attribute)
            : this(xpath, attribute, null)
        { }

        public UpdatePackageXmlAttributeNotFoundException(string xpath, string attribute, Exception innerException)
            : base(string.Format(@"The ""{0}"" attribute did not found on the ""{1}"" node.", attribute, xpath), innerException)
        {
            XPath = xpath;
            Attribute = attribute;
        }
    }

    public class UpdateModuleDataRetrieveException : Exception
    {
        public string UpdateModuleFilePath { get; protected set; }

        public UpdateModuleDataRetrieveException(string updateModuleFilePath)
            : this(updateModuleFilePath, null)
        { }

        public UpdateModuleDataRetrieveException(string updateModuleFilePath, Exception innerException)
            : base(string.Format(@"Could not retrieve the data from update module ""{0}"".", updateModuleFilePath), innerException)
        {
            UpdateModuleFilePath = updateModuleFilePath;
        }
    }
}
