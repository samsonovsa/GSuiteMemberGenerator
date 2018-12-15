using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSuite.Libs.Interface;
using GSuite.Libs.Services.Interfaces;
using GSuite.Libs.Models;
using GSuite.Libs.Config;
using Autofac;

namespace GSuite.Libs.Services
{
    class GSuiteDataGenerator : IGSuiteDataGenerator
    {
        List<Member> _users = new List<Member>();
        List<Group> _groups = new List<Group>();
        IEntityReader _reader;
        IConfiguration _config;
        IWorker _worker;
        string _userFileName;
        string _groupFileName;

        public event EventHandler<int> GenerationPercentComplete;

        public GSuiteDataGenerator(IEntityReader reader, IConfiguration configuration, IWorker worker)
        {
            _reader = reader;
            _config = configuration;
            _worker = worker;
        }

        public async Task GenerateAsync()
        {
            try
            {
                SetGrops();
                SetUsers();
                _worker.UniversalEvent += (s, e) => { Console.WriteLine(e); };
                await _worker.AuthorizationAsync(_config);
                int operationCount = await _worker.CreateGroupAsync(_groups);
                GenerationPercentComplete?.Invoke(this, operationCount);

                // await  _worker.CreateUsersAsync(_users);
                await _worker.CreateMembersAsync(_users);
                
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Message[Entity already exists.]"))
                    throw new Exception("Error creating group. The group already exists.");
                throw e;
            }

            //if (GenerationPercentComplete != null)
            //{
            //    GenerationPercentComplete.Invoke(this, _users.Count);
            //}


        }

        private void _worker_UniversalEvent(object sender, string e)
        {
            throw new NotImplementedException();
        }

        public void SetGropsFileName(string fileName)
        {
            _groupFileName = fileName;
        }

        public void SetUsersFileName(string fileName)
        {
            _userFileName = fileName;
        }

        private void SetGrops()
        {
            // Temp stub
            _groupFileName = _config.GetGroupsFileName();

            foreach (Entity item in _reader.GetEntityes(_groupFileName))
                if (item.Validator())
                    _groups.Add(new Group(item.Name));

            //  Remove duplicates
            _groups = _groups.GroupBy(x => x.Name).Select(y => y.FirstOrDefault()).ToList();

        }

        private void SetUsers()
        {
            // Temp stub
            _userFileName = _config.GetUsersFileName();

            foreach (Entity item in _reader.GetEntityes(_userFileName))
            {
                Member user = new Member(item.Name);
                if (user.Validator())
                    _users.Add(user);
            }

            //  Remove duplicates
            _users = _users.GroupBy(x => x.Name).Select(y => y.FirstOrDefault()).ToList();
            

            // Distribution of users into groups
            _users = _users.Select((item, index) => new { Index = index, Value = item }) 
                       .GroupBy(x => x.Index % _groups.Count)
                       .SelectMany(x => x.Select((j,i)=> new Member {
                           GroupName = _groups[x.Key].Name,
                           Name = j.Value.Name })).ToList();
        }
    }
}
