using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using pwiz.PanoramaClient.Properties;

namespace pwiz.PanoramaClient
{
    public partial class RemoteFileDialog : Form
    {
        private static string ReplacedInfo = "/query-selectRows.view?schemaName=targetedms&query.queryName=TargetedMSRuns&query.containerFilterName=CurrentAndSubfolders&query.columns=Name%2CReplaced%2CReplacedByRun%2CFolder%2CRowId%2CCreated&query.sort=Created";
        private static string LatestVersion = "/query-selectRows.view?schemaName=targetedms&query.queryName=TargetedMSRuns&query.containerFilterName=CurrentAndSubfolders&query.columns=Name%2CReplaced%2CReplacedByRun%2CFolder%2CRowId%2CCreated&query.sort=-Created";
        private static string CheckIfVersions = "/query-selectRows.view?schemaName=targetedms&query.queryName=TargetedMSRuns&query.containerFilterName=CurrentAndSubfolders&query.columns=Name%2CReplaced%2CReplacedByRun%2CFolder%2CRowId%2CCreated&query.sort=Created&query.Replaced~eq=True";
        private static string QueryFileString = "/query-selectRows.view?schemaName=targetedms&query.queryName=Runs&query.containerFilterName=CurrentAndSubfolders&query.columns=DocumentSize%2CPeptideGroupCount%2CPeptideCount%2CPrecursorCount%2CTransitionCount%2CReplicateCount&query.FileName~eq=";
        private static string PeptideInfoQuery = "/query-selectRows.view?schemaName=targetedms&query.queryName=Runs&query.containerFilterName=CurrentAndSubfolders&query.columns=Container%2CFileName%2CDeleted%2CContainer%2FPath%2CDocumentSize%2CPeptideGroupCount%2CPeptideCount%2CPrecursorCount%2CTransitionCount%2CReplicateCount";
        private static string QueryFolderString = "/query-selectRows.view?schemaName=targetedms&query.queryName=Runs&query.containerFilterName=AllFolders&query.columns=Container%2CFileName%2CDeleted%2CContainer%2FPath";
        private string ContainerString = "https://panoramaweb.org/project/getContainers.view?includeSubfolders=true&moduleProperties=TargetedMS";
        private string FileString = "https://panoramaweb.org/_webdav/Panorama%20Public/?method=json";
        private string InitQuery = "https://panoramaweb.org/Panorama%20Public/query-selectRows.view?schemaName=targetedms&query.queryName=Runs&query.containerFilterName=AllFolders&query.columns=Container%2CFileName%2CDeleted%2CContainer%2FPath";
        public string _server;
        public string _user;
        public string _pass;
        public string _OKButtonText;
        public string _treeState;
        private bool showingSky;
        public TreeNodeCollection _nodesState;
        private static string userCopy;
        private static string passCopy;
        public string _fileURL;
        public string _folder;
        public List<TreeView> tree = new List<TreeView>();
        public TreeViewStateRestorer state;
        private TreeNode lastSelected;
        private bool restoring;
        private Stack<TreeNode> previous = new Stack<TreeNode>();
        private TreeNode priorNode;
        private Stack<TreeNode> next = new Stack<TreeNode>();

        //Using to try and store state of tree
        private List<string> VisibleNodes = new List<string>();
        private List<string> NodeNameTag = new List<string>();
        private List<string> NodePathTag = new List<string>();
        private List<Int32> ExpandedState = new List<Int32>();
    

        public RemoteFileDialog(string user, string pass, string server)
        {
            _server = server;
            _user = user;
            _pass = pass;
            InitializeComponent();
            Uri uriFolder = new Uri(ContainerString);
            var webClient = new WebClientWithCredentials(uriFolder, _user, _pass);
            JToken json = webClient.Get(uriFolder);
            state = new TreeViewStateRestorer(treeView);
            back.Enabled = false;
            forward.Enabled = false;
            try
            {
                if (PanoramaClient.Properties.Settings.Default.state != string.Empty)
                {
                    restoring = true;
                    showingSky = PanoramaClient.Properties.Settings.Default.skyFiles;
                    if (showingSky)
                    {
                        checkBox1.Checked = true;
                    }
                    LoadTree(treeView, "testFile");
                    state.RestoreExpansionAndSelection(PanoramaClient.Properties.Settings.Default.state);
                    AddSelectedFiles(treeView.Nodes);
                    if (lastSelected == null)
                    {
                        up.Enabled = false;
                    }
                    restoring = false;
                }
               
            } catch (Exception e)
            {
                var treeNode = new TreeNode("Panorama Folders");
                treeView.Nodes.Add(treeNode);
                AddFolders(uriFolder, treeNode);
            }
        }

        /// <summary>
        /// Sets a username and password and changes the 'Open' button text if a custom string is passed in
        /// </summary>
        private void RemoteFileDialog_Load(object sender, EventArgs e)
        {
            userCopy = _user;
            passCopy = _pass;
            if (string.IsNullOrEmpty(_OKButtonText))
            {
                open.Text = Resources.RemoteFileDialog_RemoteFileDialog_Load_Open;
            }
            else
            {
                open.Text = _OKButtonText;
            }
        }

        private JToken GetJson(string query)
        {
            Uri queryUri = new Uri(query);
            var webClient = new WebClientWithCredentials(queryUri, userCopy, passCopy);
            JToken json = webClient.Get(queryUri);
            return json;
        }

        public JToken GetInfoForFolders()
        {
            Uri serverUri = new Uri("https://panoramaweb.org/");
            Uri uri = new Uri(ContainerString);

            using (var webClient = new WebClientWithCredentials(serverUri, _user, _pass))
            {
                JToken token = webClient.Get(uri);
                return token;
            }
        }


        /// <summary>
        /// Add all children of given node, and if those children have children, give them a dummy node
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="node"></param>
        private void AddFolders(Uri uri, TreeNode node)
        {
            //if (node.FirstNode != null && node.FirstNode.Text.Equals("DummyNode"))
            if (IsDummyNode(node.FirstNode))
            {
                node.FirstNode.Remove();
            }
            var webClient = new WebClientWithCredentials(uri, _user, _pass);
            JToken json = webClient.Get(uri);
            JEnumerable<JToken> subFolders = json[@"children"].Children();
            foreach (var subFolder in subFolders)
            {
                string folderName = (string)subFolder[@"name"];
                var permissions = CheckFolderPermissions(subFolder);
                if (permissions)
                {
                    TreeNode newNode = new TreeNode(folderName);
                    newNode.Tag = (string)subFolder[@"path"];
                    node.Nodes.Add(newNode);
                    var childrenCount = subFolder[@"children"].Children();
                    if (childrenCount.Count() > 0)
                    {
                        CreateDummyNode(newNode);
                        //newNode.Nodes.Add(new TreeNode("DummyNode"));
                    }
                }
            }
        }

        /// <summary>
        /// Determines if a given folder has read permissions
        /// </summary>
        /// <param name="folderJson"></param>
        /// <returns></returns>
        public static bool CheckFolderPermissions(JToken folderJson)
        {
            if (folderJson != null)
            {
                var userPermissions = folderJson.Value<int?>(@"userPermissions");
                return userPermissions != null && Equals(userPermissions & 1, 1);
            }

            return false;
        }


