using System;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;


namespace Myob.Service.Helpers
{
    class MyobAuthWebBrowser
    {
        string _email;
        string _password;
        string _code;

       
        #region Public Propertyes

        public event EventHandler GetTokenComplete;

        public string Code {
            get { return _code; }
            set { _code = value; }
        }
        #endregion



        public MyobAuthWebBrowser(string email, string password)
        {
            _email = email;
            _password = password;
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


                    //  await webBrowser.NavigateAsync(url, navigationToken);
                    //while (!_completed)
                    //{
                    //    Application.DoEvents();
                    //    Thread.Sleep(100);
                    //}

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
                usr.SetAttribute("value", _email);

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


    }
}
