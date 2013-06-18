using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace VkApiNet
{
    public partial class AuthForm : Form
    {
        private string m_accessToken;
        private string m_expiresIn;
        private int m_userId;

        public AuthForm(int appId)
        {
            InitializeComponent();

            browserPlugin.Url =
                new Uri(String.Format(
                    @"https://oauth.vk.com/authorize?client_id={0}&scope=photos,groups,messages,stats&redirect_uri=http://oauth.vk.com/blank.html&display=popup&response_type=token", appId));
            browserPlugin.Navigated += new WebBrowserNavigatedEventHandler(browserPlugin_Navigated);
        }

        void browserPlugin_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
#if DEBUG
            m_accessToken = "test_debug_token";
            m_expiresIn = "86000";
            m_userId = 1;
            DialogResult = DialogResult.OK;
            Close();
#else   
            if (String.Compare(e.Url.AbsolutePath, @"/blank.html", StringComparison.OrdinalIgnoreCase) == 0)
            {
                var parameters = e.Url.Fragment.Replace("#", "").Split(new[] {'&'}, StringSplitOptions.RemoveEmptyEntries);
                foreach (var parameter in parameters)
                {
                    var parts = parameter.Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2) continue;

                    switch (parts[0])
                    {
                        case "access_token":
                            m_accessToken = parts[1];
                            break;
                        case "expires_in":
                            m_expiresIn = parts[1];
                            break;
                        case "user_id":
                            m_userId = Int32.Parse(parts[1]);
                            break;
                        default:
                            continue;
                    }
                }
                DialogResult = DialogResult.OK;
                Close();
            }
#endif
        }

        public string GetAccessToken()
        {
            return m_accessToken;
        }
        public string GetExiprationValue()
        {
            return m_expiresIn;
        }
        public int GetTokenUserId()
        {
            return m_userId;
        }
    }
}