        /// <summary>
        /// Adds all files in a particular folder 
        /// </summary>
        /// <param name="newUri"></param>
        /// <param name="listView"></param>
        public static void AddChildFiles(Uri newUri, ListView listView)
        {
            var webClient = new WebClientWithCredentials(newUri, userCopy, passCopy);
            JToken json = webClient.Get(newUri);
            if ((int)json[@"fileCount"] != 0)
            {
                JToken files = json[@"files"];
                foreach (dynamic file in files)
                {
                    string[] listItem = new string[2];
                    string fileName = (string)file[@"text"];
                    listItem[0] = fileName;
                    bool isFile = (bool)file[@"leaf"];
                    if (isFile)
                    {
                        bool canRead = (bool)file[@"canRead"];
                        if (!canRead)
                        {
                            continue;
                        }
                        long size = (long)file[@"size"];
                        FileSize sizeObj = new FileSize(size);
                        listItem[1] = sizeObj.ToString();
                        ListViewItem fileNode;
                        if (fileName.Contains(".sky"))
                        {
                            fileNode = new ListViewItem(listItem, 1);

                        } else
                        {
                            fileNode = new ListViewItem(listItem, 0);
                        }
                        fileNode.Tag = (string)file[@"id"];
                        fileNode.Name = (string)file[@"href"];
                        listView.Items.Add(fileNode);
                    }
                }
            }
        }

        private static string[] GetLatestVersion(string path, ListView listView)
        {
            listView.Items.Clear();
            string[] result = new string[3];
            string query = LatestVersion;
            query = "https://panoramaweb.org/" + path + query;
            Uri queryUri = new Uri(query);
            var webClient = new WebClientWithCredentials(queryUri, userCopy, passCopy);
            JToken json = webClient.Get(queryUri);
            JToken rowOne = json["rows"].First();
            JToken rowNum = json["rowCount"];
            int rowCount = (int)rowNum;
            string name = rowOne["Name"].ToString();
            result[2] = rowCount.ToString();
            result[0] = name;
            var fileInfos = AddQueryFile(listView, path, name);
            result[1] = fileInfos[0];
            ListViewItem fileNode = new ListViewItem(result);
            fileNode.ToolTipText = string.Format(Resources.RemoteFileDialog_GetLatestVersion_Proteins___0___Peptides___1___Precursors___2___Transitions___3___Replicates___4_,
                fileInfos[1], fileInfos[2], fileInfos[3], fileInfos[4], fileInfos[5]);
            listView.Items.Add(fileNode);
            return result;
        }

        /// <summary>
        /// Returns true if a file has multiple versions, and false if not
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool HasVersions(string path)
        {
            string query = CheckIfVersions;
            query = "https://panoramaweb.org/" + path + query;
            Uri queryUri = new Uri(query);
            var webClient = new WebClientWithCredentials(queryUri, userCopy, passCopy);
            JToken json = webClient.Get(queryUri);
            JToken rowNum = json["rowCount"];
            string[] result = new string[2];
            int rowCount = (int)rowNum;
            return rowCount != 0;
        }

        /// <summary>
        /// Find number of rows which gives version, and latest version is the only version where replaced is false
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static string[] GetVersionInfo(string path, string fileName)
        {
            string query = ReplacedInfo;
            query = "https://panoramaweb.org/" + path + query;
            Uri queryUri = new Uri(query);
            var webClient = new WebClientWithCredentials(queryUri, userCopy, passCopy);
            JToken json = webClient.Get(queryUri);
            JToken rowNum = json["rowCount"];
            string[] result = new string[2];
            int rowCount = (int)rowNum;
            if (rowCount == 0)
            {
                result[0] = 1.ToString();
                return result;
            }

            int count = 1;
            JToken rows = json["rows"];
            string rowReplaced = null;
            string curRow = null;
            foreach (var row in rows)
            {
                string replaced = (string)row["Replaced"];
                if (replaced.Equals("True"))
                {
                    count++;
                }
                string name = (string)row["Name"];
                if (replaced.Equals("True") && name.Equals(fileName))
                {
                    rowReplaced = (string)row["ReplacedByRun"];
                    
                }
                string rowId = (string)row["RowId"];
                if (!string.IsNullOrEmpty(rowReplaced) && rowId.Equals(rowReplaced))
                {
                    curRow = (string)row["Name"];
                    result[1] = curRow;
                }
            }
            result[0] = count.ToString();
            return result;



        }

        /// <summary>
        /// Given a folder path, try and add only the most recent file located inside the folder to the ListView
        /// </summary>
        /// <param name="list"></param>
        /// <param name="nodePath"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static string[] AddQueryFile(ListView list, string nodePath, string fileName)
        {
            string[] result = new string[6];
            string query = QueryFileString;
            query = "https://panoramaweb.org/" + nodePath + query + fileName;
            Uri queryUri = new Uri(query);
            var webClient = new WebClientWithCredentials(queryUri, userCopy, passCopy);
            JToken json = webClient.Get(queryUri);
            JToken rowOne = json["rows"].First();
            long size  = (long)rowOne[@"DocumentSize"];
            FileSize sizeObj = new FileSize(size);
            result[0] = sizeObj.ToString();
            result[1] = (string)rowOne[@"PeptideGroupCount"];
            result[2] = (string)rowOne[@"PeptideCount"];
            result[3] = (string)rowOne[@"PrecursorCount"];
            result[4] = (string)rowOne[@"TransitionCount"];
            result[5] = (string)rowOne[@"ReplicateCount"];
            return result;
        }

        /// <summary>
        /// Given a folder path, try and add all .sky files inside that folder to this ListView
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="nodePath"></param>
        /// <param name="l"></param>
        /// <param name="options"></param>
        private static void AddQueryFiles(ListView listView, string nodePath, Label l, ComboBox options)
        {
            string query = PeptideInfoQuery;
            query = "https://panoramaweb.org/" + nodePath + query;
            try
            {
                Uri queryUri = new Uri(query);
                var webClient = new WebClientWithCredentials(queryUri, userCopy, passCopy);
                JToken json = webClient.Get(queryUri);
                JToken rows = json["rows"];
                bool versions = HasVersions(nodePath);
                HashSet<string> prevFolders = new HashSet<string>();
                foreach (var row in rows)
                {
                    string fileName = (string)row[@"FileName"];
                    string filePath = (string)row[@"Container/Path"];
                    filePath = filePath + "/";
                    string filePathMod = filePath.Replace(nodePath, "");
                    if (filePath.Equals(nodePath))
                    {
                        string[] listItem = new string[4];
                        string[] numVersions = new string[2];
                        if (versions)
                        {
                            l.Visible = true;
                            options.Visible = true;
                            numVersions = GetVersionInfo(nodePath, fileName);
                        }
                        else
                        {
                            l.Visible = false;
                            options.Visible = false;
                        }
                        listItem[0] = fileName;
                        if ((long)row[@"DocumentSize"] != null)
                        {
                            long size = (long)row[@"DocumentSize"];
                            FileSize sizeObj = new FileSize(size);
                            listItem[1] = sizeObj.ToString();
                        }

                        if (numVersions[0] != null)
                        {
                            listItem[2] = numVersions[0];
                        }
                        else
                        {
                            listItem[2] = "1";
                        }
                        if (numVersions[1] != null)
                        {
                            listItem[3] = numVersions[1].ToString();
                        }
                        ListViewItem fileNode = new ListViewItem(listItem, 1);
                        fileNode.Name = (string)row[@"_labkeyurl_FileName"];
                        var toolTipInfo = string.Format(
                            Resources.RemoteFileDialog_AddQueryFiles_Proteins___0___Peptides___1___Precursors___2___Transitions___3___Replicates___4_,
                            row[@"PeptideGroupCount"], row[@"PeptideCount"], row[@"PrecursorCount"],
                            row[@"TransitionCount"], row[@"ReplicateCount"]);
                        fileNode.ToolTipText = toolTipInfo;
                        listView.Items.Add(fileNode);
                    }
                }
            } catch (Exception e)
            {

            }       
        }

