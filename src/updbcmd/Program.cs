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
            using (var processingItems = new BlockingCollection<ProcessingItem>())
            {
                var itemProducerTaskParam = new ItemProducerTaskParam(processingItems);
                using (var itemProducer = Task<int>.Factory.StartNew(ItemProducerTaskAction, itemProducerTaskParam, TaskCreationOptions.PreferFairness))
                {
                    var workers = new Task<(int Succeeded, int Failed)>[settings.NumOfWorkers];
                    try
                    {
                        for (var i = 0; i < workers.Length; i++)
                        {
                            var workerTaskActionParam = new WorkerTaskActionParam(i, processingItems);
                            workers[i] = Task<(int, int)>.Factory.StartNew(WorkerTaskAction, workerTaskActionParam, TaskCreationOptions.LongRunning);
                        }

                        // Combine the producer task and consumer tasks to wait for finish all task.
                        var allTasks = new Task[1 + workers.Length];
                        allTasks[0] = itemProducer;
                        Array.Copy(workers, 0, allTasks, 1, workers.Length);
                        await Task.WhenAll(allTasks);

                        int totalSucceeded = 0, totalFailed = 0;
                        foreach (var worker in workers)
                        {
                            totalSucceeded += worker.Result.Succeeded;
                            totalFailed += worker.Result.Failed;
                        }
                        Logger.GetInstance().WriteLog(new LogRecord()
                        {
                            Message = string.Format("All workers are ended. The results are {0} succeeded, {1} failed.", totalSucceeded, totalFailed),
                        }, nameof(Program));
                    }
                    finally
                    {
                        foreach (var worker in workers) worker?.Dispose();
                    }
                }
            }
        }

        internal sealed class ProcessingItem
        {
            public string FilePath { get; private set; }
            public Guid CorrelationId { get; private set; }

            public ProcessingItem(string filePath)
            {
                FilePath = filePath;
                CorrelationId = Guid.NewGuid();
            }
        }

        internal sealed class ItemProducerTaskParam
        {
            public BlockingCollection<ProcessingItem> ProcessingItems { get; private set; }

            public ItemProducerTaskParam(BlockingCollection<ProcessingItem> processingItems)
            {
                ProcessingItems = processingItems;
            }
        }

        private static int ItemProducerTaskAction(object taskParam)
        {
            var tp = taskParam as ItemProducerTaskParam;
            var logger = Logger.GetInstance();

            var trimChars = new char[] { ' ', '\t', '"', '\'' };
            var addedCount = 0;
            while (true)
            {
                var filePath = Console.ReadLine()?.Trim(trimChars);
                if (string.IsNullOrWhiteSpace(filePath)) break;
                var item = new ProcessingItem(filePath);
                tp.ProcessingItems.Add(item);
                addedCount++;

                logger.WriteLog(new LogRecord()
                {
                    CorrelationId = item.CorrelationId,
                    Message = string.Format(@"Added the update package file path ""{0}""", item.FilePath),
                }, nameof(Program));
            }
            tp.ProcessingItems.CompleteAdding();

            logger.WriteLog(new LogRecord()
            {
                Message = string.Format("Added {0} update package paths.", addedCount),
            }, nameof(Program));

            return addedCount;
        }

        internal sealed class WorkerTaskActionParam
        {
            public int WorkerId { get; private set; }
            public BlockingCollection<ProcessingItem> ProcessingItems { get; private set; }

            public WorkerTaskActionParam(int workerId, BlockingCollection<ProcessingItem> processingItems)
            {
                WorkerId = workerId;
                ProcessingItems = processingItems;
            }
        }

        private static (int Succeeded, int Failed) WorkerTaskAction(object taskParam)
        {
            var tp = taskParam as WorkerTaskActionParam;
            var logger = Logger.GetInstance();

            logger.WriteLog(new LogRecord()
            {
                Message = string.Format(@"The worker-{0} started.", tp.WorkerId),
            }, nameof(Program));

            var succeededCount = 0;
            var failedCount = 0;
            while (true)
            {
                ProcessingItem item;
                try
                {
                    item = tp.ProcessingItems.Take();
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
                        Message = string.Format(@"The processing started on the worker-{0}.", tp.WorkerId),
                    }, nameof(Program));

                    var updatePackage = UpdatePackage.RetrieveData(item.FilePath);
                    succeededCount++;

                    logger.WriteLog(new LogRecord()
                    {
                        CorrelationId = item.CorrelationId,
                        Message = string.Format(@"The processing ended on the worker-{0}.", tp.WorkerId),
                    }, nameof(Program));
                }
                catch (Exception e)
                {
                    failedCount++;
                    logger.WriteCorrelationLog(item.CorrelationId, e.ToString(), nameof(Program));
                }
            }

            logger.WriteLog(new LogRecord()
            {
                Message = string.Format("The worker-{0} ended. The results are {1} succeeded, {2} failed.", tp.WorkerId, succeededCount, failedCount),
            }, nameof(Program));

            return (succeededCount, failedCount);
        }
    }
}
