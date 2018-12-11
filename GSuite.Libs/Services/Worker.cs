using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using GSuite.Libs.Config;
using GSuite.Libs.Models;
using GSuite.Libs.Services.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Admin.Directory.directory_v1;
using Google.Apis.Admin.Directory.directory_v1.Data;
using Google.Apis.Services;
using Google.Apis.Requests;
using Google.Apis.Util.Store;
using GSuite.Libs.Helpers;

namespace GSuite.Libs.Services
{
    class Worker : IWorker
    {
        UserCredential _credential;
        IConfiguration _configuration;
        ISerferService _serfer;

        List<string> _existGroups;
        List<Models.User> _membersLeftToAdd;
        int _availableRepeatConnection = 10;


        public event EventHandler<string> UniversalEvent;
        //public event EventHandler<AddingMembersStateEventArgs> AddingMemberIterruptEvent;

        public Worker(ISerferService serfer)
        {
            _serfer = serfer;
        }

        public async Task AuthorizationAsync(IConfiguration configuration)
        {
            _configuration = configuration;

            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        new ClientSecrets
                        {
                            ClientId = configuration.GetClientId(),
                            ClientSecret = configuration.GetClientSecret()
                        },
            new[] { DirectoryService.Scope.AdminDirectoryGroup, DirectoryService.Scope.AdminDirectoryGroupMember },
            configuration.GetLogin(),
            CancellationToken.None,
            new FileDataStore("AuthStore"));

        }

        public async Task<int> CreateGroupAsync(IEnumerable<Models.Group> groups)
        {
            int countCreatadGroups=0;

            // Create Directory API service.
            var service = new DirectoryService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = "GSuiteGroups"
            });

            var requestGroupsList = service.Groups.List();
            requestGroupsList.Customer = "my_customer";
            var existingGroup = await Task.Run(() => requestGroupsList.Execute().GroupsValue);

            foreach (var item in groups)
            {
                if (existingGroup.Count(x=>x.Email.ToLower() == item.Name.ToLower())==0)
                {
                    var request = service.Groups.Insert(new Google.Apis.Admin.Directory.directory_v1.Data.Group() { Name = item.Name, Email = item.Name });
                    await Task.Run(() => request.Execute());
                    countCreatadGroups++;
                }
            }

            // Get existing groups in GSuite
            var _existGroups = await Task.Run(() => requestGroupsList.Execute().GroupsValue);

            return countCreatadGroups;
        }

        public async Task<int> CreateMembersAsync(List<Models.User> users)
        {
            int countAddingMembers = 0;
            int countAddinMembersInCurrentGroup = 0;

            _serfer.AddUsers += ((o, x) =>
            {
                countAddingMembers = countAddingMembers + x;
                countAddinMembersInCurrentGroup = countAddinMembersInCurrentGroup + x;
            });

            await _serfer.AuthorizationAsync(_configuration);


            _membersLeftToAdd = users;

            IList<string> addedGroups = users.GroupBy(u => u.GroupName)
               .Select(grp => grp.First().GroupName)
               .ToList();

            _existGroups = _existGroups.Where(x => addedGroups.Contains(x)).ToList();
                


            foreach (var group in _existGroups)
            {
                countAddinMembersInCurrentGroup = 0;
                List<string> usersInCurrentGroup = users.Where(u => u.GroupName == group).Select(u => u.Name).ToList();

                if(await _serfer.AddMembersToGroupAsync(usersInCurrentGroup, group))
                {
                    // Save current state 
                    _membersLeftToAdd.RemoveAll(x => x.GroupName == group);
                    _existGroups.Remove(group);
                }
                else
                {
                   if( (_availableRepeatConnection--) >0 )
                    {
                        _membersLeftToAdd.RemoveRange(0, countAddinMembersInCurrentGroup);
                        await CreateMembersAsync(_membersLeftToAdd);
                    }
                }




                //var request = service.Groups.Update(new Google.Apis.Admin.Directory.directory_v1.Data.Group() { Name = item.Name, Email = item.Name });
                //await Task.Run(() => request.Execute());
                //  countAddingMembers++;


                // UniversalEvent?.BeginInvoke(this, String.Format("Add {0} members to {1} group.", countAddingMembers, group.Name), null, null);
            }

            

            return countAddingMembers;



        }

        public async Task CreateUsersAsync(List<Models.User> users)
        {
            int countAddingMembers = 0;

            bool isAccess = await _serfer.AccessAsync("https://admin.google.com", _configuration.GetLogin(), _configuration.GetPassword());
            if (isAccess)
            { 
                UniversalEvent?.BeginInvoke(this, "Page loaded", null, null);

                IList<string> groups =  users.GroupBy(u => u.GroupName)
                       .Select(grp => grp.First().GroupName)
                       .ToList();

                foreach(string group in groups)
                {
                    int countAttempts = 0;  // counter attempts re-entry to G Suite in case of session interruption

                    List<string> usersInCurrentGroup = users.Where(u => u.GroupName == group).Select(u=>u.Name).ToList();

                    while (usersInCurrentGroup.Count > 0)
                    {
                        countAddingMembers = await _serfer.AddMembersToGroupAsync(usersInCurrentGroup, group);
                        // Delete added members to next restore (like control point in transaction)
                        usersInCurrentGroup.RemoveRange(0, countAddingMembers);
                        UniversalEvent?.BeginInvoke(this, String.Format("Add {0} members to {1} group.", countAddingMembers, group), null, null);
                        countAddingMembers = 0;

                        if(usersInCurrentGroup.Count > 0)
                        {
                            await _serfer.CloseCurrentSession();
                            isAccess = await _serfer.AccessAsync("https://admin.google.com", _configuration.GetLogin(), _configuration.GetPassword());
                            if(!isAccess)
                            {
                                UniversalEvent?.BeginInvoke(this, "Can not access to G Suite Admin Console ", null, null);
                                return;
                            }
                        }

                        if (countAttempts++ > 5)
                        {
                            UniversalEvent?.BeginInvoke(this, String.Format("Can not add {0} members to {1} group. Service unavalible",
                                usersInCurrentGroup.Count, group), null, null);
                            break;
                        }
                        
                    }

                }
            }
            else
              UniversalEvent?.BeginInvoke(this,"Can not access to G Suite Admin Console ",null,null);
        }
    }
}
