using System.Threading.Tasks;

namespace ParallelTest
{
    public interface IFileSystemThing
    {
        Task WriteToFile(string line);
    }
}