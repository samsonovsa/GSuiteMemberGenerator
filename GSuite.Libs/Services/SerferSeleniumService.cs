using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSuite.Libs.Services.Interfaces;
using System.Windows.Forms;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;


namespace GSuite.Libs.Services
{
    class SerferSeleniumService : ISerferService
    {
        string _login;
        string _password;
        string _currentPage;
        string _currentUrl;


        IWebDriver _browser;             
        CancellationToken _cancelToken;

        #region Public Propertyes

        public event EventHandler<int> AddUsers;

        public string CurrentPage
        {
            get { return _currentPage; }
        }

        public string CurrentUrl
        {
            get { return _currentUrl; }
        }

        #endregion

        //public SerferSeleniumService()
        //{
            
        //}


        public async Task<bool> AccessAsync(string url, string login, string password)
        {

            //var chromeOptions = new OpenQA.Selenium.Chrome.ChromeOptions();
            //chromeOptions.AddArguments(new List<string>() { "headless" });
            //var chromeDriverService = OpenQA.Selenium.Chrome.ChromeDriverService.CreateDefaultService();
            //chromeDriverService.HideCommandPromptWindow = true;    // This is to hidden the console.

            //  _browser = new OpenQA.Selenium.Firefox.FirefoxDriver();
            //  _browser = new OpenQA.Selenium.Chrome.ChromeDriver(chromeDriverService, chromeOptions);
            // _browser = new OpenQA.Selenium.Edge.EdgeDriver();

            _browser = new OpenQA.Selenium.Chrome.ChromeDriver();
            await Task.Run(()=> _browser.Navigate().GoToUrl(url));
            _browser.Manage().Window.Maximize();

            // if(_browser.FindElement(By.TagName("Title") == "")
            try
            {
                IWebElement lioginInput = _browser.FindElement(By.Id("identifierId"));
                lioginInput.SendKeys(login);
                lioginInput.SendKeys(OpenQA.Selenium.Keys.Enter);
                WaitForPageUntilElementIsVisible(By.Id("identifierId"), 5);
                Task.Delay(5000).Wait();
            }
            catch (Exception)
            {

               
            }


            //  IWebElement passwordInput = _browser.FindElement(By.ClassName("Xb9hP")); 

            try
            {
                IWebElement passwordInput2 = _browser.FindElement(By.Name("password"));
                passwordInput2.SendKeys(password);
                passwordInput2.SendKeys(OpenQA.Selenium.Keys.Enter);
                WaitForPageUntilElementIsVisible(By.Name("password"), 5);
            }
            catch (Exception)
            {

            }

            // check access
            this.WaitUntilLoaded();
            await  this.NextPageAsync("https://admin.google.com");

            if (_browser.Url.StartsWith("https://accounts.google.com")  )// if url did not redirected
                return false;
            else
                return true;
        }


        public async Task NextPageAsync(string url)
        {
            await Task.Run(() => _browser.Navigate().GoToUrl(url));
        }

        public async Task<int> AddMembersToGroupAsync(IList<string> users, string group)
        {

            int countAddingMembers = 0;

            await Task.Run(() =>
             _browser.Navigate().GoToUrl(String.Format("https://admin.google.com/AdminHome?fral=1&groupId={0}&startUserName&startNum&chromeless=1#OGX:Group", group))
            );
            try
            {
                IWebElement form = _browser.FindElement(By.ClassName("gwt-Frame"));       // _browser.FindElement(By.Id("addmember"));
                _browser.SwitchTo().Frame(form);
                WaitForPageUntilElementIsVisible(By.Id("addmember"), 3);
            }
            catch (Exception)
            {


            }

            for (int i = 0; i <= users.Count/25; i++)
            {
                IEnumerable<string> top25users = users.Skip(i*25).Take(25);
                string members = ListToString(top25users);

                try
                {
                    IWebElement areaInput = _browser.FindElement(By.Id("add-members-textarea"));
                    if (areaInput.Text != string.Empty)
                        areaInput.Clear();

                    areaInput.Click();
                    areaInput.SendKeys(members);

                    _browser.FindElement(By.Id("add-members-add-button")).Click();
                    WaitForPageUntilElementIsVisible(By.Id("add-members-add-button"), 5);
                    countAddingMembers = countAddingMembers + top25users.Count();
                }
                catch (Exception)
                {
                    try
                    {
                        IWebElement errResponse = _browser.FindElement(By.Id("errormsg_0_members"));
                    }
                    catch (Exception)
                    {

                        return countAddingMembers;
                    }

                    return countAddingMembers;
                    // check error    id = "errormsg_0_members"  //"gptarnold@aol.comu
                    //"add-invite-users"
                }

            }

            return countAddingMembers;
        }


        private IWebElement WaitForPageUntilElementIsVisible(By locator, int maxSeconds)
        {
            return new WebDriverWait(_browser, TimeSpan.FromSeconds(maxSeconds))
                .Until(ExpectedConditions.ElementExists(locator));
        }

        private void WaitUntilLoaded()
        {
            WebDriverWait wait = new WebDriverWait(_browser, TimeSpan.FromSeconds(30));
            wait.Until((x) =>
            {
                return ((IJavaScriptExecutor)this._browser)
                .ExecuteScript("return document.readyState").Equals("complete");
            });
        }

        private string ListToString(IEnumerable<string> collection)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (string item in collection)
                stringBuilder.Append(item+", ");

            stringBuilder.Remove(stringBuilder.Length - 2, 2);
            return stringBuilder.ToString();
        }

        public async Task CloseCurrentSession()
        {
            try
            {
                await Task.Run(() => _browser.Close());
            }
            catch (Exception)
            {

            }

        }
    }
}
