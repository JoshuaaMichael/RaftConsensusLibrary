using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using TeamDecided.RaftCommon.Logging;

namespace RaftPrototype
{
    public partial class RaftBootStrap : Form
    {
        private const int MAXIMUM_NODES = 9;
        private const int MINIMUM_NODES = 3;
        private const int DEFAULT_NODES = 3;
        private const int START_PORT = 5555;

        private const string MAX_NODES_WARNING = "Maximum nine (9) nodes supported in prototype";
        private const string MIN_NODES_WARNING = "Consensus requires minimum three (3) nodes";
        private const string CLUSTER_NAME = "Prototype Cluster";
        private const string CLUSTER_PASSWD = "password";
        private const string IP_TO_BIND = "127.0.0.1";

        private const string CONFIG_FILE = "./config.json";
        private string LOGFILE = Path.Combine(Environment.CurrentDirectory, "debug.log");

        private List<Tuple<string, string, int>> cluster_peer_list = new List<Tuple<string, string, int>>();
        private RaftNode[] nodes;

        protected StatusBar mainStatusBar = new StatusBar();
        protected StatusBarPanel statusPanel = new StatusBarPanel();
        protected StatusBarPanel datetimePanel = new StatusBarPanel();

        public RaftBootStrap()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            SetupDebug();
            //add defaults to GUI
            tbClusterName.Text = CLUSTER_NAME;
            tbClusterPasswd.Text = CLUSTER_PASSWD;
            tbPort.Text = START_PORT.ToString();
            tbIPAddress.Text = IP_TO_BIND;
            tbIPAddress.Enabled = false;//don't want the user to change this at the moment
            cbInstantiate.Checked = true;//set to instantiate by default

            SetNodeCountSelector();// setup node numeric up down UI

            CreateGridView();//Populate the datagrid with nodeName, ipAddress and port based of GUI information provided
        }

        private void SetupDebug()
        {
            //string path = string.Format(@"{0}", Environment.CurrentDirectory);
            //string debug = Path.Combine(Environment.CurrentDirectory, "debug.log");
            //string debug = Path.Combine("C:\\Users\\Tori\\Downloads\\debug.log");

            RaftLogging.Instance.OverwriteLoggingFile(LOGFILE);
            RaftLogging.Instance.DeleteExistingLogFile();
            RaftLogging.Instance.SetDoInfo(true);
            RaftLogging.Instance.SetDoDebug(true);
        }

        private void SetNodeCountSelector()
        {
            // Set the Minimum, Maximum, and initial Value.
            nNodes.Value = DEFAULT_NODES;
            nNodes.Maximum = MAXIMUM_NODES;
            nNodes.Minimum = MINIMUM_NODES;
        }

        private void Create_Button_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Nodes_ValueChanged(object sender, EventArgs e)
        {
            if (nNodes.Value == 9)
            {
                lWarningNodesNumber.ForeColor = Color.Red;
                lWarningNodesNumber.Visible = true;
                statusPanel.Text = MAX_NODES_WARNING;
                lWarningNodesNumber.Text= MAX_NODES_WARNING;
            }
            else if (nNodes.Value == 3)
            {
                lWarningNodesNumber.ForeColor = Color.Red;
                lWarningNodesNumber.Visible = true;
                statusPanel.Text = MIN_NODES_WARNING;
                lWarningNodesNumber.Text = MIN_NODES_WARNING;
            }
            else
            {
                lWarningNodesNumber.Visible = false;
                statusPanel.Text = string.Format("Configuration file: {0}", Path.Combine(Environment.CurrentDirectory, "config.json"));
            }
            nodeConfigDataView.DataSource = null;
            CreateGridView();
        }

