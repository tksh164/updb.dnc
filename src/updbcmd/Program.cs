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

            var sw = new Stopwatch();
            sw.Start();

            await ProcessUpdatePackages(settings);

            sw.Stop();
            Console.WriteLine("Elapsed: {0}", sw.Elapsed.TotalSeconds);
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
                        var consumerActionParams = new ConsumerActionParameters(processItems);
                        consumerTasks = new Task[settings.ConsumerThreadCount];
                        for (var i = 0; i < consumerTasks.Length; i++)
                        {
                            consumerTasks[i] = Task<(int Succeeded, int Failed)>.Factory.StartNew(ConsumerAction, consumerActionParams, TaskCreationOptions.LongRunning);
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
            var trimChars = new char[] { ' ', '\t', '"', '\'' };
            var addedCount = 0;
            while (true)
            {
                var filePath = Console.ReadLine()?.Trim(trimChars);
                if (string.IsNullOrWhiteSpace(filePath)) break;
                ap.ProcessItems.Add(new ProcessItem(filePath));
                addedCount++;
            }
            ap.ProcessItems.CompleteAdding();
            Console.WriteLine("Added count: {0}", addedCount);
            return addedCount;
        }

        internal sealed class ConsumerActionParameters
        {
            public BlockingCollection<ProcessItem> ProcessItems { get; private set; }

            public ConsumerActionParameters(BlockingCollection<ProcessItem> processItems)
            {
                ProcessItems = processItems;
            }
        }

        private static (int Succeeded, int Failed) ConsumerAction(object actionParams)
        {
            var ap = actionParams as ConsumerActionParameters;
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
                    Console.WriteLine("Succeeded count: {0}, Failed count: {1}", succeededCount, failedCount);
                    break;
                }

                try
                {
                    var updatePackage = UpdatePackage.RetrieveData(item.FilePath);
                    succeededCount++;
                }
                catch (Exception e)
                {
                    failedCount++;
                    Console.WriteLine(e.ToString());
                }
            }
            return (Succeeded: succeededCount, Failed: failedCount);
        }
    }
}
