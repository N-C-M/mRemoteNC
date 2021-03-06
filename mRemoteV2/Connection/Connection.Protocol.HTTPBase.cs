using System;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using mRemoteNC.App;
using My;
using Skybound.Gecko;

namespace mRemoteNC
{
    public class HTTPBase : Base
    {
        #region Private Properties

        private Control wBrowser;
        public string httpOrS;
        public int defaultPort;
        private string tabTitle;

        #endregion Private Properties

        #region Public Methods

        public HTTPBase(RenderingEngine RenderingEngine)
        {
            try
            {
                switch (RenderingEngine)
                {
                    case RenderingEngine.Gecko:
                        this.Control = new MiniGeckoBrowser.MiniGeckoBrowser();
                        (this.Control as MiniGeckoBrowser.MiniGeckoBrowser).XULrunnerPath =
                            Settings.Default.XULRunnerPath;
                        break;
                    case RenderingEngine.IE:
                        this.Control = new WebBrowser();
                        break;
                    case RenderingEngine.GeckoFX:
                        Control = new Gecko.GeckoWebBrowser();
                        Gecko.Xpcom.Initialize(Settings.Default.XULRunnerPath);
                        break;
                }

                NewExtended();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg,
                                                    Language.strHttpConnectionFailed + Constants.vbNewLine +
                                                    ex.Message, true);
            }
        }

        public virtual void NewExtended()
        {
        }

        public override bool SetProps()
        {
            base.SetProps();

            try
            {
                Crownwood.Magic.Controls.TabPage objTabPage =
                    this.InterfaceControl.Parent as Crownwood.Magic.Controls.TabPage;
                this.tabTitle = objTabPage.Title;
            }
            catch (Exception)
            {
                this.tabTitle = "";
            }

            try
            {
                this.wBrowser = this.Control;

                switch (InterfaceControl.Info.RenderingEngine)
                {
                    case RenderingEngine.Gecko:
                        {
                            MiniGeckoBrowser.MiniGeckoBrowser objMiniGeckoBrowser =
                                wBrowser as MiniGeckoBrowser.MiniGeckoBrowser;

                            objMiniGeckoBrowser.TitleChanged +=
                                (sender, title) => wBrowser_DocumentTitleChanged(sender, null); //FIXME
                            objMiniGeckoBrowser.LastTabRemoved += wBrowser_LastTabRemoved;
                        }
                        break;
                    case RenderingEngine.IE:
                        {
                            WebBrowser objWebBrowser = wBrowser as WebBrowser;
                            SHDocVw.WebBrowser objAxWebBrowser = (SHDocVw.WebBrowser)objWebBrowser.ActiveXInstance;

                            objWebBrowser.AllowWebBrowserDrop = false;
                            objWebBrowser.ScrollBarsEnabled = true;

                            objWebBrowser.DocumentTitleChanged += wBrowser_DocumentTitleChanged;
                            objAxWebBrowser.NewWindow3 += ObjAxWebBrowserOnNewWindow3; //wBrowser_NewWindow3;
                        }
                        break;
                        case RenderingEngine.GeckoFX:
                        {
                            var objWebBrowser = wBrowser as Gecko.GeckoWebBrowser;
                            objWebBrowser.AllowDrop = false;
                            objWebBrowser.DocumentTitleChanged += wBrowser_DocumentTitleChanged;
                        }
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg,
                                                    Language.strHttpSetPropsFailed + Constants.vbNewLine +
                                                    ex.Message, true);
                return false;
            }
        }

        private void ObjAxWebBrowserOnNewWindow3(ref object ppDisp, ref bool cancel, uint dwFlags,
                                                 string bstrUrlContext, string bstrUrl)
        {
            wBrowser_NewWindow3(ref ppDisp, ref cancel, dwFlags, bstrUrlContext, bstrUrl);
        }

