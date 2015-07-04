using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EMusic.Models;
using EMusic.Views;

namespace EMusic
{
    public enum PageType
    {
        AUTH, PLAY, SETTINGS
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //NativeMethods.SuppressCookiePersistence();
            Loaded += (s, e) => SwitchPage(PageType.AUTH);
        }

        private UIElement _child;

        public void SwitchPage(PageType pt)
        {
            if (_child != null)
                Root.Children.Remove(_child);

            if (pt == PageType.AUTH)
            {
                var browser = new WebBrowser();
                browser.HorizontalAlignment = HorizontalAlignment.Stretch;
                browser.VerticalAlignment = VerticalAlignment.Stretch;

                browser.Navigating += (s, e) =>
                {
                    var uri = e.Uri.OriginalString;
                    if (uri.Contains("access_token"))
                    {
                        VKApi.ExtractAccessToken(uri);
                        SwitchPage(PageType.PLAY);
                    } 
                };

                browser.Source = VKApi.GetAuthUri();

                _child = browser;
            }

            if (pt == PageType.PLAY)
            {
                var playView = new PlayerView();
                _child = playView;
            }

            //if (pt == PageType.SETTINGS)
            //{

            //}

            if (_child != null)
                Root.Children.Add(_child);
        }

        public static partial class NativeMethods
        {
            [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

            private const int INTERNET_OPTION_SUPPRESS_BEHAVIOR = 81;
            private const int INTERNET_SUPPRESS_COOKIE_PERSIST = 3;

            public static void SuppressCookiePersistence()
            {
                var lpBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int)));
                Marshal.StructureToPtr(INTERNET_SUPPRESS_COOKIE_PERSIST, lpBuffer, true);

                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SUPPRESS_BEHAVIOR, lpBuffer, sizeof(int));

                Marshal.FreeCoTaskMem(lpBuffer);
            }
        }
    }
}
