using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSuite.Libs.Config;

namespace GSuite.Libs.Services.Interfaces
{
    public interface ISerferService
    {
        Task AuthorizationAsync(IConfiguration configuration);
        Task<bool> AddMembersToGroupAsync(IList<string> users, string group);

        Task<bool> AccessAsync(string url, string login, string password);
        Task NextPageAsync(string url);
        Task CloseCurrentSession();

        string CurrentPage { get; }
        string CurrentUrl { get; }

        event EventHandler<int> AddUsers;
    }
}
