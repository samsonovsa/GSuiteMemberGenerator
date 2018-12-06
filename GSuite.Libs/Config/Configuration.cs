using System;
using System.IO;
using Newtonsoft.Json;


namespace GSuite.Libs.Config
{
    public  class Configuration : IConfiguration
    {
        ConfigModel _config;

        public Configuration()
        {
            try
            {
                // deserialize JSON directly from a file
                using (StreamReader file = File.OpenText(@"appsettings.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    _config = (ConfigModel)serializer.Deserialize(file, typeof(ConfigModel));
                }
            }
            catch (Exception err)
            {
                throw new Exception( String.Format("Error config reading. ({0}) ",err.Message));
            }

        }

        public string GetPassword()
        {
            return _config.GSuiteSignIn.Password;
        }

        public string GetLogin()
        {
            return _config.GSuiteSignIn.Login;
        }

        public string GetURL()
        {
            return _config.GSuiteSignIn.Url;
        }

        public string GetReserveEmail()
        {
            return _config.GSuiteSignIn.ReserveEmail;
        }

        public string GetGroupsFileName()
        {
            return _config.InputData.GroupsFileName;
        }

        public string GetUsersFileName()
        {
            return _config.InputData.UsersFileName;
        }

        public string GetClientId()
        {
            return _config.ApiSettings.ClientId;
        }

        public string GetProjectId()
        {
            return _config.ApiSettings.ProjectId;
        }

        public string GetAuthUri()
        {
            return _config.ApiSettings.AuthUri;
        }

        public string GetTokenUri()
        {
            return _config.ApiSettings.TokenUri;
        }

        public string GetAuthProvider()
        {
            return _config.ApiSettings.AuthProvider;
        }

        public string GetClientSecret()
        {
            return _config.ApiSettings.ClientSecret;
        }

        public string[] RedirectUris()
        {
            return _config.ApiSettings.RedirectUris;
        }
    }


}
