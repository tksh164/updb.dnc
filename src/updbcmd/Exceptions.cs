using System;
using System.Collections.Generic;
using System.Text;

namespace updbcmd
{
    public class LoggerAlreadyInitializedException : Exception
    {
        public string ExistingLoggFilePath { get; protected set; }

        public LoggerAlreadyInitializedException(string existingLoggFilePath)
            : this(existingLoggFilePath, null)
        { }

        public LoggerAlreadyInitializedException(string existingLoggFilePath, Exception innerException)
            : base(string.Format(@"The logger is already initialized with ""{0}"".", existingLoggFilePath), innerException)
        {
            ExistingLoggFilePath = existingLoggFilePath;
        }
    }

    public class LoggerNotInitializedException : Exception
    {
        public LoggerNotInitializedException()
            : this(null)
        { }

        public LoggerNotInitializedException(Exception innerException)
            : base(string.Format(@"The uninitialized logger was used. The logger is not initialized yet."), innerException)
        { }
    }
}
