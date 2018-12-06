
namespace GSuite.Libs.Config
{
    public  interface IConfiguration
    {
        string GetClientId();
        string GetProjectId();
        string GetAuthUri();
        string GetTokenUri();
        string GetAuthProvider();
        string GetClientSecret();
        string[] RedirectUris();
        string GetURL();
        string GetLogin();
        string GetReserveEmail();
        string GetPassword();
        string GetGroupsFileName();
        string GetUsersFileName();
    }
}