        private static void AddQueryFolders(TreeNode node)
        {
            //if (node.FirstNode != null && node.FirstNode.Text.Equals("DummyNode"))
            if (IsDummyNode(node.FirstNode))
            {
               node.Nodes.Clear();
            }
            if (node.Nodes.Count == 0)
            {
                string query = QueryFolderString;
                string path = (string)node.Tag;
                query = "https://panoramaweb.org/" + node.Name + query;
                Uri queryUri = new Uri(query);
                var webClient = new WebClientWithCredentials(queryUri, userCopy, passCopy);
                JToken json = null;
                try
                {
                    json = webClient.Get(queryUri);
                    JToken rows = json["rows"];
                    HashSet<string> prevFolders = new HashSet<string>();
                    foreach (var row in rows)
                    {
                        string fileName = (string)row[@"FileName"];
                        string filePath = (string)row[@"Container/Path"];
                        string origPath = filePath;
                        filePath = filePath.Replace(path, ""); 
                        string[] Arr = filePath.Split('/');
                        //If we haven't seen this file path yet, try and add the folder it belongs in to the tree
                        if (prevFolders.Add(origPath) && !string.IsNullOrEmpty(Arr[0]))
                        {
                            TreeNode fileNode = new TreeNode(Arr[0]);
                            string newPath = path + Arr[0];
                            fileNode.Tag = newPath + "/";
                            fileNode.Name = origPath;

                            //Check to make sure this folder isn't already in the tree
                            bool inTree = false;
                            foreach (TreeNode curNode in node.Nodes)
                            {
                                string tag = (string)curNode.Tag;
                                if (tag != null && tag.Contains(newPath))
                                {
                                    inTree = true;
                                    if (curNode.Nodes.Count == 0)
                                    {
                                        CreateDummyNode(curNode);
                                        //curNode.Nodes.Add(new TreeNode("DummyNode"));
                                    }
                                }
                            }
                            if (!inTree)
                            {
                                if (!origPath.Equals(newPath))
                                {
                                    CreateDummyNode(fileNode);
                                    //fileNode.Nodes.Add(new TreeNode("DummyNode"));
                                }
                                node.Nodes.Add(fileNode);
                            }  
                        }
                    }
                }
                catch (Exception e)
                {
                    
                }
            }  
        }

        /// <summary>
        /// When a node is clicked on, find all of the files located in the corresponding remote folder and select the node.
        /// In the case of .sky folders, if the node has not been expanded yet, find its subfolders
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView2_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            ClearTreeRecursive(treeView.Nodes);
            var hit = e.Node.TreeView.HitTest(e.Location);
            if (hit.Location != TreeViewHitTestLocations.PlusMinus)
            {
                forward.Enabled = false;
                if (priorNode != null && priorNode != e.Node)
                {
                    previous.Push(priorNode);
                    back.Enabled = true;
                }
                priorNode = e.Node;
                if (e.Node.Parent == null)
                {
                    up.Enabled = false;
                } else
                {
                    up.Enabled = true;
                }
                string path = (string)e.Node.Tag;
                listView.Items.Clear();
                if (!string.IsNullOrEmpty(path))
                {
                    Uri uri;
                    if (!e.Node.IsExpanded)
                    {
                        //add subfolders in this node's treeview here
                        if (checkBox1.Checked)
                        {
                            AddQueryFolders(e.Node);
                        }
                    }
                    if (checkBox1.Checked)
                    {
                        AddQueryFiles(listView, (string)e.Node.Tag, versionLabel, comboBox1);
                    }
                    else
                    {
                        uri = new Uri("https://panoramaweb.org/_webdav" + path + "/%40files" + "?method=json");
                        AddChildFiles(uri, listView);
                    }
                }
                e.Node.BackColor = SystemColors.MenuHighlight;
                e.Node.ForeColor = Color.White;
            }


        }

