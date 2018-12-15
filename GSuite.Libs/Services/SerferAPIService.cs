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

        public event EventHandler<string> UniversalEvent;
        public event EventHandler<int> AddMembersEvent;

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

        public async Task<bool> AddMembersToGroupAsync(IList<string> members, string groupId)
        {
            int countAddingMembers = 0;

            // Create Directory API service.
            var service = new DirectoryService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = "GSuiteGroups"
            });

            // splitting the list into packages by 25 members
            for (int i = 0; i <= members.Count / 25; i++)
            {
                try
                {
                    // Create a batch request.
                    BatchRequest request = new BatchRequest(service);

                    IEnumerable<string> top25members = members.Skip(i * 25).Take(25);
                    int countRequestAttempt = 0;    // for implement elivate algorrytm

                    foreach (string member in top25members)
                    {
                        request.Queue<Member>(service.Members.Insert(
                            new Member()
                            {
                                Email = member,
                                Role = "MEMBER"
                            }
                            , groupId),
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
                    AddMembersEvent?.BeginInvoke(this,countAddingMembers, null, null);
                  

                }
                catch (Exception e)
                {

                    UniversalEvent?.BeginInvoke(this, String.Format("Error {0}", e.Message), null, this);
                    return false;
                }

            }

            return true;
        }



        public string CurrentPage => throw new NotImplementedException();

        public string CurrentUrl => throw new NotImplementedException();



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
