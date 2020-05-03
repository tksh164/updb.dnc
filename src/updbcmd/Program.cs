using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using UPDB.Gathering;

namespace updbcmd
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var settings = new UpdbCmdSettings();
            var logger = Logger.Initialize(settings.LogFolderPath, settings.LogFileName);
            logger.WriteLog(new LogRecord()
            {
                Message = string.Format("---- Logging started ----"),
            }, nameof(Program));

            var sw = new Stopwatch();
            sw.Start();

            await ProcessUpdatePackages(settings);

            sw.Stop();
            logger.WriteLog(new LogRecord()
            {
                Message = string.Format("The elapsed time was {0} seconds.", sw.Elapsed.TotalSeconds),
            }, nameof(Program));
        }

        private static async Task ProcessUpdatePackages(UpdbCmdSettings settings)
        {
            using (var processItems = new BlockingCollection<ProcessItem>())
            {
                var producerActionParams = new ProducerActionParameters(processItems);
                using (var producerTask = Task<int>.Factory.StartNew(ProducerAction, producerActionParams, TaskCreationOptions.PreferFairness))
                {
                    Task[] consumerTasks = null;
                    try
                    {
                        consumerTasks = new Task[settings.ConsumerThreadCount];
                        for (var i = 0; i < consumerTasks.Length; i++)
                        {
                            var consumerActionParam = new ConsumerActionParameters(i, processItems);
                            consumerTasks[i] = Task<(int Succeeded, int Failed)>.Factory.StartNew(ConsumerAction, consumerActionParam, TaskCreationOptions.LongRunning);
                        }

                        // Combine the producer task and consumer tasks to wait for finish all task.
                        var allTasks = new Task[1 + consumerTasks.Length];
                        allTasks[0] = producerTask;
                        Array.Copy(consumerTasks, 0, allTasks, 1, consumerTasks.Length);
                        await Task.WhenAll(allTasks);
                    }
                    finally
                    {
                        if (consumerTasks != null)
                        {
                            for (var i = 0; i < consumerTasks.Length; i++)
                            {
                                consumerTasks[i].Dispose();
                            }
                        }
                    }
                }
                Console.WriteLine("IsAddingCompleted: {0}", processItems.IsAddingCompleted);
                Console.WriteLine("IsCompleted: {0}", processItems.IsCompleted);
            }
        }

        internal sealed class ProcessItem
        {
            public string FilePath { get; private set; }
            public Guid CorrelationId { get; private set; }

            public ProcessItem(string filePath)
            {
                FilePath = filePath;
                CorrelationId = Guid.NewGuid();
            }
        }

        internal sealed class ProducerActionParameters
        {
            public BlockingCollection<ProcessItem> ProcessItems { get; private set; }

            public ProducerActionParameters(BlockingCollection<ProcessItem> processItems)
            {
                ProcessItems = processItems;
            }
        }

        private static int ProducerAction(object actionParams)
        {
            var ap = actionParams as ProducerActionParameters;
            var logger = Logger.GetInstance();

            var trimChars = new char[] { ' ', '\t', '"', '\'' };
            var addedCount = 0;
            while (true)
            {
                var filePath = Console.ReadLine()?.Trim(trimChars);
                if (string.IsNullOrWhiteSpace(filePath)) break;
                var item = new ProcessItem(filePath);
                ap.ProcessItems.Add(item);
                addedCount++;

                logger.WriteLog(new LogRecord()
                {
                    CorrelationId = item.CorrelationId,
                    Message = string.Format(@"Update package file path: ""{0}""", item.FilePath),
                }, nameof(Program));
            }
            ap.ProcessItems.CompleteAdding();

            logger.WriteLog(new LogRecord()
            {
                Message = string.Format("Added update package path count: {0}", addedCount),
            }, nameof(Program));

            return addedCount;
        }

        internal sealed class ConsumerActionParameters
        {
            public int WorkerId { get; private set; }
            public BlockingCollection<ProcessItem> ProcessItems { get; private set; }

            public ConsumerActionParameters(int workerId, BlockingCollection<ProcessItem> processItems)
            {
                WorkerId = workerId;
                ProcessItems = processItems;
            }
        }

        private static (int Succeeded, int Failed) ConsumerAction(object actionParams)
        {
            var ap = actionParams as ConsumerActionParameters;
            var logger = Logger.GetInstance();

            logger.WriteLog(new LogRecord()
            {
                Message = string.Format(@"The worker-{0} started.", ap.WorkerId),
            }, nameof(Program));

            var succeededCount = 0;
            var failedCount = 0;
            while (true)
            {
                ProcessItem item;
                try
                {
                    item = ap.ProcessItems.Take();
                }
                catch (InvalidOperationException)
                {
                    break;
                }

                try
                {
                    logger.WriteLog(new LogRecord()
                    {
                        CorrelationId = item.CorrelationId,
                        Message = string.Format(@"The processing started on the worker-{0}.", ap.WorkerId),
                    }, nameof(Program));

                    var updatePackage = UpdatePackage.RetrieveData(item.FilePath);
                    succeededCount++;

                    logger.WriteLog(new LogRecord()
                    {
                        CorrelationId = item.CorrelationId,
                        Message = string.Format(@"The processing ended on the worker-{0}.", ap.WorkerId),
                    }, nameof(Program));
                }
                catch (Exception e)
                {
                    failedCount++;
                    Console.WriteLine(e.ToString());
                }
            }

            logger.WriteLog(new LogRecord()
            {
                Message = string.Format("The worker-{0} ended. The results are {1} succeeded, {2} failed.", ap.WorkerId, succeededCount, failedCount),
            }, nameof(Program));

            return (Succeeded: succeededCount, Failed: failedCount);
        }
    }
}
