using System;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;


namespace Myob.Service.Helpers
{
    class MyobAuthForm: WebForm
    {
        bool _autoAuth = true;
        string _email;
        string _password;

#region Public Propertyes

        public event EventHandler GetTokenComplete;
        public AutoResetEvent WaitHandler = new AutoResetEvent(false);

        public bool AutoAuth {
            get { return _autoAuth; }
            set {
                _autoAuth = value;
                if (_autoAuth)
                {
                    // Add a handler for the web browser to auto comlete registration
                    webBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;
                    // Add a handler for the web browser to capture content change 
                    webBrowser.DocumentTitleChanged += WebBrowser_DocumentTitleChanged;
                }
                else
                {
                    // Add a handler for the web browser to auto comlete registration
                    webBrowser.DocumentCompleted -= WebBrowser_DocumentCompleted;
                    // Add a handler for the web browser to capture content change 
                    webBrowser.DocumentTitleChanged -= WebBrowser_DocumentTitleChanged;
                }

            }
        }

        public string DocumentText
        {
            get {return webBrowser.DocumentText; }
        }
#endregion

        public MyobAuthForm(string email, string password)
        {
            _email = email;
            _password = password;
        }

        public void Navigate(string url)
        {
            webBrowser.Navigate(url);
        }

        public async Task GetCode(string url, TimeSpan ts)
        {
            TaskCompletionSource<EventArgs> tcs = new TaskCompletionSource<EventArgs>();
            GetTokenComplete += (o, args) => tcs.TrySetResult(args);

            // Add a handler for the web browser to auto comlete registration
            webBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;
            // Add a handler for the web browser to capture content change 
            webBrowser.DocumentTitleChanged += WebBrowser_DocumentTitleChanged;
            webBrowser.Navigate(url);

            

              //  MessageSent += GetTokenComplete;
                var timeoutTask = Task.Delay(ts);
                var winner = await Task.WhenAny(tcs.Task, timeoutTask);
                if (winner == timeoutTask)
                    throw new TimeoutException("Authentification timeout");
             //   return await tcs.Task;


        }

#region Events Methods
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
            var frm = (Form)webB.Parent;

            //Check if OAuth code is returned
            if (webB.DocumentText.Contains("code="))
            {
                // frm.Close();
                  GetTokenComplete?.Invoke(this,new EventArgs());
               // WaitHandler?.Set();
            }
        }
#endregion

    }
    }
