using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.RequestParams;
using System.Diagnostics;
using System.Text;
using System.Configuration;

namespace VkSearchER
{

    public partial class Form1 : Form
    {
        public string AToken = ConfigurationManager.AppSettings["access_token"];
        public VkApi api = new VkApi();
        public long UId = 1;
        public uint CountSercher = 20;
        public ushort BirthDaySet = 01;
        public ushort BirthMothSet = 01;
        public ushort AgeFromSet = 18;
        public ushort AgeToSet = 0;
        public string StrSiteId = "";

        public void Auths()
        {
            try
            {
                if (AToken != null)
                {
                    api.Authorize(new ApiAuthParams
                    {
                        AccessToken = AToken,
                        Settings = Settings.All
                    });
                    GetInfo(UId);
                }
                else
                {
                    MessageBox.Show("Ошибка Авторизации: Не найден ключ 'access_token' проверьте конфиг-файл 'VkSearchER.exe.config' !");
                    Process.GetCurrentProcess().Kill();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка Авторизации: " + ex.Message);
                Process.GetCurrentProcess().Kill();
            }
        }
        public Form1()
        {
            InitializeComponent();
        }
        
        public void GetInfo(long uids)
        {
            var ppf = ProfileFields.LastSeen | ProfileFields.CanWritePrivateMessage | ProfileFields.BirthDate | ProfileFields.Online | ProfileFields.City;
            var p = api.Users.Get(new long[] { uids }, ppf).FirstOrDefault();
            if (p == null)
            {
                FirstName.Text = "Нет данных";
                return;
            }

            var onlineprofile = (bool)p.Online ? "Online" : "Offline";
            var cuty = p.City?.Title ?? "Скрыт";
            var ls = p.CanWritePrivateMessage ? "Сообщения: Открыты" : "Сообщения: Закрыты";
            var lastSeen = p.LastSeen == null ? "Был в сети: Давно" : $"Был в сети: {TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(p.LastSeen.Time.ToString()), TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time"))}";
            var birthDate = DateTime.Parse(p.BirthDate);
            var age = (int)((DateTime.Today - birthDate).TotalDays / 365);
            labellastseen.Text = lastSeen;
            label9.Text = $"Возраст: {p.BirthDate} ({age})";
            label6.Text = ls;
            FirstName.Text = $"{p.FirstName} {p.LastName} ({onlineprofile}) Г.{cuty}";
        }

        public void ReadEBox()
        {
            BirthDaySet = Convert.ToUInt16(BirthDayTextBox.Text);
            BirthMothSet = Convert.ToUInt16(BirthMothTextBox.Text);
            CountSercher = Convert.ToUInt32(CountSerchTextBox.Text);
            AgeFromSet = Convert.ToUInt16(AgeFromEditB.Text);
            AgeToSet = Convert.ToUInt16(AgeToEditB.Text);
        }
        public void SearchFriends(bool checkCanWritePrivateMessage)
        {
            ReadEBox();
            Random rans = new Random();
            uint uintValue = (uint)rans.Next(0, 100);
            VkNet.Utils.VkCollection<User> PeopleSearch = api.Users.Search(new UserSearchParams()
             {
                 Fields = ProfileFields.CanWritePrivateMessage,
                 Count = CountSercher,
                 Offset = uintValue,
                 AgeFrom = AgeFromSet,
                 AgeTo = AgeToSet,
                 BirthDay = BirthDaySet,
                 BirthMonth = BirthMothSet,
                 Online = true
             });
             
            while (!PeopleSearch.Any())
            {
                PeopleSearch = api.Users.Search(new UserSearchParams()
                {
                    Fields = ProfileFields.CanWritePrivateMessage,
                    Count = CountSercher,
                    Offset = uintValue,
                    AgeFrom = AgeFromSet,
                    AgeTo = AgeToSet,
                    BirthDay = BirthDaySet,
                    BirthMonth = BirthMothSet,
                    Online = true
                });
            }
            foreach (var resultsearch in PeopleSearch)
            {
                 if (!checkCanWritePrivateMessage || resultsearch.CanWritePrivateMessage)
                 {
                     listBox2.Items.Add(resultsearch.Id.ToString());
                 }
                button2.Text = $"Поиск ({listBox2.Items.Count.ToString()})";
            }
        }
       
        private void button2_Click(object sender, EventArgs e)
        {
            ReadEBox();
            listBox2.Items.Clear();
            if (CountSercher > 1000)
            {
                MessageBox.Show("Не больше 1к, ограничение VkNET");
            }
            else
            {
                AgeFromSet = (AgeFromSet < 18) ? (ushort)18 : AgeFromSet;
                AgeToSet = (AgeToSet > 40) ? (ushort)0 : AgeToSet;
                SearchFriends(checkBox2.Checked);
            }

        }
        public void SetDate()
        {
            DateTime now = DateTime.Now;
            int day = now.Day;
            int month = now.Month;
            BirthDayTextBox.Text = day.ToString();
            BirthMothTextBox.Text = month.ToString();

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            SetDate();
            Auths();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            SearchFriends(checkBox2.Checked);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string path = "Result";
            string filedataname = $"{DateTime.Now.ToShortDateString()}({listBox2.Items.Count})";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if (listBox2.Items.Count > 0)
            {
                foreach (string item in listBox2.Items)
                {
                    using (StreamWriter writer = new StreamWriter($"{path}/{filedataname}.txt", true))
                    {
                        writer.WriteLineAsync(StrSiteId + item.ToString());
                    }
                }
            }
           
        }

        private void listBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                if (listBox2.SelectedItem != null)
                {
                    Clipboard.SetText($"vk.com/id{listBox2.SelectedItem.ToString()}");
                }
            }
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem != null)
            {
                GetInfo(long.Parse(listBox2.SelectedItem.ToString()));
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (listBox2.Items.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in listBox2.Items)
                {
                    sb.AppendLine(StrSiteId + item.ToString());
                }
                Clipboard.SetText(sb.ToString());
            }
        
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/yatolya/VKSearchER");
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            StrSiteId = comboBox1.Text;
        }

        private void comboBox1_TextUpdate(object sender, EventArgs e)
        {
            StrSiteId = comboBox1.Text;
        }
    }
}
