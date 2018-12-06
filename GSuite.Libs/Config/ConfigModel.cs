using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSuite.Libs.Config
{
    class ApiSettingsProperty
    {
        public string ClientId { get; set; }
        public string ProjectId { get; set; }
        public string AuthUri { get; set; }
        public string TokenUri { get; set; }
        public string AuthProvider { get; set; }
        public string ClientSecret { get; set; }
        public string[] RedirectUris { get; set; }
    }

    class GSuiteSignInProperty
    {
        public string Url { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string ReserveEmail { get; set; }
    }

    class InputDataProperty
    {
        public string GroupsFileName { get; set; }
        public string UsersFileName { get; set; }
    }

    class ConfigModel
    {
        public ApiSettingsProperty ApiSettings { get; set; }
        public GSuiteSignInProperty GSuiteSignIn { get; set; }
        public InputDataProperty InputData { get; set; }
    }
}
