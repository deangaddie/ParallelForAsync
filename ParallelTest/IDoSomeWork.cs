using System.Threading;
using System.Threading.Tasks;

namespace ParallelTest
{
    public interface IDoSomeWork
    {
        /// <summary>
        /// This isn't the iterations being tested. This is purely something which runs to take up time and keep a thread busy for a bit. 
        /// </summary>
        /// <param name="id">An ID for this work item</param>
        /// <param name="iterations">The number of times this should run the loop</param>
        /// <param name="subMs">The min time we want each loop to run for.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task to await.</returns>
        Task DoSomeWorkWhichTakesTime(int id, int iterations, int subMs, CancellationToken cancellationToken);
        
    }
}
