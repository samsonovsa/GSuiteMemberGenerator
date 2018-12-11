using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using GSuite.Libs.Config;
using GSuite.Libs.Services.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Admin.Directory.directory_v1;
using Google.Apis.Admin.Directory.directory_v1.Data;
using Google.Apis.Services;
using Google.Apis.Requests;
using Google.Apis.Util.Store;

namespace GSuite.Libs.Services
{
    public class SerferAPIService : ISerferService
    {
        UserCredential _credential;
        IConfiguration _configuration;

        public event EventHandler<int> AddUsers;


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

        public async Task<bool> AddMembersToGroupAsync(IList<string> users, string groupName)
        {
            // Create Directory API service.
            var service = new DirectoryService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = "GSuiteGroups"
            });

            var requestGroupsList = service.Groups.List();
            requestGroupsList.Customer = "my_customer";
            var groups = await Task.Run(() => requestGroupsList.Execute().GroupsValue);
            var group = groups.Where(x => x.Name == groupName).FirstOrDefault();

            for (int i = 0; i <= users.Count / 25; i++)
            {
                try
                {

                    // Create a batch request.
                    BatchRequest request = new BatchRequest(service);
                    int countRequestAttempt = 0;
                    IEnumerable<string> top25users = users.Skip(i * 25).Take(25);
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

                                   if (error.Code == 403 && countRequestAttempt < 1)
                                   {
                                       // implement elivate algorrytm
                                       countRequestAttempt++;
                                       Task.Delay(countRequestAttempt ^ 2 * 2000).Wait();
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
        }



        public string CurrentPage => throw new NotImplementedException();

        public string CurrentUrl => throw new NotImplementedException();

        public event EventHandler<int> AddUsers;

        public Task<bool> AccessAsync(string url, string login, string password)
        {
            throw new NotImplementedException();
        }



        public Task CloseCurrentSession()
        {
            throw new NotImplementedException();
        }

        public Task NextPageAsync(string url)
        {
            throw new NotImplementedException();
        }
    }
}