        /// <summary>
        /// De-selects all nodes in the tree
        /// </summary>
        /// <param name="nodes"></param>
        private void ClearTreeRecursive(IEnumerable nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.BackColor = Color.White;
                node.ForeColor = Color.Black;
                ClearTreeRecursive(node.Nodes);
            }

        }

        /// <summary>
        /// Currently using for testing purposes
        /// </summary>
        /// <param name="nodes"></param>
        private void PrintTreeRecursive(IEnumerable nodes)
        {
            foreach (TreeNode node in nodes)
            {
                Debug.WriteLine(node.ToString());
                if (node.IsExpanded == true)
                {
                    ExpandedState.Add(1);
                } else
                {
                    ExpandedState.Add(0);
                }
                VisibleNodes.Add(node.Text);
                NodeNameTag.Add(node.Name);
                NodePathTag.Add((string)node.Tag);
                PrintTreeRecursive(node.Nodes);


            }

        }

        /// <summary>
        /// Resets the TreeView to display either all Panorama folders, or only Panorama folders containing .sky files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!restoring)
            {
                previous.Clear();
                next.Clear();
                back.Enabled = false;
                forward.Enabled = false;
                priorNode = null;
                up.Enabled = false;
                var treeNode = new TreeNode("Panorama Folders");
                treeView.Nodes.Clear();
                listView.Items.Clear();
                comboBox1.Visible = false;
                versionLabel.Visible = false;
                treeView.Nodes.Add(treeNode);
                CreateDummyNode(treeNode);
                //var dummy = new TreeNode("DummyNode");
                //treeNode.Nodes.Add(dummy);
                if (checkBox1.Checked)
                {
                    showingSky = true;
                    treeNode.Tag = "";
                    treeNode.Name = "First";
                }
                else
                {
                    showingSky = false;
                    var uri = new Uri(FileString);
                    JToken folders = null;
                    try
                    {
                        folders = GetInfoForFolders();
                    }
                    catch (Exception ex)
                    {
                    }

                }
            }
        }

        private void treeView2_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            //If the only node below is a dummy node
            //if (e.Node.FirstNode != null && e.Node.FirstNode.Text.Equals("DummyNode"))
            if (IsDummyNode(e.Node.FirstNode))
            {
                var path = (string)e.Node.Tag;
                if (checkBox1.Checked)
                {
                    //If this is the first time we're looking at the .sky folders, use a slightly different query to find all folders
                    if (e.Node.Name.Equals("First"))
                    {
                        e.Node.FirstNode.Remove();
                        AddInitialSkyFolders(e.Node);
                    }
                    else
                    {
                        AddQueryFolders(e.Node);
                    }
                }
                else
                { 
                    Uri uriFolder = new Uri("https://panoramaweb.org/project" + path +
                                          "/getContainers.view?includeSubfolders=true&moduleProperties=TargetedMS");
                    AddFolders(uriFolder, e.Node);
                }



            }
        }

        private void AddSelectedFiles(IEnumerable nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Name != string.Empty && node.Name.StartsWith("Selected"))
                {
                    node.Name = node.Name.Replace("Selected", "");
                    //Highlight the selected node
                    priorNode = node;
                    node.BackColor = SystemColors.MenuHighlight;
                    node.ForeColor = Color.White;
                    if (node.Parent == null)
                    {
                        up.Enabled = false;
                    }
                    lastSelected = node;
                    if (showingSky)
                    {
                        AddQueryFiles(listView, (string)node.Tag, versionLabel, comboBox1);
                    } else
                    {
                        Uri uri = new Uri("https://panoramaweb.org/_webdav" + (string)node.Tag + "/%40files" + "?method=json");
                        AddChildFiles(uri, listView);
                    }
                } else
                {
                    AddSelectedFiles(node.Nodes);
                }

            }
        }

        /// <summary>
        /// Uses a slightly different query to find all accessible folders containing targeted MS runs and adds them to the tree
        /// </summary>
        /// <param name="node"></param>
        private void AddInitialSkyFolders(TreeNode node)
        {
            var path = (string)node.Tag;
            string query = InitQuery;
            Uri queryUri = new Uri(query);
            var webClient = new WebClientWithCredentials(queryUri, userCopy, passCopy);
            JToken json = webClient.Get(queryUri);
            JToken rows = json[@"rows"];
            HashSet<string> prevFolders = new HashSet<string>();
            foreach (var row in rows)
            {
                string filePath = (string)row[@"Container/Path"];
                string origPath = filePath;
                string[] Arr = filePath.Split('/');
                //If we haven't seen this file path yet, try and add the folder it belongs in to the tree
                if (prevFolders.Add(origPath) && !string.IsNullOrEmpty(Arr[1]))
                {
                    TreeNode fileNode = new TreeNode(Arr[1]);
                    string newPath = path + Arr[1];
                    fileNode.Tag = "/" + newPath + "/";
                    fileNode.Name = origPath;

                    //Check to make sure this folder isn't already in the tree
                    bool inTree = false;
                    foreach (TreeNode curNode in node.Nodes)
                    {
                        string tag = (string)curNode.Tag;
                        if (tag != null && tag.Contains(newPath))
                        {
                            inTree = true;
                            if (curNode.Nodes.Count == 0)
                            {
                                CreateDummyNode(curNode);
                                //curNode.Nodes.Add(new TreeNode("DummyNode"));
                            }
                        }
                    }
                    if (!inTree)
                    {
                        //If this folder isn't in the tree, and the folder has subfolders, give it a dummy node
                        if (!origPath.Substring(1).Equals(newPath))
                        {
                            CreateDummyNode(fileNode);
                            //fileNode.Nodes.Add(new TreeNode("DummyNode"));
                        }
                        node.Nodes.Add(fileNode);
                    }
                }
            }
        }

        /// <summary>
        /// Displays either all versions of a Skyline file, or only the most recent version
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listView.Items.Clear();
            if (comboBox1.Text.Equals("Most recent version"))
            {
                GetLatestVersion((string)treeView.SelectedNode.Tag, listView);
            }
            else
            {
                AddQueryFiles(listView, (string)treeView.SelectedNode.Tag, versionLabel, comboBox1);
            }
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void open_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void RemoteFileDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            
            if (listView.SelectedItems.Count != 0)
            {
                _fileURL = listView.SelectedItems[0].Name;
            } else
            {
                _fileURL = "";
            }
            if (treeView.SelectedNode != null)
            {
                _folder = treeView.SelectedNode.Text;
            } else
            {
                _folder = "";
            }
            
            tree.Add(treeView);
            string treeState = state.GetPersistentString();
            if (checkBox1.Checked)
            {
                PanoramaClient.Properties.Settings.Default.skyFiles = true;
                showingSky = true;
            } else
            {
                PanoramaClient.Properties.Settings.Default.skyFiles = false;
                showingSky = false;
            }

            PanoramaClient.Properties.Settings.Default.state = treeState;
            PanoramaClient.Properties.Settings.Default.Save();
            if (lastSelected != null)
            {
                lastSelected.Name = "Selected" + lastSelected.Name;
            }
            
            _treeState = treeState;
            _nodesState = tree[0].Nodes;
            SaveTree(treeView, "testFile");
        }

        //Need to update and not use binary formatter: replace with JsonSerializer or XmlSerializer
        public static void SaveTree(TreeView tree, string filename)
        {
            using (Stream file = File.Open(filename, FileMode.Create))
            {
                BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                bf.Serialize(file, tree.Nodes.Cast<TreeNode>().ToList());
            }
        }

        //Need to update and not use binary formatter: replace with JsonSerializer or XmlSerializer
        public static void LoadTree(TreeView tree, string filename)
        {
            using (Stream file = File.Open(filename, FileMode.Open))
            {
                BinaryFormatter bf = new BinaryFormatter();
                object obj = bf.Deserialize(file);

                TreeNode[] nodeList = (obj as IEnumerable<TreeNode>).ToArray();
                tree.Nodes.AddRange(nodeList);
            }
        }

        private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            lastSelected = e.Node;
        }

        /// <summary>
        /// Downloads the selected file to the documents folder
        /// Currently hardcoded to my documents, needs to be changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string downloadName = listView.SelectedItems[0].Name;
            if (showingSky)
            {
                downloadName = downloadName.Replace("showPrecursorList", "downloadDocument");
            } 
            //Should use the webclient with credentials class instead
            using (WebClient wc = new WebClient())
            {
                wc.Credentials = new NetworkCredential(_user, _pass);
                wc.DownloadFileAsync(

                    // Param1 = Link of file
                    new System.Uri("https://panoramaweb.org/" + downloadName),
                    // Param2 = Path to save
                    "C:\\Users\\srpall\\documents\\" + listView.SelectedItems[0].Text
                );
                
            }
            FileAttributes attributes = File.GetAttributes("C:\\Users\\srpall\\documents\\" + listView.SelectedItems[0].Text);
            File.SetAttributes("C:\\Users\\srpall\\documents\\" + listView.SelectedItems[0].Text, FileAttributes.ReadOnly);
        }

        private void upButton_Click(object sender, EventArgs e)
        {
            if (lastSelected != null)
            {
                lastSelected.BackColor = Color.White;
                lastSelected.ForeColor = Color.Black;
                TreeNode parent = lastSelected.Parent;
                forward.Enabled = false;
                next.Clear();
                if (lastSelected != null && lastSelected != parent)
                {
                    previous.Push(priorNode);
                    back.Enabled = true;
                }
                priorNode = parent;
                treeView.SelectedNode = parent;
                lastSelected = parent;
                treeView.Focus();
                if (parent != null)
                {
                    listView.Items.Clear();
                    if (parent.Parent == null || parent.Text.Equals("Panorama Folders"))
                    {
                        up.Enabled = false;
                    }
                    else
                    {
                        if (showingSky)
                        {
                            AddQueryFiles(listView, (string)parent.Tag, versionLabel, comboBox1);
                        }
                        else
                        {
                            Uri uri = new Uri("https://panoramaweb.org/_webdav" + (string)parent.Tag + "/%40files" + "?method=json");
                            AddChildFiles(uri, listView);
                        }
                    }

                }

            }
        }

        private void back_Click(object sender, EventArgs e)
        {
            TreeNode prior = previous.Pop();
            next.Push(lastSelected);
            forward.Enabled = true;
            lastSelected.BackColor = Color.White;
            lastSelected.ForeColor = Color.Black;
            lastSelected = prior;
            treeView.SelectedNode = prior;
            treeView.Focus();
            listView.Items.Clear();
            priorNode = prior;
            if (lastSelected != null && !lastSelected.Text.Equals("Panorama Folders")) {
                up.Enabled = true;
            }
            if (previous.Count == 0)
            {
                back.Enabled = false;
            }
            if (showingSky)
            {
                AddQueryFiles(listView, (string)prior.Tag, versionLabel, comboBox1);
            }
            else
            {
                Uri uri = new Uri("https://panoramaweb.org/_webdav" + (string)prior.Tag + "/%40files" + "?method=json");
                AddChildFiles(uri, listView);
            }
        }

        private void forward_Click(object sender, EventArgs e)
        {
            previous.Push(lastSelected);
            back.Enabled = true;
            TreeNode nextNode = next.Pop();
            lastSelected.BackColor = Color.White;
            lastSelected.ForeColor = Color.Black;
            treeView.SelectedNode = nextNode;
            lastSelected = nextNode;
            treeView.Focus();
            listView.Items.Clear();
            if (nextNode != null && !nextNode.Text.Equals("Panorama Folders"))
            {
                up.Enabled = true;
            }
            if (next.Count == 0)
            {
                forward.Enabled = false;
            }
            if (showingSky)
            {
                AddQueryFiles(listView, (string)nextNode.Tag, versionLabel, comboBox1);
            }
            else
            {
                Uri uri = new Uri("https://panoramaweb.org/_webdav" + (string)nextNode.Tag + "/%40files" + "?method=json");
                AddChildFiles(uri, listView);
            }
        }

        public const string DUMMYNODE = "DummyNode";

        /// <summary>
        /// Creates a placeholder child node under a given current node
        /// </summary>
        /// <param name="curNode"></param>
        private static void CreateDummyNode(TreeNode curNode)
        {
            var dummyNode = new TreeNode(RemoteFileDialog.DUMMYNODE);
            curNode.Nodes.Add(dummyNode);
        }

        /// <summary>
        /// Returns true if the given node is a placeholder node, and false if not
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static bool IsDummyNode(TreeNode node)
        {
            if (node != null)
            {
                return node.Text.Equals(RemoteFileDialog.DUMMYNODE);
            }
            else
            {
                return false;
            }
        }
            
    }



    public class UTF8WebClient : WebClient
    {
        public UTF8WebClient()
        {
            Encoding = Encoding.UTF8;
        }

        public Uri ServerUri { get; private set; }

        public JObject Get(Uri uri)
        {
            var response = DownloadString(uri);
            return JObject.Parse(response);
        }

        public string DownloadStringAsync(Uri queryUri, string username, string password)
        {
            string data = null;
            Exception error = null;
            using (var webClient = new WebClientWithCredentials(ServerUri, username, password))
            {
                bool finishedDownloading = false;
                webClient.DownloadStringAsync(queryUri);
                webClient.DownloadStringCompleted += (sender, e) =>
                {
                    error = e.Error;
                    if (error == null)
                        data = e.Result;
                    finishedDownloading = true;
                };
                while (!finishedDownloading)
                {
                    
                }
            }
            if (error != null)
                throw error;
            return data;
        }
    }

    public class WebClientWithCredentials : UTF8WebClient
    {
        private CookieContainer _cookies = new CookieContainer();
        private string _csrfToken;
        private Uri _serverUri;

        private static string LABKEY_CSRF = @"X-LABKEY-CSRF";

        public WebClientWithCredentials(Uri serverUri, string username, string password)
        {
            // Add the Authorization header
            Headers.Add(HttpRequestHeader.Authorization, Server.GetBasicAuthHeader(username, password));
            _serverUri = serverUri;
        }
    }

    public sealed class Server
    {
        public Server(string uriText, string username, string password)
            : this(new Uri(uriText), username, password)
        {
        }

        public Server(Uri uri, string username, string password)
        {
            Username = username;
            Password = password;
            URI = uri;
        }

        internal string Username { get; set; }
        internal string Password { get; set; }
        internal Uri URI { get; set; }

        public string GetKey()
        {
            return URI.ToString();
        }

        internal string AuthHeader
        {
            get { return GetBasicAuthHeader(Username, Password); }
        }

        internal static string GetBasicAuthHeader(string username, string password)
        {
            byte[] authBytes = Encoding.UTF8.GetBytes(String.Format(@"{0}:{1}", username, password));
            var authHeader = @"Basic " + Convert.ToBase64String(authBytes);
            return authHeader;
        }
    }

   

    public class TreeViewStateRestorer
    {
        private readonly TreeView _tree;

        public TreeViewStateRestorer(TreeView tree)
        {
            _tree = tree;
        }

        /// <summary>
        /// Generates a persistent string storing information about the expansion and selection
        /// of nodes as well as the vertical scrolling of the form, separated by pipes
        /// </summary>
        public string GetPersistentString()
        {
            StringBuilder result = new StringBuilder();
            result.Append(GenerateExpansionString(_tree.Nodes)).Append('|');

            var treeMS = _tree as TreeViewMS;
            if (treeMS != null)
                result.Append(GenerateSelectionString()).Append('|');
            else
                result.Append(GenerateSingleSelectionString()).Append('|');

            result.Append(GenerateScrollString());
            return result.ToString();
        }

        /// <summary>
        /// The expansion string stores the indices of expanded nodes when called in the format
        /// a(b(c)), where a is the top level node as an integer, b is a child of a, and c
        /// is a child of b, etc. Multiple nodes and their children are stored as a comma-separated
        /// string, e.g. 0(1(0,1),2(0)),3
        /// </summary>
        private static string GenerateExpansionString(IEnumerable nodes)
        {
            int index = 0;
            StringBuilder result = new StringBuilder();
            foreach (TreeNode parent in nodes)
            {
                if (parent.IsExpanded)
                {
                    if (result.Length > 0)
                        result.Append(',');
                    result.Append(index);
                    string children = GenerateExpansionString(parent.Nodes);
                    if (children.Length != 0)
                    {
                        result.Append('(').Append(children).Append(')');
                    }
                }
                index++;
            }
            return result.ToString();
        }

        /// <summary>
        /// Gets the index of the selected node in a single-select TreeView according to the
        /// visual order of the nodes in the tree
        /// </summary>
        private int GenerateSingleSelectionString()
        {
            int index = 0;
            foreach (TreeNode node in VisibleNodes)
            {
                if (node.IsSelected)
                    return index;
                index++;
            }
            return 0;
        }

        /// <summary>
        /// <para>The selection string stores which nodes are selected in the graph. The first element
        /// is a single integer representing which node "the" selected node of the underlying TreeView.
        /// The remaining comma-separated elements in the string represent the indices of nodes
        /// that are selected according to the visual order of the nodes</para> 
        ///
        /// <para>These selections can be a single element (e.g. 1), a range (e.g. 1-7) or a disjoint selection
        /// consisting of multiple single elements and/or ranges (e.g. 1,3-6,8)</para>
        /// </summary>
        private string GenerateSelectionString()
        {
            StringBuilder selectedRanges = new StringBuilder();

            int index = 0;
            int rangeStart = -1;
            int prevSelection = -1;
            int selectedIndex = -1;

            foreach (TreeNodeMS node in VisibleNodes)
            {
                if (node.IsInSelection)
                {
                    if (rangeStart == -1)
                    {
                        rangeStart = index;
                    }
                    else if (index != prevSelection + 1)
                    {
                        AppendRange(selectedRanges, rangeStart, prevSelection);
                        rangeStart = index;
                    }
                    prevSelection = index;
                }

                // insert the TreeView selected node at the front of the string
                if (node.IsSelected)
                    selectedIndex = index;
                index++;
            }

            // complete any selection(s) that occur at the end of the tree
            if (rangeStart != -1)
            {
                AppendRange(selectedRanges, rangeStart, prevSelection);
            }

            return selectedIndex + @"," + selectedRanges;
        }

        private static void AppendRange(StringBuilder selectedRanges, int rangeStart, int prevSelection)
        {
            if (selectedRanges.Length > 0)
                selectedRanges.Append(',');

            if (rangeStart == prevSelection)
                selectedRanges.Append(rangeStart);
            else
                selectedRanges.AppendFormat(@"{0}-{1}", rangeStart, prevSelection);
        }

        /// <summary>
        /// The scroll string stores the numerical index of the first visible node in the form.
        /// The index corresponds to the location in the visual order of nodes in the form
        /// </summary>
        /// <returns></returns>
        private int GenerateScrollString()
        {
            int index = 0;
            foreach (TreeNode node in VisibleNodes)
            {
                if (node.IsVisible)
                    return index;
                index++;
            }
            return 0;
        }

        /// <summary>
        /// Restores the expansion and selection of the tree, and sets the top node for scrolling
        /// to be updated after all resizing has occured
        /// </summary>
        public void RestoreExpansionAndSelection(string persistentString)
        {
            if (!string.IsNullOrEmpty(persistentString))
            {
                string[] stateStrings = persistentString.Split('|');

                // check that the .view file will have the necessary information to rebuild the tree
                if (stateStrings.Length > 2)
                {
                    TreeViewMS treeMS = null;
                    try
                    {
                        _tree.BeginUpdate();

                        treeMS = _tree as TreeViewMS;
                        if (treeMS != null)
                            treeMS.AutoExpandSingleNodes = false;

                        ExpandTreeFromString(stateStrings[0]);

                        if (treeMS != null)
                            treeMS.AutoExpandSingleNodes = true;

                        SelectTreeFromString(stateStrings[1]);
                        NextTopNode = GetTopNodeFromString(stateStrings[2]);

                        if (treeMS != null)
                            treeMS.RestoredFromPersistentString = true;
                    }
                    catch (FormatException)
                    {
                        // Ignore and give up
                    }
                    finally
                    {
                        _tree.EndUpdate();
                        if (treeMS != null)
                            treeMS.AutoExpandSingleNodes = true;
                    }
                }
            }
        }

        /// <summary>
        /// Expands the tree from the persistent string data
        /// </summary>
        private void ExpandTreeFromString(string persistentString)
        {
            IEnumerator<char> dataEnumerator = persistentString.GetEnumerator();
            ExpandTreeFromString(_tree.Nodes, dataEnumerator);
        }

        private static bool ExpandTreeFromString(TreeNodeCollection nodes, IEnumerator<char> data)
        {
            bool finishedEnumerating = !data.MoveNext();
            int currentNode = 0;
            while (!finishedEnumerating)
            {
                char value = data.Current;
                switch (value)
                {
                    case ',':
                        finishedEnumerating = !data.MoveNext();
                        break;
                    case '(':
                        finishedEnumerating = ExpandTreeFromString(nodes[currentNode].Nodes, data);
                        break;
                    case ')':
                        return !data.MoveNext();
                    default: // value must be an integer
                        StringBuilder dataIndex = new StringBuilder();
                        dataIndex.Append(value);
                        finishedEnumerating = !data.MoveNext();

                        // enumerate until the next element is not an integer
                        while (!finishedEnumerating && data.Current != ',' && data.Current != '(' && data.Current != ')')
                        {
                            dataIndex.Append(data.Current);
                            finishedEnumerating = !data.MoveNext();
                        }

                        currentNode = int.Parse(dataIndex.ToString());

                        // if invalid node in tree, return
                        if (currentNode >= nodes.Count)
                            return true;
                        nodes[currentNode].Expand();
                        break;
                }
            }
            return true;
        }

        /// <summary>
        /// Reselects tree nodes from the persistent string data
        /// </summary>
        private void SelectTreeFromString(string persistentString)
        {
            IList<TreeNode> visualOrder = VisibleNodes.ToArray();
            int nodeCount = visualOrder.Count;
            string[] selections = persistentString.Split(',');

            // select first element separately, returning if it is not a valid node
            int selectedIndex = int.Parse(selections[0]);
            if (selectedIndex < 0 || selectedIndex >= nodeCount)
                return;
            _tree.SelectedNode = visualOrder[selectedIndex];

            var tree = _tree as TreeViewMS;

            // add remaining nodes to selection (if TreeViewMS)
            if (tree != null)
            {
                for (int i = 1; i < selections.Length; i++)
                {
                    string selection = selections[i];
                    if (selection.Contains(@"-")) // when true, the string represents a range and not a single element
                    {
                        string[] range = selection.Split('-');
                        int start = Math.Min(nodeCount - 1, Math.Max(0, int.Parse(range[0])));
                        int end = Math.Min(nodeCount - 1, Math.Max(0, int.Parse(range[1])));
                        for (int j = start; j <= end; j++)
                        {
                            tree.SelectNode((TreeNodeMS)visualOrder[j], true);
                        }
                    }
                    else // the string represents a single element
                    {
                        int index = int.Parse(selection);
                        if (0 > index || index >= nodeCount)
                            return;
                        tree.SelectNode((TreeNodeMS)visualOrder[index], true);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the top node (for scrolling) for update when the tree has finished resizing
        /// </summary>
        private TreeNode GetTopNodeFromString(string persistentString)
        {
            IList<TreeNode> nodes = VisibleNodes.ToArray();
            int index = int.Parse(persistentString);
            if (0 > index || index >= nodes.Count)
                return null;
            return nodes[index];
        }

        private TreeNode NextTopNode { get; set; }

        /// <summary>
        /// Updates the top node in order to establish the correct scrolling of the tree. This should
        /// not be called until all resizing of the tree has occured
        /// </summary>
        public void UpdateTopNode()
        {
            _tree.TopNode = NextTopNode ?? _tree.TopNode;
        }

        /// <summary>
        /// Generates the visual order of nodes as they appear in the tree
        /// </summary>
        private IEnumerable<TreeNode> VisibleNodes
        {
            get
            {
                for (TreeNode node = _tree.Nodes.Count > 0 ? _tree.Nodes[0] : null; node != null; node = node.NextVisibleNode)
                    yield return node;
            }
        }
    }
    

   

    /// <summary>
    /// A MultiSelect TreeView.
    /// <para>
    /// Inspired by the example at http://www.codeproject.com/KB/tree/treeviewms.aspx for details.</para>
    /// </summary>
    public abstract class TreeViewMS : TreeView
    {
        // Length of the horizontal dashed lines representing each branch of the tree
        protected internal const int HORZ_DASH_LENGTH = 11;
        // Text padding
        protected internal const int PADDING = 3;
        // Width of images associated with the tree
        protected internal const int IMG_WIDTH = 16;

        private TreeNodeMS _anchorNode;
        private bool _inRightClick;

        private const int DEFAULT_ITEM_HEIGHT = 16;
        private const float DEFAULT_FONT_SIZE = (float)8.25;

        public const double DEFAULT_TEXT_FACTOR = 1;
        public const double LRG_TEXT_FACTOR = 1.25;
        public const double XLRG_TEXT_FACTOR = 1.5;

        protected TreeViewMS()
        {
            UseKeysOverride = false;
            _inRightClick = false;

            SelectedNodes = new TreeNodeSelectionMS();

            SetStyle(ControlStyles.UserPaint, true);
            ItemHeight = DEFAULT_ITEM_HEIGHT;

            TreeStateRestorer = new TreeViewStateRestorer(this);
            AutoExpandSingleNodes = true;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]

        public TextureBrush DashBrush { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ICollection<TreeNodeMS> SelectedNodes { get; private set; }

        // If true, disjoint select is enabled.
        private bool _allowDisjoint;

        /// <summary>
        /// For functional testing of multiple selection code.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Keys KeysOverride { get; set; }

        public bool UseKeysOverride { get; set; }

        private Keys ModifierKeysOverriden
        {
            // If the control key is overriden, we can assume disjoint select was intended. 
            get
            {
                if (KeysOverride == Keys.Control)
                    _allowDisjoint = true;
                return UseKeysOverride ? KeysOverride : ModifierKeys;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // It is necessary to make sure that just the CTRL key is pressed and no modifiers -
            // else, CTRL+C, CTRL+V, CTRL+Z .. etc can all cause incorrect selections.
            // The combination below represents just the CTRL key command key.
            // Modifiers to CTRL are sent as a seperate, following command key, which will disable disjoint select.
            if (!UseKeysOverride)
                _allowDisjoint = keyData == (Keys.Control | Keys.LButton | Keys.ShiftKey);
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected bool IsDisjointSelect
        {
            // Disjoint select only occurs when the control key is held, so check that first then check
            // allow disjoint to check for modifiers to the control key.
            get { return ModifierKeysOverriden == Keys.Control && _allowDisjoint; }
        }

        protected bool IsRangeSelect { get { return ModifierKeysOverriden == Keys.Shift; } }

        public void SelectNode(TreeNodeMS node, bool select)
        {
            if (!select)
                SelectedNodes.Remove(node);
            else if (!node.IsInSelection)
            {
                SelectedNodes.Add(node);
                // Make sure all ancestors of this node are expanded
                for (var parent = node.Parent; parent != null && !parent.IsExpanded; parent = parent.Parent)
                {
                    parent.Expand();
                }
            }
            node.IsInSelection = select;
        }

        public bool IsNodeSelected(TreeNode node)
        {
            return node is TreeNodeMS && ((TreeNodeMS)node).IsInSelection;
        }

        protected void UpdateSelection()
        {
            // Remove any nodes from the selection that may have been
            // removed from the tree.
            var selectedNodes = SelectedNodes.ToArray();
            foreach (var node in selectedNodes)
            {
                if (node.TreeView == null)
                    SelectedNodes.Remove(node);
            }

            // If any nodes were removed from the selection, reset the
            // anchor node to the selected node.
            if (selectedNodes.Length != SelectedNodes.Count)
                _anchorNode = (TreeNodeMS)SelectedNode;
        }

        [Browsable(true)]
        public bool AutoExpandSingleNodes { get; set; }

        protected void TreeViewMS_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (!IsInUpdate)
            {
                TreeNode nodeTree = e.Node;
                if (IsParentNode(nodeTree))
                {
                    // Save and restore top node to keep from scrolling
                    TreeNode nodeTop = TopNode;

                    int children = EnsureChildren(nodeTree);

                    // Do the Windows explorer thing of expanding single node children.
                    if (AutoExpandSingleNodes && children == 1)
                        nodeTree.Nodes[0].Expand();

                    TopNode = nodeTop;
                }
            }
        }

        protected abstract bool IsParentNode(TreeNode node);

        protected abstract int EnsureChildren(TreeNode node);

        public bool RestoredFromPersistentString { get; set; }
        private TreeViewStateRestorer TreeStateRestorer { get; set; }

        public string GetPersistentString()
        {
            return TreeStateRestorer.GetPersistentString();
        }

        public void RestoreExpansionAndSelection(string persistentString)
        {
            TreeStateRestorer.RestoreExpansionAndSelection(persistentString);
        }

        public void UpdateTopNode()
        {
            TreeStateRestorer.UpdateTopNode();
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

        public void ScrollLeft()
        {
            SetScrollPos(Handle, 0, 0, true);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _inRightClick = e.Button == MouseButtons.Right;
            if (_inRightClick)
            {
                TreeNodeMS node = (TreeNodeMS)GetNodeAt(0, e.Y);
                if (node != null && node.BoundsMS.Contains(e.Location))
                    SelectedNode = node;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (!_inRightClick)
            {
                TreeNodeMS node = (TreeNodeMS)GetNodeAt(0, e.Y);
                if (node != null && node.BoundsMS.Contains(e.Location))
                {
                    // If we are within the bounds of a node and that node is not the selected node,
                    // make it the selected node. Changing the font of the TreeView at runtime
                    // apparently does not update node bounds, so we need to do this check in case the
                    // user clicked outside of the original node bounds.
                    if (!ReferenceEquals(node, SelectedNode))
                        SelectedNode = node;
                    // Handle cases where clicking on the selected node should change
                    // the selection.
                    else
                    {
                        // Disjoint selection or the SelectedNode is not in the selection
                        if (IsDisjointSelect || !IsNodeSelected(node))
                            SelectedNode = null;
                        // More than a single node currently selected, and not performing
                        // range selection on an existing range selection.
                        else if (SelectedNodes.Count > 1 &&
                                    (!IsRangeSelect || ReferenceEquals(_anchorNode, SelectedNode)))
                            SelectedNode = null;
                    }
                }
            }
            base.OnMouseUp(e);
            _inRightClick = false;
        }

        protected override void OnBeforeSelect(TreeViewCancelEventArgs e)
        {
            base.OnBeforeSelect(e);

            // New selection is always the anchor for the next shift selection
            if (_anchorNode == null || !IsRangeSelect)
                _anchorNode = (TreeNodeMS)e.Node;
        }

        protected override void OnAfterSelect(TreeViewEventArgs e)
        {
            // Save old selection for invalidating
            var selectedNodesOld = SelectedNodes.ToArray();

            TreeNodeMS node = (TreeNodeMS)e.Node;

            // Don't change the selection if this is a right click and the node is in the
            // selection.
            if (node != null && !(_inRightClick && node.IsInSelection))
            {
                if (IsDisjointSelect)
                {
                    // Toggle selection on the node
                    SelectNode(node, !IsNodeSelected(e.Node));
                }
                else if (IsRangeSelect && !ReferenceEquals(_anchorNode, node))
                {
                    // Figure out top and bottom of the range to be selected
                    TreeNodeMS upperNode = _anchorNode;
                    TreeNodeMS bottomNode = node;
                    if (upperNode.BoundsMS.Top > bottomNode.BoundsMS.Top)
                        Swap(ref upperNode, ref bottomNode);

                    // Set new selection to contain all visible nodes between top and bottom
                    SelectedNodes.Clear();
                    while (upperNode != null && !ReferenceEquals(upperNode, bottomNode))
                    {
                        SelectNode(upperNode, true);
                        upperNode = (TreeNodeMS)upperNode.NextVisibleNode;
                    }
                    SelectNode(bottomNode, true);
                }
                else
                {
                    // Make this a single selection of the selected node.
                    SelectedNodes.Clear();
                    SelectNode(node, true);
                }

                // Invalidate the changed nodes
                var unchangedNodes = new HashSet<TreeNodeMS>(selectedNodesOld.Intersect(SelectedNodes));
                InvalidateChangedNodes(selectedNodesOld, unchangedNodes);
                InvalidateChangedNodes(SelectedNodes, unchangedNodes);
            }

            Invalidate();

            // Make sure selection is updated before after select event is fired
            base.OnAfterSelect(e);
        }

        public static void Swap<TItem>(ref TItem val1, ref TItem val2)
        {
            TItem tmp = val1;
            val1 = val2;
            val2 = tmp;
        }



        private void InvalidateChangedNodes(IEnumerable<TreeNodeMS> nodes, ICollection<TreeNodeMS> unchangedNodes)
        {
            if (IsInUpdate)
                return;

            foreach (var node in nodes)
            {
                if (!unchangedNodes.Contains(node))
                    InvalidateNode(node);
            }
        }

        protected void InvalidateNode(TreeNodeMS node)
        {
            Invalidate(new Rectangle(0, node.BoundsMS.Top, ClientRectangle.Width, node.BoundsMS.Height));
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {

        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // If we have nodes, then we have to draw them - and that means everything
            // about the node.
            using (var backColorBrush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(backColorBrush, ClientRectangle);
            }

            // No painting beyond the background while updating, since it can cause
            // unexpected exceptions.  This used to happen during a node removal that
            // caused removal of the control's scrollbar.
            if (IsInUpdate)
                return;

            // Draw all nodes exposed in the paint clipping rectangle.
            var drawRect = e.Graphics.ClipBounds;
            drawRect.Intersect(e.ClipRectangle);
            int bottom = (int)drawRect.Bottom;
            for (var node = TopNode;
                node != null && node.Bounds.Top <= bottom;
                node = node.NextVisibleNode)
            {
                ((TreeNodeMS)node).DrawNodeCustom(e.Graphics, ClientRectangle.Right);
            }
        }

        private int _updateLockCount;

        public bool IsInUpdate { get { return _updateLockCount > 0; } }

        public void BeginUpdateMS()
        {
            BeginUpdate();
            _updateLockCount++;
        }

        public void EndUpdateMS()
        {
            if (_updateLockCount == 0)
                return;
            if (--_updateLockCount == 0)
                UpdateSelection();
            EndUpdate();
        }

        private class TreeNodeSelectionMS : ICollection<TreeNodeMS>
        {
            private readonly List<TreeNodeMS> _nodes = new List<TreeNodeMS>();

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEnumerator<TreeNodeMS> GetEnumerator()
            {
                return _nodes.GetEnumerator();
            }

            public void Add(TreeNodeMS item)
            {
                if (item == null)
                {
                    throw new ArgumentNullException();
                }
                item.IsInSelection = true;
                _nodes.Add(item);
            }

            public void Clear()
            {
                _nodes.ForEach(node => node.IsInSelection = false);
                _nodes.Clear();
            }

            public bool Contains(TreeNodeMS item)
            {
                return _nodes.Contains(item);
            }

            public void CopyTo(TreeNodeMS[] array, int arrayIndex)
            {
                _nodes.CopyTo(array, arrayIndex);
            }

            public bool Remove(TreeNodeMS item)
            {
                if (item == null)
                {
                    return false;
                }
                if (_nodes.Remove(item))
                {
                    item.IsInSelection = false;
                    return true;
                }
                return false;
            }

            public int Count
            {
                get { return _nodes.Count; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }
        }
    }

    public class TreeNodeMS : TreeNode
    {
        private const TextFormatFlags FORMAT_TEXT = TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;

        public TreeNodeMS()
        {
        }

        public TreeNodeMS(string text) : base(text)
        {
        }

        /// <summary>
        /// Returns a typed reference to the owning <see cref="SequenceTree"/>.
        /// </summary>
        public TreeViewMS TreeViewMS { get { return (TreeViewMS)TreeView; } }

        public bool IsInSelection { get; protected internal set; }

        public Color ForeColorMS
        {
            get
            {
                if (!(IsSelected && IsInSelection) || !TreeViewMS.Focused)
                    return ForeColor;
                return SystemColors.HighlightText;
            }
        }




     

        protected double _textZoomFactor;
        protected string _widthText;
        protected int _widthCustom;
        protected IList<Color> _groupColors;

        protected virtual int WidthCustom
        {
            get { return _widthCustom > 0 ? _widthCustom : Bounds.Width; }
        }

  

        /// <summary>
        /// Because these nodes allow override of text drawing, this virtual
        /// is required to get the true bounds of the text that is drawn.
        /// </summary>
        public Rectangle BoundsMS
        {
            get
            {
                var bounds = Bounds;
                bounds.Width = WidthCustom;
                return bounds;
            }
        }

        public int XIndent
        {
            // Finds the X coordinate of the indent for this node, accounting for horizontal scrolling.
            get
            {
                int treeIndent = TreeViewMS.HORZ_DASH_LENGTH + TreeViewMS.PADDING;
                // Always indent for the node image, whether it has one or not
                treeIndent += TreeViewMS.IMG_WIDTH;
                // Only indent for the state image, if it has one
                if (StateImageIndex != -1)
                    treeIndent += TreeViewMS.IMG_WIDTH;
                return BoundsMS.X - treeIndent;
            }
        }

        public int HorizScrollDiff
        {
            get
            {
                return XIndent - (Level * TreeView.Indent + 11);
            }
        }

        public virtual void DrawNodeCustom(Graphics g, int rightEdge)
        {

            Rectangle bounds = BoundsMS;

            // Draw dashed lines
            var treeView = TreeViewMS;
            var dashBrush = treeView.DashBrush;
            // Horizontal line.
            dashBrush.TranslateTransform(Level % 2 + HorizScrollDiff, 0);
            g.FillRectangle(dashBrush, XIndent, bounds.Top + bounds.Height / 2,
                TreeViewMS.HORZ_DASH_LENGTH, 1);
            // Vertical lines corresponding to the horizontal level of this node.
            dashBrush.TranslateTransform(-Level % 2 - HorizScrollDiff, 0);
            // Check if this is the Root.
            if (ReferenceEquals(this, treeView.Nodes[0]))
            {
                if (treeView.Nodes.Count > 1)
                {
                    g.FillRectangle(dashBrush, XIndent, bounds.Top + bounds.Height / 2,
                        1, bounds.Height / 2);
                }
            }
            // Move up the levels of the tree, drawing the corresponding vertical lines.
            else
            {
                try
                {
                    TreeNodeMS curNode = this;
                    while (curNode != null)
                    {
                        dashBrush.TranslateTransform(0, curNode.Level % 2);
                        if (curNode.NextNode != null)
                            g.FillRectangle(dashBrush, curNode.XIndent, bounds.Top, 1, bounds.Height);
                        else if (curNode == this)
                            g.FillRectangle(dashBrush, curNode.XIndent, bounds.Top, 1, bounds.Height / 2);
                        dashBrush.TranslateTransform(0, -curNode.Level % 2);
                        curNode = curNode.Parent as TreeNodeMS;
                    }
                }
                catch (NullReferenceException)
                {
                    // Ignore a NullReferenceException in this code.  The case
                    // that once caused this has been fixed, but this safeguard is
                    // kept to avoid showing an unhandled exception to the user.

                    // If the node being painted is in the process of being removed
                    // from the tree, then curNode.NextNode will throw a NRE.
                }
            }


            // Draw images associated with the node.
            int imgLocX = XIndent + TreeViewMS.HORZ_DASH_LENGTH;
            const int imgWidth = TreeViewMS.IMG_WIDTH, imgHeight = TreeViewMS.IMG_WIDTH;
            if (StateImageIndex != -1)
            {
                Image stateImg = TreeView.StateImageList.Images[StateImageIndex];
                g.DrawImageUnscaled(stateImg, imgLocX, bounds.Top + (bounds.Height - imgHeight) / 2, imgWidth, imgHeight);
                imgLocX += imgWidth;
            }
            if (ImageIndex != -1)
            {
                Image nodeImg = TreeView.ImageList.Images[ImageIndex];
                g.DrawImageUnscaled(nodeImg, imgLocX, bounds.Top + (bounds.Height - imgHeight) / 2, imgWidth, imgHeight);
            }

               
        }

           

            
    }

    public class FileSizeFormatProvider : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter)) return this;
            return null;
        }

        private const string FILE_SIZE_FORMAT = "fs";
        private const Decimal ONE_KILO_BYTE = 1024M;
        private const Decimal ONE_MEGA_BYTE = ONE_KILO_BYTE * 1024M;
        private const Decimal ONE_GIGA_BYTE = ONE_MEGA_BYTE * 1024M;

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (format == null || !format.StartsWith(FILE_SIZE_FORMAT))
            {
                return DefaultFormat(format, arg, formatProvider);
            }

            if (arg is string)
            {
                return DefaultFormat(format, arg, formatProvider);
            }

            Decimal size;

            try
            {
                size = Convert.ToDecimal(arg);
            }
            catch (InvalidCastException)
            {
                return DefaultFormat(format, arg, formatProvider);
            }

            string suffix;

            if (size > ONE_GIGA_BYTE)
            {
                size /= ONE_GIGA_BYTE;
                suffix = @" GB";
            }
            else if (size > ONE_MEGA_BYTE)
            {
                size /= ONE_MEGA_BYTE;
                suffix = @" MB";
            }
            else if (size > ONE_KILO_BYTE)
            {
                size /= ONE_KILO_BYTE;
                suffix = @" KB";
            }
            else
            {
                suffix = @" B";
            }

            string precision = format.Substring(2);
            if (String.IsNullOrEmpty(precision))
                precision = @"2";
            string formatString = @"{0:N" + precision + @"}{1}";  // Avoid ReSharper analysis
            return String.Format(formatString, size, suffix);
        }

        private static string DefaultFormat(string format, object arg, IFormatProvider formatProvider)
        {
            IFormattable formattableArg = arg as IFormattable;
            if (formattableArg != null)
            {
                return formattableArg.ToString(format, formatProvider);
            }
            return arg.ToString();
        }
    }

    public struct FileSize : IComparable
    {
        private static readonly FileSizeFormatProvider FORMAT_PROVIDER = new FileSizeFormatProvider();
        public static FileSizeFormatProvider FormatProvider
        {
            get { return FORMAT_PROVIDER; }
        }
        public FileSize(long byteCount) : this()
        {
            ByteCount = byteCount;
        }

        public long ByteCount { get; private set; }
        public override string ToString()
        {
            return String.Format(FORMAT_PROVIDER, @"{0:fs}", ByteCount);
        }

        public int CompareTo(object obj)
        {
            if (null == obj)
            {
                return 1;
            }
            if (!(obj is FileSize))
            {
                throw new ArgumentException(@"Must be FileSize");
            }
            return ByteCount.CompareTo(((FileSize)obj).ByteCount);
        }
    }


}
