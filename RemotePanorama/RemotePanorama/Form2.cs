using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemotePanorama
{
    public partial class Client : Form
    {
        public string LastPath2 { get; set; }
        public RemoteFileDialog dlg;
        public List<string> AllNodes = new List<string>();
        public string TreeState;
        public string url;
        public string folder;
        public Client()
        {
            InitializeComponent();
            
        }


        private void viewFolders_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.userName = username.Text;
            Properties.Settings.Default.password = password.Text;
            Properties.Settings.Default.server = serverName.Text;
            Properties.Settings.Default.Save();
            if (string.IsNullOrEmpty(serverName.Text))
            {
                MessageBox.Show("You must enter the name of a server!");
            }
            else
            {
                if (Properties.Settings.Default.persistString != null)
                {
                    TreeState = Properties.Settings.Default.persistString;
                    Debug.WriteLine(Properties.Settings.Default.persistString);
                }
               
                using (RemoteFileDialog dlg = new RemoteFileDialog("", "")
                {
                    _user = username.Text,
                    _pass = password.Text,
                    _server = serverName.Text,
                    //Optional to pass in name of Open button
                    //_OKButtonText = "Upload"
                })

                {
                    
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {


                    }
                }
                folderLabel.Text = folder;
                urlLabel.Text = url;
                Properties.Settings.Default.persistString = TreeState;
                Properties.Settings.Default.nodeNames = AllNodes;
                Properties.Settings.Default.Save();
            }
        }

        private void Client_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.userName != string.Empty)
            {
                serverName.Text = Properties.Settings.Default.server;
                username.Text = Properties.Settings.Default.userName;
                password.Text = Properties.Settings.Default.password;
            }
            
        }




        /*
         * private void openMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckSaveDocument())
                return;
            using (OpenFileDialog dlg = new OpenFileDialog
            {
                InitialDirectory = Settings.Default.ActiveDirectory,
                CheckPathExists = true,
                SupportMultiDottedExtensions = true,
                DefaultExt = SrmDocument.EXT,
                Filter = TextUtil.FileDialogFiltersAll(SrmDocument.FILTER_DOC_AND_SKY_ZIP, SrmDocumentSharing.FILTER_SHARING, SkypFile.FILTER_SKYP)
            })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    Settings.Default.ActiveDirectory = Path.GetDirectoryName(dlg.FileName);

                    if (dlg.FileName.EndsWith(SrmDocumentSharing.EXT))
                    {
                        OpenSharedFile(dlg.FileName);
                    }
                    else if (dlg.FileName.EndsWith(SkypFile.EXT))
                    {
                        OpenSkypFile(dlg.FileName, this);
                    }
                    else
                    {
                        OpenFile(dlg.FileName);
                    }
                }
            }
        }
         */


    }

    public class DataSaver
    {
        public string lastPath;


        public void SetPath(string newPath)
        {
            lastPath = newPath;
            Debug.WriteLine(lastPath);
        }
    }
}
