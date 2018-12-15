using System;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using GSuite.Libs.Helpers;
using GSuite.Libs.Services.Interfaces;
using System.Collections.Generic;
using GSuite.Libs.Config;

//using System.Web;


namespace GSuite.Libs
{
    class SerferService: ISerferService, IDisposable
    {
        string _login;
        string _password;
        string _currentPage;
        string _currentUrl;

        MessageLoopApartment _apartment;   
        WebBrowser _browser;             // _webBrowser is created on a separate thread, with MessageLoopApartment.Run
        CancellationToken _cancelToken;

 #region Public Propertyes

        public event EventHandler<int> AddUsers;
        public event EventHandler<int> AddMembersEvent;
        public event EventHandler<string> UniversalEvent;

        public string CurrentPage
        {
            get { return _currentPage; }
        }

        public string CurrentUrl
        {
            get { return _currentUrl; }
        }

        #endregion

                                          // will be remove
                                            public EventHandler GetTokenComplete;
                                            string _code;


        public SerferService()
        {
            // CancellationToken cancelToken

            WebBrowserExt.SetFeatureBrowserEmulation();
            _cancelToken = new CancellationToken();

            // create an independent STA thread
            _apartment = new MessageLoopApartment();

            // create a WebBrowser on that STA thread
            _browser = _apartment.Run(() => new WebBrowser(), _cancelToken).Result;

            //// Add a handler for the web browser to auto comlete registration
            //_browser.DocumentCompleted += WebBrowser_DocumentCompleted;
            //// Add a handler for the web browser to capture content change 
            //_browser.DocumentTitleChanged += WebBrowser_DocumentTitleChanged;


        }


