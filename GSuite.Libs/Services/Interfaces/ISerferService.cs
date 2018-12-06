using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSuite.Libs.Services.Interfaces
{
    public interface ISerferService
    {
        Task<bool> AccessAsync(string url, string login, string password);
        Task NextPageAsync(string url);
        Task<int> AddMembersToGroupAsync(IList<string> users, string group);
        Task CloseCurrentSession();

        string CurrentPage { get; }
        string CurrentUrl { get; }

        event EventHandler<int> AddUsers;
    }
}
