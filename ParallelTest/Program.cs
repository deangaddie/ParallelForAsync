using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Bulkhead;

namespace ParallelTest
{
    internal class Program
    {
        private static async Task Main()
        {
            const int iterations = 20;
            const int subIterations = 2;
            const int subIterationMs = 1000;
            const int bulkheadCount = 10;
            const int maxWriteToFileAsync = 4;
            
            IFileSystemThing results = new FileSystemThing("results.log", 10);
            IFileSystemThing fileSystemThing = new FileSystemThing("output.log", maxWriteToFileAsync);

            AsyncBulkheadPolicy bulkhead = Policy.BulkheadAsync(bulkheadCount, int.MaxValue);

            for (var t = 0; t < 10; t++)
            {
                await RunSynchronousTasks(iterations, fileSystemThing, subIterations, subIterationMs, maxWriteToFileAsync, bulkheadCount, results);

                long asyncTimeInMs = await RunAsynchronousTasks(iterations, fileSystemThing, subIterations, subIterationMs, maxWriteToFileAsync, bulkheadCount, results);

                await RunBulkheadTasks(iterations, bulkhead, fileSystemThing, subIterations, subIterationMs, maxWriteToFileAsync, bulkheadCount, results);

                RunParallelForTasks(iterations, fileSystemThing, subIterations, subIterationMs, asyncTimeInMs);
            }
        }

        private static async Task RunSynchronousTasks(int iterations, IFileSystemThing fileSystemThing, int subIterations,
            int subIterationMs, int maxWriteToFileAsync, int bulkheadCount, IFileSystemThing results)
        {
            CancellationTokenSource syncTokenSource = new CancellationTokenSource();
            Console.WriteLine("Starting Synchronous");
            Stopwatch synchronousTime = new Stopwatch();
            synchronousTime.Start();
            for (int i = 0; i < iterations; i++)
            {
                if (syncTokenSource.IsCancellationRequested) break;
                Console.WriteLine($"Synch {i}");
                IDoSomeWork doSomeWork = new DoSomeWork(fileSystemThing);
                await doSomeWork.DoSomeWorkWhichTakesTime(i, subIterations, subIterationMs, syncTokenSource.Token)
                    .ConfigureAwait(false);
            }

            synchronousTime.Stop();
            string syncMessage =
                $"Synchronous elapsed |{synchronousTime.ElapsedMilliseconds}|ms for |{iterations}| iterations, MaxFileConcurrent: |{maxWriteToFileAsync}| Bulkheads: |{bulkheadCount}| SubIterations: |{subIterations}| SubMs: |{subIterationMs}|{Environment.NewLine}";
            await results.WriteToFile(syncMessage).ConfigureAwait(false);
            Console.WriteLine(syncMessage);

            Console.WriteLine("Ending Synchronous");
        }


        private static async Task<long> RunAsynchronousTasks(int iterations, IFileSystemThing fileSystemThing, int subIterations,
            int subIterationMs, int maxWriteToFileAsync, int bulkheadCount, IFileSystemThing results)
        {
            CancellationTokenSource asyncTokenSource = new CancellationTokenSource();
            Console.WriteLine("Starting Asynchronous");
            Stopwatch asynchronousTime = new Stopwatch();
            asynchronousTime.Start();

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < iterations; i++)
            {
                if (asyncTokenSource.IsCancellationRequested) break;
                Console.WriteLine($"Asynch {i}");
                IDoSomeWork doSomeWork = new DoSomeWork(fileSystemThing);
                tasks.Add(doSomeWork.DoSomeWorkWhichTakesTime(i, subIterations, subIterationMs, asyncTokenSource.Token));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            asynchronousTime.Stop();
            string asyncMessage =
                $"Asynchronous elapsed |{asynchronousTime.ElapsedMilliseconds}|ms for |{iterations}| iterations, MaxFileConcurrent: |{maxWriteToFileAsync}| Bulkheads: |{bulkheadCount}| SubIterations: |{subIterations}| SubMs: |{subIterationMs}|{Environment.NewLine}";
            await results.WriteToFile(asyncMessage).ConfigureAwait(false);
            Console.WriteLine(asyncMessage);

            Console.WriteLine("Ending Asynchronous");
            return asynchronousTime.ElapsedMilliseconds;
        }


        private static async Task RunBulkheadTasks(int iterations, AsyncBulkheadPolicy bulkhead,
            IFileSystemThing fileSystemThing, int subIterations, int subIterationMs, int maxWriteToFileAsync, int bulkheadCount,
            IFileSystemThing results)
        {
            CancellationTokenSource bulkheadTokenSource = new CancellationTokenSource();
            Console.WriteLine("Starting bulkhead");
            Stopwatch bulkheadTime = new Stopwatch();
            List<Task> bulkheadTasks = new List<Task>();
            bulkheadTime.Start();
            for (int i = 0; i < iterations; i++)
            {
                if (bulkheadTokenSource.IsCancellationRequested) break;
                int count = i;
                bulkheadTasks.Add(bulkhead.ExecuteAsync(async () =>
                {
                    if (bulkheadTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    Console.WriteLine($"Bulk {count}");
                    IDoSomeWork doSomeWork = new DoSomeWork(fileSystemThing);
                    await doSomeWork.DoSomeWorkWhichTakesTime(count, subIterations, subIterationMs, bulkheadTokenSource.Token)
                        .ConfigureAwait(false);
                }));
            }

            await Task.WhenAll(bulkheadTasks).ConfigureAwait(false);

            bulkheadTime.Stop();
            string bulkMessage =
                $"Bulkhead elapsed |{bulkheadTime.ElapsedMilliseconds}|ms for |{iterations}| iterations, MaxFileConcurrent: |{maxWriteToFileAsync}| Bulkheads: |{bulkheadCount}| SubIterations: |{subIterations}| SubMs: |{subIterationMs}|{Environment.NewLine}";
            await results.WriteToFile(bulkMessage).ConfigureAwait(false);
            Console.WriteLine(bulkMessage);

            Console.WriteLine("Ending bulkhead");
        }

        private static void RunParallelForTasks(int iterations, IFileSystemThing fileSystemThing, int subIterations,
            int subIterationMs, long expectedTimeToWait)
        {
            Console.WriteLine("Starting Parallel.For");

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            Stopwatch stopwatch = new Stopwatch();

            Console.WriteLine(
                "Not writing out timer for parallel.for because it just completes calling the async's in <5ms and then goes on, it will never wait.");
            Console.WriteLine(
                "Watch the console for when tasks are kicked off. This one kicks them all off at once.");
            Console.WriteLine(
                "We can't even get the Tasks to await at the end because they are fire-and-forget");
            Console.WriteLine(
                "We could add to a list and await-whenAll, but this is to show what parallel.for is doing.");
            
            stopwatch.Start();
            Parallel.For(0, iterations, async i =>
            {
                if (cancellationTokenSource.IsCancellationRequested) return;
                Console.WriteLine($"Parallel {i}");
                IDoSomeWork doSomeWork = new DoSomeWork(fileSystemThing);
                    await doSomeWork.DoSomeWorkWhichTakesTime(i, subIterations, subIterationMs, cancellationTokenSource.Token)
                        .ConfigureAwait(false);
                
            });
            Console.WriteLine("Ending Parallel.For");
            
            Console.Write("Waiting a bit for the tasks to complete...");
            while (stopwatch.ElapsedMilliseconds < expectedTimeToWait + 1000)
            {
                Console.Write(".");
                Thread.Sleep(1000);
            }

            stopwatch.Stop();

            Console.WriteLine("Finished waiting. Let's hope they are all completed.");
        }
    }
}