        private void CreateGridView()
        {
            // temporary datasource
            cluster_peer_list = new List<Tuple<string, string, int>>();
            int maxNodes = (int) nNodes.Value;

            for (int i = 0; i< maxNodes; i++)
            {
                string nodeName = string.Format("Node{0}", i + 1);
                string nodeIP = IP_TO_BIND;
                int nodePort = int.Parse(tbPort.Text) + i;

                Tuple<string, string, int> temp = new Tuple<string, string, int> (nodeName, nodeIP, nodePort);
                cluster_peer_list.Add(temp);
            }

            nodeConfigDataView.DataSource = cluster_peer_list;
            nodeConfigDataView.Columns[0].HeaderText = "Name";
            nodeConfigDataView.Columns[1].HeaderText = "IP Address";
            nodeConfigDataView.Columns[2].HeaderText = "Port";

            foreach (DataGridViewColumn col in nodeConfigDataView.Columns)
            {
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

        }

        private void TbPort_textChangedEventHandler(object sender, EventArgs e)
        {
            CreateGridView();
        }

        private void CreateRaftNodes_Click(object sender, EventArgs e)
        {
            ///Create config file, and save it, then start people
            ///Information for config file is represented by information in GUI
            int maxNodes = (int)nNodes.Value;

            //create a config file structure
            RaftBootstrapConfig rbsc = new RaftBootstrapConfig
            {
                clusterName = tbClusterName.Text,
                clusterPassword = tbClusterPasswd.Text,//should this really be plain text!
                maxNodes = maxNodes//set max nodes, generic for all
            };

            foreach (var peer in cluster_peer_list)
            {
                rbsc.nodeNames.Add(peer.Item1);
                rbsc.nodeIPAddresses.Add(peer.Item2);
                rbsc.nodePorts.Add(peer.Item3);
            }

            // serialize the config object
            string json = JsonConvert.SerializeObject(rbsc, Formatting.Indented);

            //create new config file 
            File.Delete(CONFIG_FILE);
            File.WriteAllText(CONFIG_FILE, json);


            if (cbInstantiate.Checked)
            {
                //create array of RaftNode
                nodes = new RaftNode[maxNodes];

                for (int i = 0; i < rbsc.nodeNames.Count; i++)
                {
                    nodes[i] = new RaftNode(rbsc.nodeNames[i], CONFIG_FILE, LOGFILE);
                    nodes[i].FormClosed += new FormClosedEventHandler(RaftNodeClosure);
                    nodes[i].Show();
                    //this.Enabled = false;
                    Thread.Sleep(500);
                }

                this.Enabled = false;
                //Hide();
            }
            else
            {
                //create default start info for the process
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = System.Reflection.Assembly.GetEntryAssembly().Location,
                    WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location),
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                //start up node1 first so that it can become leader
                startInfo.Arguments = string.Format("{0} {1} {2}", rbsc.nodeNames[0], CONFIG_FILE, string.Format("{0}-debug.log", rbsc.nodeNames[0]));
                Process.Start(startInfo);
                //sleep to give head start for setting it self up
                Thread.Sleep(200);

                for (int i = rbsc.nodeNames.Count - 1; i > 0; i--)
                {
                    //start up the rest of the nodes
                    startInfo.Arguments = string.Format("{0} {1} {2}", rbsc.nodeNames[i], CONFIG_FILE, string.Format("{0}-debug.log", rbsc.nodeNames[i]));
                    Process.Start(startInfo);
                }

                Close();
            }
        }

