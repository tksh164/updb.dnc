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
            await ProcessUpdatePackages(6);
        }

        private static async Task ProcessUpdatePackages(int numOfConsumerTasks)
        {
            using (var processItems = new BlockingCollection<string>())
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

        internal sealed class ProducerActionParameters
        {
            public BlockingCollection<string> ProcessItems { get; private set; }

            public ProducerActionParameters(BlockingCollection<string> processItems)
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
                ap.ProcessItems.Add(filePath);
                count++;
            }
            ap.ProcessItems.CompleteAdding();
            Console.WriteLine("Complete adding: {0}", count);
            return count;
        }

        internal sealed class ConsumerActionParameters
        {
            public BlockingCollection<string> ProcessItems { get; private set; }

            public ConsumerActionParameters(BlockingCollection<string> processItems)
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
                string filePath;
                try
                {
                    filePath = ap.ProcessItems.Take();
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine("Complete take: {0}", count);
                    break;
                }
                var updatePackage = UpdatePackage.RetrieveData(filePath);
                count++;
            }
            return count;
        }
    }
}
