using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSuite.Libs.Config;
using GSuite.Libs.Models;

namespace GSuite.Libs.Services.Interfaces
{
    interface IWorker
    {
        Task AuthorizationAsync(IConfiguration configuration);
        Task<int> CreateGroupAsync(IEnumerable<Group> groups);
        Task CreateUsersAsync(List<Member> users);
        Task<int> CreateMembersAsync(List<Models.Member> users);
        event EventHandler<string> UniversalEvent;

    }
}
