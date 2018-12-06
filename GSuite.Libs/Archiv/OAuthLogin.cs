using System;
using System.Threading.Tasks;
using System.Web;
using MYOB.AccountRight.SDK;
using Myob.Service.Config;

namespace Myob.Service.Helpers
{
    internal class OAuthLogin
    {
        private const string CsOAuthServer = "https://secure.myob.com/oauth2/account/authorize/";

        private const string CsOAuthScope = "CompanyFile";

        private string _email;
        private string _password;

        public OAuthLogin(IConfiguration config)
        {
            _email = config.GetEmail();
            _password = config.GetMyobPassword();
        }

        /// <summary>
        /// Function to return the OAuth code
        /// </summary>
        /// <param name="config">Contains the API configuration such as ClientId and Redirect URL</param>
        /// <returns>OAuth code</returns>
        /// <remarks></remarks>
        public string GetAuthorizationCode(IApiConfiguration configAPI)
        {
            //Format the URL so  User can login to OAuth server
            string url = string.Format("{0}?client_id={1}&redirect_uri={2}&scope={3}&response_type=code", CsOAuthServer,
                                       configAPI.ClientId, HttpUtility.UrlEncode(configAPI.RedirectUrl), CsOAuthScope);

            string code;

            // Create a new form with a web browser to display OAuth login page
            MyobAuthWebBrowser browser = new MyobAuthWebBrowser(_email, _password);

            try
            {
                Task<string> task = browser.GetCode(url, TimeSpan.FromSeconds(30));
                code =  task.Result;        
            }
            catch (Exception e)
            {

                throw new Exception(e.InnerException!=null ? e.InnerException.Message: e.Message);
            }

            return code;

        }

    }
}
