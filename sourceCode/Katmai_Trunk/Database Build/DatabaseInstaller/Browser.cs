using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using SHDocVw;


namespace Microsoft.SqlServer.DatabaseInstaller
{
    public class Browser
    {
        private bool _IsOpen = false;
        private SHDocVw.InternetExplorer _IExplorer = null;
        private IWebBrowserApp _WebBrowser = null;

        private void MaybeOpen()
        {
            if (!_IsOpen)
            {
                _IExplorer = new SHDocVw.InternetExplorer();
                _IExplorer.OnQuit += new DWebBrowserEvents2_OnQuitEventHandler(this.OnQuit);
                _WebBrowser = (IWebBrowserApp)_IExplorer;
            }
        }

        public void Navigate(string url)
        {
            Object ignore = null;

            MaybeOpen();

            _WebBrowser.Visible = true;
            _WebBrowser.Navigate(url, ref ignore, ref ignore, ref ignore, ref ignore);
        }

        private void OnQuit()
        {
            _IsOpen = false;
            _IExplorer = null;
            _WebBrowser = null;
        }

    }
}
