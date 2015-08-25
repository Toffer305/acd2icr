using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading;
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
using FirstFloor.ModernUI.Windows.Controls;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;

namespace ACD2ICR.Pages
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl
    {
        CookieContainer cookies = new CookieContainer();
        MySqlConnection dbConn = null;
        MySqlCommand cmd = null;
        private const string NBSP = @"&nbsp;";

        public Home()
        {
            InitializeComponent();
            ACD_Login();
            Thread.Sleep(500);
            ACD_GoBusy();
            Thread.Sleep(500);
            ACD_ActivityMonitor();
        }

        private void ACD_Login()
        {
            HtmlDocument doc = new HtmlDocument();
            Uri cookieuri = new Uri(ACD2ICR.Properties.Settings.Default.ACDLoginUrl);
            string acd_parameters  = string.Format("login={0}&password={1}&Button_DoLogin={2}", "ic4215", "i4215", "Login");
            var request = (HttpWebRequest)HttpWebRequest.Create(cookieuri);
            request.Timeout = 5000;
            request.CookieContainer = cookies;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            byte[] bytes = Encoding.ASCII.GetBytes(acd_parameters);
            request.ContentLength = bytes.Length;
            using (Stream os = request.GetRequestStream())
            {
                os.Write(bytes, 0, bytes.Length);
            }
            WebResponse resp = request.GetResponse();
            using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
            {
                string returnedmessage = sr.ReadToEnd();                
            }
            
        }

        private void ACD_GoBusy()
        {
            Uri cookieuri = new Uri(ACD2ICR.Properties.Settings.Default.ACDGoBusy);
            var request = (HttpWebRequest)HttpWebRequest.Create(cookieuri);
            request.Timeout = 5000;
            request.CookieContainer = cookies;
            request.Method = "GET";

            HttpWebResponse WebResp = (HttpWebResponse)request.GetResponse();
            Stream answer = WebResp.GetResponseStream();
            StreamReader _Answer = new StreamReader(answer);            
        }

        private void ACD_ActivityMonitor()
        {
            HtmlDocument doc = new HtmlDocument();
            Uri cookieuri = new Uri(ACD2ICR.Properties.Settings.Default.ACDActivityMonitorUrl);
            var request = (HttpWebRequest)HttpWebRequest.Create(cookieuri);
            request.Timeout = 5000;
            request.CookieContainer = cookies;
            request.Method = "GET";
            try
            {               
                HttpWebResponse WebResp =(HttpWebResponse)request.GetResponse();
                Stream answer = WebResp.GetResponseStream();
                StreamReader _Answer = new StreamReader(answer);
                string htmlresponse = _Answer.ReadToEnd();                
                doc.LoadHtml(htmlresponse);

                ExtractHTMLtables(doc);               
                                              
            }
            catch (WebException we)
            {
                System.Diagnostics.Debug.WriteLine(we);                
            }            
        }

        private async void ExtractHTMLtables(HtmlDocument doc)
        {
            List<string> t1Cells = new List<string>();
            List<string> t2Cells = new List<string>();
            
            var tablenodes = doc.DocumentNode.SelectNodes("//table[@class='Grid']");
            var t1rownodes = tablenodes[0].SelectNodes("tr[@class='Row'] | tr[@class='AltRow']");
            var t2rownodes = tablenodes[1].SelectNodes("tr[@class='Row'] | tr[@class='AltRow']");

            for (int i = 1; i < t1rownodes.Count; i++)
            {
                System.Diagnostics.Debug.WriteLine(" ");
                var t1cellnodes = t1rownodes[i].SelectNodes("td");
                for (int j = 0; j < t1cellnodes.Count; j++)
                {
                    string dirtystring = t1cellnodes[j].InnerText;
                    string cleanstring = dirtystring.Replace(NBSP, "");
                    System.Diagnostics.Debug.WriteLine(cleanstring);
                }               
            }

            for (int i = 1; i < t2rownodes.Count; i++)
            {
                System.Diagnostics.Debug.WriteLine(" ");
                var t2cellnodes = t2rownodes[i].SelectNodes("td");
                string name = t2cellnodes[1].InnerText.Replace(NBSP, "");
                string phone = t2cellnodes[2].InnerText.Replace(NBSP, "");
                string status = t2cellnodes[4].InnerText.Replace(NBSP, "");
                string lastcall = t2cellnodes[5].InnerText.Replace(NBSP,"");
                string callstoday = t2cellnodes[6].InnerText.Replace(NBSP, "");
                string missedtoday = t2cellnodes[7].InnerText.Replace(NBSP,"");

                // DB Connection
                dbConn = new MySqlConnection("Server=" + ACD2ICR.Properties.Settings.Default.DBServer + ";Database=" + ACD2ICR.Properties.Settings.Default.DBName + ";Uid=" + ACD2ICR.Properties.Settings.Default.DBUser + ";Pwd=" + ACD2ICR.Properties.Settings.Default.DBPass + ";");
                await dbConn.OpenAsync();

                // DB Command Preparation
                cmd = new MySqlCommand(ACD2ICR.Properties.Settings.Default.DBInsertRecord, dbConn);
                cmd.Parameters.AddWithValue("?name", name);
                cmd.Parameters.AddWithValue("?phone", phone);
                cmd.Parameters.AddWithValue("?status", status);
                cmd.Parameters.AddWithValue("?lastcall", lastcall);
                cmd.Parameters.AddWithValue("?callstoday", callstoday);
                cmd.Parameters.AddWithValue("?missedtoday", missedtoday);
                cmd.Prepare();
                // DB Command Execution
                int insertcompletion = await cmd.ExecuteNonQueryAsync();
                dbConn.Close();

            }
        }


    }
}
