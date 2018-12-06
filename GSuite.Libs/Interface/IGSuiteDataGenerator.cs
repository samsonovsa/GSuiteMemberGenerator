using System;
using System.Threading.Tasks;

namespace GSuite.Libs.Interface
{
    public interface IGSuiteDataGenerator
    {
        Task GenerateAsync();
        void SetGropsFileName(string fileName);
        void SetUsersFileName(string fileName);

        event EventHandler<int> GenerationPercentComplete;
    }
}
