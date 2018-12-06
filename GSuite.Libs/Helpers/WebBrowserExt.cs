// Coded by Andrew Nosenko (https://plus.google.com/+AndrewNosenko) 
// Licensed under ISC license (https://opensource.org/licenses/ISC)

using Microsoft.Win32;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GSuite.Libs.Helpers
{
    /// <summary>
    /// WebBrowserExt - WebBrowser extensions
    /// by Noseratio - http://stackoverflow.com/a/22262976/1768303
    /// </summary>
    public static class WebBrowserExt
    {
        const int POLL_DELAY = 500;

        // navigate and download 
        public static async Task<string> NavigateAsync(this WebBrowser webBrowser, string url, CancellationToken token)
        {
            // navigate and await DocumentCompleted
            var tcs = new TaskCompletionSource<bool>();
            WebBrowserDocumentCompletedEventHandler handler = (s, arg) =>
            {
                try
                {
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            };
             

            using (token.Register(
                () => { webBrowser.Stop(); tcs.TrySetCanceled(); },
                useSynchronizationContext: true))
            {
                webBrowser.DocumentCompleted += handler;
                try
                {
                    webBrowser.Navigate(url);
                    await tcs.Task; // wait for DocumentCompleted
                }
                finally
                {
                    webBrowser.DocumentCompleted -= handler;
                }
            }

            // get the root element
            var documentElement = webBrowser.Document.GetElementsByTagName("html")[0];

            // poll the current HTML for changes asynchronosly
            var html = documentElement.OuterHtml;
            while (true)
            {
                // wait asynchronously, this will throw if cancellation requested
                await Task.Delay(POLL_DELAY, token);

                // continue polling if the WebBrowser is still busy
                if (webBrowser.IsBusy)
                    continue;

                var htmlNow = documentElement.OuterHtml;
                if (html == htmlNow)
                    break; // no changes detected, end the poll loop

                html = htmlNow;
            }

            // consider the page fully rendered 
            token.ThrowIfCancellationRequested();
            return html;
        }

        // enable HTML5 (assuming we're running IE10+)
        // more info: http://stackoverflow.com/a/18333982/1768303
        public static void SetFeatureBrowserEmulation()
        {
            if (System.ComponentModel.LicenseManager.UsageMode != System.ComponentModel.LicenseUsageMode.Runtime)
                return;
            var appName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION",
                appName, 10000, RegistryValueKind.DWord);
        }
    }
}
