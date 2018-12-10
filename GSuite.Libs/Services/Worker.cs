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


        public event EventHandler<string> UniversalEvent;
        public event EventHandler<AddingMembersStateEventArgs> AddingMemberIterruptEvent;

        public Worker(ISerferService serfer)
        {
            _serfer = serfer;
        }

        public async Task AuthorizationAsync(IConfiguration configuration)
        {
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

            _configuration = configuration;

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
                   
            return countCreatadGroups;
        }

        public async Task<int> CreateMembersAsync(List<Models.User> users)
        {
            int countAddingMembers = 0;

            // Create Directory API service.
            var service = new DirectoryService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = "GSuiteGroups"
            });


            var requestGroupsList = service.Groups.List();
            requestGroupsList.Customer = "my_customer";
            var existingGroup = await Task.Run(() => requestGroupsList.Execute().GroupsValue);

            IList<string> addedGroups = users.GroupBy(u => u.GroupName)
               .Select(grp => grp.First().GroupName)
               .ToList();

            existingGroup = existingGroup.Where(x => addedGroups.Contains(x.Name)).ToList();
                


            foreach (var group in existingGroup)
            { 

                List<string> usersInCurrentGroup = users.Where(u => u.GroupName == group.Name).Select(u => u.Name).ToList();



                for (int i = 0; i <= usersInCurrentGroup.Count / 25; i++)
                {
                    try
                    {

                    // Create a batch request.
                    BatchRequest request = new BatchRequest(service);
                    int countRequestAttempt = 0;
                    IEnumerable<string> top25users = usersInCurrentGroup.Skip(i * 25).Take(25);
                   // List<Member> listMembers = new List<Member>();
                    foreach (string user in top25users)
                    {
                        // listMembers.Add(new Member() {Email = user, Role = "MEMBER" });
                        // Members members = new Members() { MembersValue = listMembers };
                        request.Queue<Member>(service.Members.Insert(
                            new Member()
                            {
                                Email = user,
                                Role = "MEMBER"
                            }
                            , group.Id),
                           (content, error, x, message) => 
                         {
                             // Put your callback code here.
                             if (error != null)
                             {
                                 if (error.Code == 403 && countRequestAttempt >= 1)
                                     return;

                                 if (error.Code==403 && countRequestAttempt<1)
                                 {
                                     // implement elivate algorrytm
                                     countRequestAttempt++;
                                     Task.Delay(countRequestAttempt^2*2000).Wait();
                                     request.ExecuteAsync();

                                 }
                                else

                                     
                                     UniversalEvent?.BeginInvoke(this, error.Message, null, null);
                             }
                                 
                         });

                        countAddingMembers++;
                    }

                    countRequestAttempt = 0;
                    await request.ExecuteAsync();
                    UniversalEvent?.BeginInvoke(this, String.Format("Add {0} members to {1} group.", countAddingMembers, group.Name), null, null);

                    }
                    catch (Exception e)
                    {

                        UniversalEvent?.BeginInvoke(this, String.Format("Error {0}", e.Message), null, this);
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
