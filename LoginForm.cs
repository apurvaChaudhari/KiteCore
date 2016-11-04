using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KiteCore
{
    public partial class LoginForm : Form
    {
        private string requestToken = "";
        private string url = "";

        public string RequestToken
        {
            get
            {
                return requestToken;
            }
        }
        public string Url
        {
            set
            {
                url = value;
            }
        }

        public LoginForm()
        {
            InitializeComponent();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            webBrowser1.AllowNavigation = true;
            webBrowser1.ScriptErrorsSuppressed = true;
            webBrowser1.Navigate(url);
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            string[] list = webBrowser1.Url.ToString().Split("&".ToCharArray());
            foreach (string str in list)
            {
                string[] values = str.Split("=".ToCharArray());
                if (values[0].Equals("request_token"))
                {
                    requestToken = values[1];
                    this.Close();
                }
            }
        }

    }
}