        public override bool Connect()
        {
            try
            {
                string strHost = (string)this.InterfaceControl.Info.Hostname;
                string strAuth = "";

                if (this.InterfaceControl.Info.Username != "" && this.InterfaceControl.Info.Password != "")
                {
                    strAuth =
                        (string)
                        ("Authorization: Basic " +
                         Convert.ToBase64String(
                             System.Text.Encoding.ASCII.GetBytes(this.InterfaceControl.Info.Username + ":" +
                                                                 this.InterfaceControl.Info.Password)) +
                         Constants.vbNewLine);
                }

                if (this.InterfaceControl.Info.Port != defaultPort)
                {
                    if (strHost.EndsWith("/"))
                    {
                        strHost = strHost.Substring(0, strHost.Length - 1);
                    }

                    if (strHost.Contains(httpOrS + "://") == false)
                    {
                        strHost = httpOrS + "://" + strHost;
                    }

                    switch (InterfaceControl.Info.RenderingEngine)
                    {
                        case RenderingEngine.Gecko:
                            (wBrowser as MiniGeckoBrowser.MiniGeckoBrowser).Navigate(strHost + ":" +
                                                                                     this.InterfaceControl.Info.Port);
                            break;
                          case  RenderingEngine.IE:
                            (wBrowser as WebBrowser).Navigate(strHost + ":" + this.InterfaceControl.Info.Port, null,
                                                              null, strAuth);
                            break;
                        case RenderingEngine.GeckoFX:
                            (wBrowser as Gecko.GeckoWebBrowser).Navigate(strHost + ":" +
                                                                                     this.InterfaceControl.Info.Port);
                            break;
                    }
                }
                else
                {
                    if (strHost.Contains(httpOrS + "://") == false)
                    {
                        strHost = httpOrS + "://" + strHost;
                    }

                    switch (InterfaceControl.Info.RenderingEngine)
                    {
                        case RenderingEngine.Gecko:
                            (wBrowser as MiniGeckoBrowser.MiniGeckoBrowser).Navigate(strHost);
                            break;
                        case RenderingEngine.IE:
                            (wBrowser as WebBrowser).Navigate(strHost, null, null, strAuth);
                            break;
                       case RenderingEngine.GeckoFX:
                            var geckoWebBrowser = wBrowser as Gecko.GeckoWebBrowser;
                            if (geckoWebBrowser != null)
                                geckoWebBrowser.Navigate(strHost);
                            break;
                    }
                }

                base.Connect();
                return true;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg,
                                                    Language.strHttpConnectFailed + Constants.vbNewLine + ex.Message,
                                                    true);
                return false;
            }
        }

        #endregion Public Methods

        #region Private Methods

        #endregion Private Methods

        #region Events

        private void gex_CreateWindow(object sender, Skybound.Gecko.GeckoCreateWindowEventArgs e)
        {
            e.WebBrowser = (GeckoWebBrowser)this.wBrowser;
        }

        private void wBrowser_NewWindow3(ref object ppDisp, ref bool Cancel, long dwFlags, string bstrUrlContext,
                                         string bstrUrl)
        {
            Cancel = (dwFlags & 8L) <= 0L;
        }

        private void wBrowser_LastTabRemoved(object sender)
        {
            this.Close();
        }

        private void wBrowser_DocumentTitleChanged(System.Object sender, System.EventArgs e)
        {
            try
            {
                Crownwood.Magic.Controls.TabPage tabP;
                tabP = InterfaceControl.Parent as Crownwood.Magic.Controls.TabPage;

                if (tabP != null)
                {
                    string shortTitle = "";

                    switch (this.InterfaceControl.Info.RenderingEngine)
                    {
                        case RenderingEngine.Gecko:
                            if ((wBrowser as MiniGeckoBrowser.MiniGeckoBrowser).Title.Length >= 30)
                            {
                                shortTitle = (wBrowser as MiniGeckoBrowser.MiniGeckoBrowser).Title.Substring(0, 29) +
                                             " ...";
                            }
                            else
                            {
                                shortTitle = (wBrowser as MiniGeckoBrowser.MiniGeckoBrowser).Title;
                            }
                            break;
                        case RenderingEngine.IE:
                            if ((wBrowser as WebBrowser).DocumentTitle.Length >= 30)
                            {
                                shortTitle = (wBrowser as WebBrowser).DocumentTitle.Substring(0, 29) + " ...";
                            }
                            else
                            {
                                shortTitle = (wBrowser as WebBrowser).DocumentTitle;
                            }
                            break;
                            case RenderingEngine.GeckoFX:
                            if ((wBrowser as Gecko.GeckoWebBrowser).DocumentTitle.Length >= 30)
                            {
                                shortTitle = (wBrowser as Gecko.GeckoWebBrowser).DocumentTitle.Substring(0, 29) +
                                             " ...";
                            }
                            else
                            {
                                shortTitle = (wBrowser as Gecko.GeckoWebBrowser).DocumentTitle;
                            }
                            break;
                    }

                    if (this.tabTitle != "")
                    {
                        tabP.Title = tabTitle + " - " + shortTitle;
                    }
                    else
                    {
                        tabP.Title = shortTitle;
                    }
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.WarningMsg,
                                                    Language.strHttpDocumentTileChangeFailed + Constants.vbNewLine +
                                                    ex.Message, true);
            }
        }

        #endregion Events

        #region Enums

        public enum RenderingEngine
        {
            [LocalizedAttributes.LocalizedDescriptionAttribute("strHttpInternetExplorer")]
            IE = 1,
            [LocalizedAttributes.LocalizedDescription("strHttpGecko")]
            Gecko = 2,
            [LocalizedAttributes.LocalizedDescription("strHttpGeckoFX")]
            GeckoFX=3
        }

        private enum NWMF
        {
            NWMF_UNLOADING = 0x1,
            NWMF_USERINITED = 0x2,
            NWMF_FIRST = 0x4,
            NWMF_OVERRIDEKEY = 0x8,
            NWMF_SHOWHELP = 0x10,
            NWMF_HTMLDIALOG = 0x20,
            NWMF_FROMDIALOGCHILD = 0x40,
            NWMF_USERREQUESTED = 0x80,
            NWMF_USERALLOWED = 0x100,
            NWMF_FORCEWINDOW = 0x10000,
            NWMF_FORCETAB = 0x20000,
            NWMF_SUGGESTWINDOW = 0x40000,
            NWMF_SUGGESTTAB = 0x80000,
            NWMF_INACTIVETAB = 0x100000
        }

        #endregion Enums
    }
}