        private void RaftNodeClosure(object sender, FormClosedEventArgs e)
        {
            int index = int.Parse(sender.ToString().Substring(sender.ToString().Length - 1)) - 1;

            nodes[index] = null;

            //MessageBox.Show(sender.ToString() + "\n" + index);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null)
                {
                    break;
                }
                this.Enabled = true;
            }
        }

        #region old methods to cleanout
        private void CreateRaftNodes_WithInstantiate_Click(object sender, EventArgs e)
        {

        }
        private void CreateRaftNodes_WithStartInfo_Click(object sender, EventArgs e)
        {
            ///Create config file, and save it, then start people
            ///Information for config file is represented by information in GUI
            int maxNodes = (int)nNodes.Value;

            //create a config file structure
            RaftBootstrapConfig rbsc = new RaftBootstrapConfig
            {
                clusterName = tbClusterName.Text,
                clusterPassword = tbClusterPasswd.Text,//should this really be plain text!
                maxNodes = maxNodes//set max nodes, generic for all
            };

            foreach (var peer in cluster_peer_list)
            {
                rbsc.nodeNames.Add(peer.Item1);
                rbsc.nodeIPAddresses.Add(peer.Item2);
                rbsc.nodePorts.Add(peer.Item3);
            }

            // serialize the config object
            string json = JsonConvert.SerializeObject(rbsc, Formatting.Indented);

            //create new config file 
            File.Delete(CONFIG_FILE);
            File.WriteAllText(CONFIG_FILE, json);

            ////The commented out code below is for testing RaftNode with debug
            //RaftNode node = new RaftNode(rbsc.nodeNames[0], configFile);
            //this is the leader window
            //node.Show();

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = System.Reflection.Assembly.GetEntryAssembly().Location,
                WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location),
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Normal
            };

            startInfo.Arguments = string.Format("{0} {1} {2}", rbsc.nodeNames[0], CONFIG_FILE, string.Format("{0}-debug.log", rbsc.nodeNames[0]));
            Process.Start(startInfo);
            Thread.Sleep(200);

            for (int i = rbsc.nodeNames.Count - 1; i > 0; i--)
            {
                startInfo.Arguments = string.Format("{0} {1} {2}", rbsc.nodeNames[i], CONFIG_FILE, string.Format("{0}-debug.log", rbsc.nodeNames[i]));
                Process.Start(startInfo);

                /// seems to perform better with this sleep on all Process.Start() calls. 
                /// This value increases to 1750ms when we re enable the JoinCluster call 
                /// within RaftNode, however still volatile and doesn't open window 100% 
                /// of the time
            }

            Close();
        }
        #endregion

        #region Click events that keep geting deleted by IDE
        private void cCreateRaftNodes_WithInstantiate_Click(object sender, EventArgs e)
        {
            ///Create config file, and save it, then start people
            ///Information for config file is represented by information in GUI

            int maxNodes = (int)nNodes.Value;

            //create a config file structure
            RaftBootstrapConfig rbsc = new RaftBootstrapConfig
            {
                clusterName = tbClusterName.Text,
                clusterPassword = tbClusterPasswd.Text,//should this really be plain text!
                maxNodes = maxNodes//set max nodes, generic for all
            };

            foreach (var node in cluster_peer_list)
            {
                rbsc.nodeNames.Add(node.Item1);
                rbsc.nodeIPAddresses.Add(node.Item2);
                rbsc.nodePorts.Add(node.Item3);
            }

            string json = JsonConvert.SerializeObject(rbsc, Formatting.Indented);

            File.Delete(CONFIG_FILE);
            File.WriteAllText(CONFIG_FILE, json);

            ////The commented out code below is for testing RaftNode with debug
            //RaftNode node = new RaftNode(rbsc.nodeNames[0], configFile);
            //this is the leader window
            //node.Show();

            RaftNode[] nodes = new RaftNode[maxNodes];

            //string s = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), string.Format("{0}-debug.log", rbsc.nodeNames[i]));

            for (int i = 0; i < rbsc.nodeNames.Count; i++)
            {
                ////Let's start the leader with a 500ms head start (sleep) before starting the rest
                //if (i == 1)
                //{
                //    Thread.Sleep(1000);
                //}

                //nodes[i] = new RaftNode(rbsc.nodeNames[i], configFile, Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), string.Format("{0}-debug.log", rbsc.nodeNames[i])));
                nodes[i] = new RaftNode(rbsc.nodeNames[i], CONFIG_FILE, string.Format("{0}-debug.log", rbsc.nodeNames[i]));
                nodes[i].Show();
                /// seems to perform better with this sleep on all Process.Start() calls. 
                /// This value increases to 1750ms when we re enable the JoinCluster call 
                /// within RaftNode, however still volatile and doesn't open window 100% 
                /// of the time
                Thread.Sleep(500);
            }

            Hide();
        }
        private void cCreateRaftNodes_WithStartInfo_Click(object sender, EventArgs e)
        {
            ///Create config file, and save it, then start people
            ///Information for config file is represented by information in GUI

            int maxNodes = (int)nNodes.Value;

            //create a config file structure
            RaftBootstrapConfig rbsc = new RaftBootstrapConfig
            {
                clusterName = tbClusterName.Text,
                clusterPassword = tbClusterPasswd.Text,//should this really be plain text!
                maxNodes = maxNodes//set max nodes, generic for all
            };

            foreach (var node in cluster_peer_list)
            {
                rbsc.nodeNames.Add(node.Item1);
                rbsc.nodeIPAddresses.Add(node.Item2);
                rbsc.nodePorts.Add(node.Item3);
            }

            string json = JsonConvert.SerializeObject(rbsc, Formatting.Indented);

            File.Delete(CONFIG_FILE);
            File.WriteAllText(CONFIG_FILE, json);

            ////The commented out code below is for testing RaftNode with debug
            //RaftNode node = new RaftNode(rbsc.nodeNames[0], configFile);
            //this is the leader window
            //node.Show();

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = System.Reflection.Assembly.GetEntryAssembly().Location,
                WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location),
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Normal
            };

            for (int i = 0; i < rbsc.nodeNames.Count; i++)
            {
                startInfo.Arguments = string.Format("{0} {1} {2}", rbsc.nodeNames[i], CONFIG_FILE, string.Format("{0}-debug.log", rbsc.nodeNames[i]));
                Process.Start(startInfo);

                /// seems to perform better with this sleep on all Process.Start() calls. 
                /// This value increases to 1750ms when we re enable the JoinCluster call 
                /// within RaftNode, however still volatile and doesn't open window 100% 
                /// of the time
                Thread.Sleep(500);
            }

            Close();
        }
        private void cCreateRaftNodes_Click(object sender, EventArgs e)
        {
            ///Create config file, and save it, then start people
            ///Information for config file is represented by information in GUI
            int maxNodes = (int)nNodes.Value;

            //create a config file structure
            RaftBootstrapConfig rbsc = new RaftBootstrapConfig
            {
                clusterName = tbClusterName.Text,
                clusterPassword = tbClusterPasswd.Text,//should this really be plain text!
                maxNodes = maxNodes//set max nodes, generic for all
            };

            foreach (var peer in cluster_peer_list)
            {
                rbsc.nodeNames.Add(peer.Item1);
                rbsc.nodeIPAddresses.Add(peer.Item2);
                rbsc.nodePorts.Add(peer.Item3);
            }

            // serialize the config object
            string json = JsonConvert.SerializeObject(rbsc, Formatting.Indented);

            //create new config file 
            File.Delete(CONFIG_FILE);
            File.WriteAllText(CONFIG_FILE, json);


            if (cbInstantiate.Checked)
            {
                //create array of RaftNode
                RaftNode[] nodes = new RaftNode[maxNodes];

                for (int i = 0; i < rbsc.nodeNames.Count; i++)
                {
                    nodes[i] = new RaftNode(rbsc.nodeNames[i], CONFIG_FILE, LOGFILE);
                    nodes[i].Show();
                    Thread.Sleep(500);
                }

                this.Enabled = false;
                //Hide();
            }
            else
            {
                //create default start info for the process
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = System.Reflection.Assembly.GetEntryAssembly().Location,
                    WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location),
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                //start up node1 first so that it can become leader
                startInfo.Arguments = string.Format("{0} {1} {2}", rbsc.nodeNames[0], CONFIG_FILE, string.Format("{0}-debug.log", rbsc.nodeNames[0]));
                Process.Start(startInfo);
                //sleep to give head start for setting it self up
                Thread.Sleep(200);

                for (int i = rbsc.nodeNames.Count - 1; i > 0; i--)
                {
                    //start up the rest of the nodes
                    startInfo.Arguments = string.Format("{0} {1} {2}", rbsc.nodeNames[i], CONFIG_FILE, string.Format("{0}-debug.log", rbsc.nodeNames[i]));
                    Process.Start(startInfo);
                }

                Close();
            }
        }
        #endregion
    }
}