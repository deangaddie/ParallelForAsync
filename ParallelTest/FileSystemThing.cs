using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelTest
{
    public class FileSystemThing : IFileSystemThing
    {
        private readonly string _filePathAndName;
        private static SemaphoreSlim _semaphore;

        public FileSystemThing(string filePathAndName, int maxSemaphore)
        {
            _filePathAndName = filePathAndName;
            // Create the semaphore.
            _semaphore = new SemaphoreSlim(maxSemaphore, maxSemaphore);
        }

        public async Task WriteToFile(string line)
        {
            _semaphore.Wait();
            await File.AppendAllTextAsync(_filePathAndName, line, CancellationToken.None).ConfigureAwait(false);
            _semaphore.Release();
        }
    }
}