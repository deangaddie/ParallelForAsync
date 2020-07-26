using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelTest
{
    public class DoSomeWork : IDoSomeWork
    {
        private readonly IFileSystemThing _fileSystemThing;

        public DoSomeWork(IFileSystemThing fileSystemThing)
        {
            _fileSystemThing = fileSystemThing;
        }

        public async Task DoSomeWorkWhichTakesTime(int id, int iterations, int subMs, CancellationToken cancellationToken)
        {
            Stopwatch totalTime = new Stopwatch();
            totalTime.Start();
            for (int i = 0; i < iterations; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Cancellation requested.");
                    break;
                }

                int count = i;
                await Task.Run(() =>
                {
                    Stopwatch innerStopwatch = new Stopwatch();
                    innerStopwatch.Start();
                    while (innerStopwatch.ElapsedMilliseconds < subMs)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Console.WriteLine("Cancellation requested.");
                            break;
                        }
                        Random random = new Random(id + DateTime.Now.Millisecond);
                        int someNumber = id;
                        int otherNumber = someNumber * random.Next();
                        for (int j = 0; j < 100; j++)
                        {
                            if (j % 2 == 0)
                            {
                                someNumber += otherNumber;
                            }
                            else
                            {
                                someNumber -= otherNumber;
                            }

                            _fileSystemThing.WriteToFile(
                                $"{id} - {count} - {j} - {someNumber}{Environment.NewLine}");
                        }
                    }
                    innerStopwatch.Stop();
                }, cancellationToken).ConfigureAwait(false);
            }
            totalTime.Stop();
            if (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Exiting from cancellation request.");
            }
        }
    }
}