        public async Task<string> GetCode(string url, TimeSpan ts)
        {
            CancellationToken token;

            TaskCompletionSource<EventArgs> tcs = new TaskCompletionSource<EventArgs>();
            GetTokenComplete += (o, args) =>
            {
                try
                {
                    tcs.TrySetResult(args);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            };

            // cancel in 30s or when the main token is signalled
            var navigationCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            navigationCts.CancelAfter((int)ts.TotalMilliseconds);
            var navigationToken = navigationCts.Token;


            using (var apartment = new MessageLoopApartment())
            {
                // create WebBrowser inside MessageLoopApartment ( create an independent STA thread)
                var webBrowser = apartment.Invoke(() => new WebBrowser());

                // Add a handler for the web browser to auto comlete registration
                webBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;
                // Add a handler for the web browser to capture content change 
                webBrowser.DocumentTitleChanged += WebBrowser_DocumentTitleChanged;

                try
                {
                    // run the navigation task inside MessageLoopApartment
                       string html = await apartment.Run(() =>
                            webBrowser.NavigateAsync(url, navigationToken), navigationToken);

                    // wait for end of atuth
                    var timeoutTask = Task.Delay(ts);
                    var winner = await Task.WhenAny(tcs.Task, timeoutTask);
                    if (winner == timeoutTask)
                        throw new TimeoutException("Authentification timeout");

                    return _code; ;

                }
                finally
                {
                    // dispose of WebBrowser inside MessageLoopApartment
                    apartment.Invoke(() => webBrowser.Dispose());
                }


            }


        }

        #region Events Methods

        /// <summary>
        /// Hendler  that is called when document load complete in  browser 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WebBrowser_DocumentCompleted(object sender, System.Windows.Forms.WebBrowserDocumentCompletedEventArgs e)
        {
            var webB = (WebBrowser)sender;
            HtmlElement body = webB.Document.Body;

            var title = webB.Document.Title;
            var inputs = body.GetElementsByTagName("input");

            if (title == "Sign in to MYOB - MYOB")
            {
                HtmlElement usr = inputs.GetElementsByName("UserName")[0];
                usr.SetAttribute("value", _login);

                HtmlElement pwd = inputs.GetElementsByName("Password")[0];
                pwd.SetAttribute("value", _password);

                //Get all buttons from body
                var buttons = body.GetElementsByTagName("button");
                foreach (HtmlElement button in buttons)
                {
                    if (button.InnerText.Equals("Sign in"))
                        button.InvokeMember("Click");
                }
            }

            if (title == "Improve your security - MYOB")
            {
                //HtmlElement cancel = inputs.GetElementsByName("__RequestVerificationToken")[0];
                //cancel.SetAttribute("value", "cancel");
                var buttons = body.GetElementsByTagName("button");
                foreach (HtmlElement button in buttons)
                {
                    if (button.InnerText.Equals("Remind me later "))
                        button.InvokeMember("Click");
                }
            }

            if (title == "When would you like to be reminded to turn on 2FA? - MYOB")
            {
                var buttons = body.GetElementsByTagName("button");
                buttons[0].InvokeMember("Click");     //next day
                                                      // buttons[1].InvokeMember("Click");   //next week
            }
           
        }


        /// <summary>
        /// Handler that is called when HTML title is changed in browser (i.e. content is reloaded)
        /// Once user has signed in to OAth page and authorised this app the OAuth code is returned in the HTML content 
        /// </summary>
        /// <param name="sender">The web browser control</param>
        /// <param name="e">The event</param>
        /// <remarks>This assumes redirect URL is http://desktop</remarks>
        private void WebBrowser_DocumentTitleChanged(object sender, EventArgs e)
        {
            var webB = (WebBrowser)sender;

            //Check if OAuth code is returned
            if (webB.DocumentText.Contains("code="))
            {
                _code = ExtractSubstring(webB.DocumentText, "code=", "<");
                GetTokenComplete?.Invoke(this, new EventArgs());     
            }
        }
        #endregion

        /// <summary>
        /// Function to retrieve content from a string based on begining and ending pattern
        /// </summary>
        /// <param name="input">input string</param>
        /// <param name="startsWith">start pattern</param>
        /// <param name="endsWith">end pattern</param>
        /// <returns></returns>
        /// <remarks></remarks>
        private string ExtractSubstring(string input, string startsWith, string endsWith)
        {
            Match match = Regex.Match(input, startsWith + "(.*)" + endsWith);
            string code = match.Groups[1].Value;
            return code;
        }



        public async Task<bool> AccessAsync(string url, string login, string password)
        {
            _currentUrl = url;
            _login = login;
            _password = password;

            // cancel in 30s or when the main token is signalled
            var navigationCts = CancellationTokenSource.CreateLinkedTokenSource(_cancelToken);
            navigationCts.CancelAfter((int)TimeSpan.FromSeconds(30).TotalMilliseconds);
            var navigationToken = navigationCts.Token;

            //_currentPage = await _apartment.Run(() => _browser.NavigateAsync(_currentUrl, navigationToken), navigationToken);

            _currentPage = await _apartment.Run(async () =>
            {
                WebBrowserDocumentCompletedEventHandler handler = null;
                var navigateTcs = new TaskCompletionSource<bool>();
                handler = (s, e) =>
                    navigateTcs.TrySetResult(true);
                _browser.DocumentCompleted += handler;
                try
                {
                    using (_cancelToken.Register(() => navigateTcs.TrySetCanceled()))
                    {
                        _browser.Navigate(url);
                        await navigateTcs.Task;

                        // Authorizartion

                        HtmlElement body = _browser.Document.Body;

                        var title = _browser.Document.Title;
                        var inputs = body.GetElementsByTagName("input");
                        HtmlElementCollection forms = body.GetElementsByTagName("form");
                        var form = forms[0];
                       

                        var inputs2 = form.GetElementsByTagName("input type=\"password\" ");

                        HtmlElementCollection col = body.GetElementsByTagName("div");
                        foreach (HtmlElement element in col)
                        {
                            string cls = element.GetAttribute("className");
                            if (String.IsNullOrEmpty(cls) || !cls.Equals("Xb9hP"))
                                continue;

                            HtmlElementCollection childDivs = element.Children.GetElementsByName("password");
                            foreach (HtmlElement childElement in childDivs)
                            {
                                childElement.SetAttribute("value", _password);
                            }
                        }

                        //Get all buttons from body
                        var buttons = body.GetElementsByTagName("button");
                        foreach (HtmlElement button in buttons)
                        {
                            if (button.InnerText.Equals("Next"))
                                button.InvokeMember("Click");
                        }


                        //if (title == "Sign in - Google Accounts")
                        //{
                        //    //HtmlElement usr = inputs.GetElementsByName("UserName")[0];
                        //    //usr.SetAttribute("value", _login);

                        //    HtmlElement pwd = inputs?.GetElementsByName("password")[0];
                        //    pwd.SetAttribute("value", _password);

                        //    //Get all buttons from body
                        //    var buttons = body.GetElementsByTagName("button");
                        //    foreach (HtmlElement button in buttons)
                        //    {
                        //        if (button.InnerText.Equals("Next"))
                        //            button.InvokeMember("Click");
                        //    }
                        //}

                        return _browser.Document.Body.OuterHtml;
                    }
                }
                finally
                {
                    _browser.DocumentCompleted -= handler;
                }
            },
_cancelToken);




            //if (title == "Improve your security - MYOB")
            //{
            //    //HtmlElement cancel = inputs.GetElementsByName("__RequestVerificationToken")[0];
            //    //cancel.SetAttribute("value", "cancel");
            //    var buttons = body.GetElementsByTagName("button");
            //    foreach (HtmlElement button in buttons)
            //    {
            //        if (button.InnerText.Equals("Remind me later "))
            //            button.InvokeMember("Click");
            //    }
            //}

            return true;
        }


        public async Task NextPageAsync(string url)
        {
            _currentUrl = url;

           // _currentPage = await _apartment.Run(() => _browser.NavigateAsync(_currentUrl, navigationToken), navigationToken);
            _currentPage = await _apartment.Run(async () =>
            {
                WebBrowserDocumentCompletedEventHandler handler = null;
                var navigateTcs = new TaskCompletionSource<bool>();
                handler = (s, e) =>
                    navigateTcs.TrySetResult(true);
                _browser.DocumentCompleted += handler;
                try
                {
                    using (_cancelToken.Register(() => navigateTcs.TrySetCanceled()))
                    {
                        _browser.Navigate(url);
                        await navigateTcs.Task;
                        return _browser.Document.Body.OuterHtml;
                    }
                }
                finally
                {
                    _browser.DocumentCompleted -= handler;
                }
            },
            _cancelToken);
        }

        public void Dispose()
        {
            // destroy the WebBrowser
            _apartment.Run(
                () => _browser.Dispose(),
                CancellationToken.None).Wait();

            // shut down the appartment
            _apartment.Dispose();
        }

        public Task<int> AddMembersToGroupAsync(IList<string> users, string group)
        {
            throw new NotImplementedException();
        }

        public Task CloseCurrentSession()
        {
            throw new NotImplementedException();
        }

        public Task AuthorizationAsync(IConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        Task<bool> ISerferService.AddMembersToGroupAsync(IList<string> users, string groupId)
        {
            throw new NotImplementedException();
        }

        ~SerferService() { Dispose(); }




        //public static void authtori()
        //{
        //    HttpRequest req = new HttpRequest();
        //    req.AllowAutoRedirect = true;
        //    req.IgnoreProtocolErrors = true;
        //    req.MaximumAutomaticRedirections = 100;
        //    req.Proxy = ProxyClient.Parse(ProxyType.Http, "127.0.0.1:8888");
        //    req.Cookies = new CookieDictionary();
        //    req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:51.0) Gecko/20100101 Firefox/51.0";
        //    string result = null;
        //    req["Upgrade-Insecure-Requests"] = "1";
        //    req["Keep-Alive"] = "300";
        //    req["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
        //    req["Accept-Language"] = "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3";

        //    while (true)
        //    {
        //        try
        //        {
        //            result = req.Get("https://accounts.google.com/ServiceLogin?continue=https%3A%2F%2Fwww.youtube.com%2Fsignin%3Fnext%3Dhttps%253A%252F%252Fm.youtube.com%252F%253Frdm%253D2jh9jl5hc%26app%3Dm%26feature%3Dblazerbootstrap%26action_handle_signin%3Dtrue%26hl%3Dru%26noapp%3D1&hl=ru&passive=true&service=youtube&uilel=3").ToString();
        //            break;
        //        }
        //        catch (Exception) { }

        //    }
        //    string GALX = Utils.pars(result, @"name=""GALX"" value=""(.*?)""");
        //    string gfx = Utils.pars(result, @"name=""gxf"" value=""(.*?)""");

        //    while (true)
        //    {
        //        try
        //        {
        //            req.AddParam("Email", "ТВОЙ ЕМАИЛ");
        //            req.AddParam("requestlocation", "https://accounts.google.com/ServiceLogin?continue=https%3A%2F%2Fwww.youtube.com%2Fsignin%3Fnext%3Dhttps%253A%252F%252Fm.youtube.com%252F%253Frdm%253D2jh9jl5hc%26app%3Dm%26feature%3Dblazerbootstrap%26action_handle_signin%3Dtrue%26hl%3Dru%26noapp%3D1&hl=ru&passive=true&service=youtube&uilel=3#identifier");
        //            req.AddParam("Page", "PasswordSeparationSignIn");
        //            req.AddParam("GALX", GALX);
        //            req.AddParam("gxf", gfx);
        //            req.AddParam("continue", "https://www.youtube.com/signin?next=https%3A%2F%2Fm.youtube.com%2F%3Frdm%3D2jh9jl5hc&app=m&feature=blazerbootstrap&action_handle_signin=true&hl=ru&noapp=1");
        //            req.AddParam("service", "youtube");
        //            req.AddParam("hl", "ru");
        //            req.AddParam("_utf8", "");
        //            req.AddParam("pstMsg", "1");
        //            //req.AddParam("checkConnection", "youtube:656:1");
        //            req.AddParam("checkedDomains", "youtube");
        //            req.AddParam("PersistentCookie", "yes");
        //            //req.AddParam("", "");

        //            result = req.Post("https://accounts.google.com/_/signin/v1/lookup").ToString();
        //            break;
        //        }
        //        catch (Exception) { }
        //    }

        //    string prof_state = Utils.pars(result, @"""encoded_profile_information"":""(.*?)""");
        //    string session_state = Utils.pars(result, @"""session_state"":""(.*?)""");


        //    while (true)
        //    {
        //        try
        //        {
        //            // req.AddHeader("Referer", "https://accounts.google.com/ServiceLogin?passive=1209600&continue=https://accounts.google.com/o/oauth2/auth?state%3DoFFb6OeFH1OatIt13xSqusB82VA67Uan%26redirect_uri%3Dhttp://smmok-yt.ru/complete/google-oauth2/%26response_type%3Dcode%26client_id%3D881376626299-rmdnjpro3lgd5n17c5cvupc9639t2702.apps.googleusercontent.com%26scope%3Dhttps://www.googleapis.com/auth/userinfo.email%2Bhttps://www.googleapis.com/auth/userinfo.profile%2Bhttps://www.googleapis.com/auth/youtube.readonly%2Bhttps://www.googleapis.com/auth/youtubepartner%2Bhttps://www.googleapis.com/auth/youtube.force-ssl%2Bhttps://www.googleapis.com/auth/youtube%2Bhttps://www.googleapis.com/auth/plus.login%2Bhttps://www.googleapis.com/auth/plus.me%26access_type%3Doffline%26from_login%3D1%26as%3D-d8d28d69f4e1395&ltmpl=nosignup&oauth=1&sarp=1&scc=1");
        //            // req.AddHeader("Upgrade-Insecure-Requests", "1");
        //            req.AddParam("Page", "PasswordSeparationSignIn");//
        //            req.AddParam("GALX", GALX);//
        //            req.AddParam("gxf", gfx);//
        //            req.AddParam("continue", "https://www.youtube.com/signin?next=https%3A%2F%2Fm.youtube.com%2F%3Frdm%3D2jh9jl5hc&app=m&feature=blazerbootstrap&action_handle_signin=true&hl=ru&noapp=1");
        //            req.AddParam("service", "youtube");
        //            req.AddParam("hl", "ru");
        //            req.AddParam("ProfileInformation", prof_state);//
        //            req.AddParam("SessionState", session_state);//
        //            req.AddParam("_utf8", "");//
        //            req.AddParam("pstMsg", "1");
        //            //req.AddParam("checkConnection", "youtube:656:1");
        //            req.AddParam("checkedDomains", "youtube");
        //            req.AddParam("identifiertoken", "");
        //            req.AddParam("identifiertoken_audio", "");
        //            req.AddParam("identifier-captcha-input", "");
        //            req.AddParam("Email", "ТВОЙ ЕМАИЛ");
        //            req.AddParam("Passwd", "ТВОЙ ПАРОЛЬ");
        //            req.AddParam("PersistentCookie", "yes");
        //            result = req.Post("https://accounts.google.com/signin/challenge/sl/password").ToString();
        //            break;
        //        }
        //        catch (Exception e)
        //        {
        //            MessageBox.Show(e.ToString());
        //        }
        //    }
        //}
    }
}
