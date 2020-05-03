using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace updbcmd
{
    internal class Logger
    {
        protected static Logger instance = null;
        private static readonly object writeLogLock = new object();

        public static Logger Initialize(string logFolderPath, string logFileName)
        {
            if (instance != null) throw new LoggerAlreadyInitializedException(instance.LogFilePath);
            instance = new Logger(logFolderPath, logFileName);
            return instance;
        }

        public static Logger GetInstance()
        {
            if (instance != null) return instance;
            throw new LoggerNotInitializedException();
        }

        public string LogFolderPath { get; protected set; }
        public string LogFileName { get; protected set; }
        public string LogFilePath { get { return Path.Combine(LogFolderPath, LogFileName); } }

        protected Logger(string logFolderPath, string logFileName)
        {
            LogFolderPath = logFolderPath;
            LogFileName = logFileName;
        }

        public string WriteLog(LogRecord record, string className = null, [CallerMemberName] string callerMemberName = null, [CallerLineNumber] int callerLineNumber = -1, [CallerFilePath] string callerFilePath = null)
        {
            var parts = new string[] {
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"),
                record.CorrelationId != null ? record.CorrelationId.ToString() : (new Guid()).ToString(),
                record.Message,
                className,
                callerMemberName,
                callerLineNumber.ToString(),
                callerFilePath,
            };
            var line = string.Join('\t', parts);

            lock (writeLogLock)
            {
                using (var stream = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.WriteLine(line);
                    writer.Flush();
                    //writer.Close();
                    //writer.Dispose();
                    //stream.Close();
                    //stream.Dispose();
                }
            }

            var stderrParts = new string[] {
                "[" + parts[0] + "]",
                "{" + parts[1] + "}",
                parts[2]
            };
            Console.Error.WriteLine(string.Join(' ', stderrParts));

            return line;
        }

        public void WriteCorrelationLog(LogRecord record, string correlationLogBody, string className = null, [CallerMemberName] string callerMemberName = null, [CallerLineNumber] int callerLineNumber = -1, [CallerFilePath] string callerFilePath = null)
        {
            if (record.CorrelationId == null) throw new ArgumentException("The log record has not set the correlation ID.", nameof(record));

            var line = WriteLog(record, className, callerMemberName, callerLineNumber, callerFilePath);

            var builder = new StringBuilder();
            builder.AppendLine(line);
            builder.AppendLine();
            builder.AppendLine(correlationLogBody);
            builder.AppendLine("================================");

            var correlationLogFileName = record.CorrelationId.ToString() + ".txt";
            var correlationLogFilePath = Path.Combine(LogFolderPath, correlationLogFileName);
            using (var stream = new FileStream(correlationLogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.WriteLine(builder.ToString());
                writer.Flush();
            }
        }
    }

    internal class LogRecord
    {
        public Guid? CorrelationId { get; set; }
        public string Message { get; set; }
    }
}
