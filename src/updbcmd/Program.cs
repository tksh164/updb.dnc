using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UPDB.Gathering;

namespace updbcmd
{
    class Program
    {
        static async Task Main(string[] args)
        {
            const int ConsumerThreadCount = 6;
            await ProcessUpdatePackages(ConsumerThreadCount);
        }

        private static async Task ProcessUpdatePackages(int numOfConsumerTasks)
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
                        consumerTasks = new Task[numOfConsumerTasks];
                        for (var i = 0; i < consumerTasks.Length; i++)
                        {
                            consumerTasks[i] = Task<int>.Factory.StartNew(ConsumerAction, consumerActionParams, TaskCreationOptions.LongRunning);
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
                CorrelationId = new Guid();
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
            var count = 0;
            while (true)
            {
                var filePath = Console.ReadLine()?.Trim(trimChars);
                if (string.IsNullOrWhiteSpace(filePath)) break;
                ap.ProcessItems.Add(new ProcessItem(filePath));
                count++;
            }
            ap.ProcessItems.CompleteAdding();
            Console.WriteLine("Complete adding: {0}", count);
            return count;
        }

        internal sealed class ConsumerActionParameters
        {
            public BlockingCollection<ProcessItem> ProcessItems { get; private set; }

            public ConsumerActionParameters(BlockingCollection<ProcessItem> processItems)
            {
                ProcessItems = processItems;
            }
        }

        private static int ConsumerAction(object actionParams)
        {
            var ap = actionParams as ConsumerActionParameters;
            var count = 0;
            while (true)
            {
                ProcessItem item;
                try
                {
                    item = ap.ProcessItems.Take();
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine("Complete take: {0}", count);
                    break;
                }
                var updatePackage = UpdatePackage.RetrieveData(item.FilePath);
                count++;
            }
            return count;
        }
    }
}
