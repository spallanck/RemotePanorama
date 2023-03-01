using System;
using RemotePanorama;


namespace TestHandler
{
    public partial class Form1 : Form
    {
        private RemoteFileDialog dlg;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.user != string.Empty)
            {
                userText.Text = Properties.Settings.Default.user;
                passText.Text = Properties.Settings.Default.pass;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

            dlg = new RemoteFileDialog(userText.Text, passText.Text);
            dlg._user = userText.Text;
            dlg._pass = passText.Text;
            dlg._server = "https://panoramaweb.org";
            Properties.Settings.Default.user = userText.Text;
            Properties.Settings.Default.pass =passText.Text;
            Properties.Settings.Default.Save();

           
            dlg.ShowDialog();
            string file = dlg._fileURL;
            string folder = dlg._folder;
            filePath.Text = file;
            folderPath.Text = folder;
            
        }
    }
}