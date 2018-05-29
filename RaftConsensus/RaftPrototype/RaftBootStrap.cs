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

        private string LOGFILE = Path.Combine(Environment.CurrentDirectory, "debug.log");
        private string configFile = "./config.json";

        List<Tuple<string, string, int>> config = new List<Tuple<string, string, int>>();

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

            SetNodeCountSelector();// setup node numeric up down UI

            CreateStatusBar();// setup status bar

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

        private void Button1_Click(object sender, EventArgs e)
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

        private void CreateStatusBar()

        {
            // Set first panel properties and add to StatusBar
            //statusPanel.BorderStyle = StatusBarPanelBorderStyle.Sunken;
            statusPanel.BorderStyle = StatusBarPanelBorderStyle.Raised;
            //statusPanel.Text = "";//"Application started. No action yet"
            //statusPanel.ToolTipText = "Last Activity";
            statusPanel.AutoSize = StatusBarPanelAutoSize.Spring;
            mainStatusBar.Panels.Add(statusPanel);

            // Set second panel properties and add to StatusBar
            datetimePanel.BorderStyle = StatusBarPanelBorderStyle.Raised;
            datetimePanel.ToolTipText = "DateTime: " + System.DateTime.Today.ToString();
            datetimePanel.Text = System.DateTime.Today.ToLongDateString();
            datetimePanel.AutoSize = StatusBarPanelAutoSize.Contents;
            datetimePanel.Alignment = HorizontalAlignment.Right;

            mainStatusBar.Panels.Add(datetimePanel);
            mainStatusBar.ShowPanels = true;

            // Add StatusBar to Form controls
            this.Controls.Add(mainStatusBar);
        }

        private void CreateGridView()
        {
            // temporary datasource
            config = new List<Tuple<string, string, int>>();
            int maxNodes = (int) nNodes.Value;

            for (int i = 0; i< maxNodes; i++)
            {
                string nodeName = string.Format("Node{0}", i);
                string nodeIP = IP_TO_BIND;
                int nodePort = int.Parse(tbPort.Text) + i;

                Tuple<string, string, int> temp = new Tuple<string, string, int> (nodeName, nodeIP, nodePort);
                config.Add(temp);
            }

            nodeConfigDataView.DataSource = config;
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

            //Create a config file structure
            RaftBootstrapConfig rbsc = new RaftBootstrapConfig
            {
                clusterName = tbClusterName.Text,
                clusterPassword = tbClusterPasswd.Text,//should this really be plain text!
                //leaderIP = IP_TO_BIND,
                maxNodes = maxNodes//set max nodes, generic for all
            };

            foreach (var node in config)
            {
                rbsc.nodeNames.Add(node.Item1);
                rbsc.nodeIPAddresses.Add(node.Item2);
                rbsc.nodePorts.Add(node.Item3);
            }

            string json = JsonConvert.SerializeObject(rbsc, Formatting.Indented);

            File.Delete(configFile);
            File.WriteAllText(configFile, json);
            RaftLogging.Instance.Info("Created configuration file {0}", configFile);
            ////The commented out code below is for testing RaftNode with debug
            //RaftNode node = new RaftNode(rbsc.nodeNames[0], configFile);
            //this is the leader window
            //node.Show();

            for (int i = 0; i < rbsc.nodeNames.Count; i++)
            {
                //Let's start the leader with a 500ms head start (sleep) before starting the rest
                if (i == 1)
                {
                    Thread.Sleep(1000);
                }
                /// seems to perform better with this sleep on all Process.Start() calls. 
                /// This value increases to 1750ms when we re enable the JoinCluster call 
                /// within RaftNode, however still volatile  and doesn't open window 100% 
                /// of the time
                Process.Start(System.Reflection.Assembly.GetEntryAssembly().Location, string.Format("{0} {1}", rbsc.nodeNames[i], configFile));
            }

            Close();
        }

        private void CreateRaftNodes_WithStartInfo_Click(object sender, EventArgs e)
        {

        }

        private void CreateRaftNodes_WithInstantiate_Click(object sender, EventArgs e)
        {
            ///Create config file, and save it, then start people
            ///Information for config file is represented by information in GUI

            int maxNodes = (int)nNodes.Value;

            //Create a config file structure
            RaftBootstrapConfig rbsc = new RaftBootstrapConfig
            {
                clusterName = tbClusterName.Text,
                clusterPassword = tbClusterPasswd.Text,//should this really be plain text!
                //leaderIP = IP_TO_BIND,
                maxNodes = maxNodes//set max nodes, generic for all
            };

            foreach (var item in config)
            {
                rbsc.nodeNames.Add(item.Item1);
                rbsc.nodeIPAddresses.Add(item.Item2);
                rbsc.nodePorts.Add(item.Item3);
            }

            string json = JsonConvert.SerializeObject(rbsc, Formatting.Indented);

            File.Delete(configFile);
            File.WriteAllText(configFile, json);
            RaftLogging.Instance.Info("Created configuration file {0}", configFile);

            ////The commented out code below is for testing RaftNode with debug
            RaftNode[] nodes = new RaftNode[maxNodes];
            RaftNode node = new RaftNode(rbsc.nodeNames[0], configFile, string.Format("{0}-debug.log", rbsc.nodeNames[0]));
            //this is the leader window
            node.Show();

            for (int i = 1; i < rbsc.nodeNames.Count; i++)
            {
                //Let's start the leader with a 500ms head start (sleep) before starting the rest
                if (i == 1)
                {
                    Thread.Sleep(1000);
                }
                /// seems to perform better with this sleep on all Process.Start() calls. 
                /// This value increases to 1750ms when we re enable the JoinCluster call 
                /// within RaftNode, however still volatile  and doesn't open window 100% 
                /// of the time
                nodes[i] = new RaftNode(rbsc.nodeNames[i], configFile, string.Format("{0}-debug.log", rbsc.nodeNames[i]));
                nodes[i].Show();
                //Process.Start(System.Reflection.Assembly.GetEntryAssembly().Location, string.Format("{0} {1}", rbsc.nodeNames[i], configFile));
            }

            Hide();
        }
    }
}