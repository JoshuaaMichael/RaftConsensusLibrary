using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TeamDecided.RaftConsensus;
using TeamDecided.RaftConsensus.Interfaces;

namespace RaftPrototype
{
    public partial class RaftForm : Form
    {
        private const int MAXIMUM_NODES = 9;
        private const int MINIMUM_NODES = 3;
        private const int DEFAULT_NODES = 5;
        private const int START_PORT = 5555;

        private const string MAX_NODES_WARNING = "Maximum nine (9) nodes supported in prototype";
        private const string MIN_NODES_WARNING = "Consensus requires minimum three (3) nodes";
        private const string CLUSTER_NAME = "Prototype Cluster";
        private const string CLUSTER_PASSWD = "password";
        private const string IP_TO_BIND = "127.0.0.1";

        protected StatusBar mainStatusBar = new StatusBar();
        protected StatusBarPanel statusPanel = new StatusBarPanel();
        protected StatusBarPanel datetimePanel = new StatusBarPanel();

        private IConsensus<string, string>[] nodes;

        public RaftForm()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            nodes = RaftConsensus<string, string>.MakeNodesForTest(5, START_PORT);

            tbClusterName.Text = CLUSTER_NAME;
            //tbClusterPassword = CLUSTER_PASSWD;

            //leader = new RaftConsensus<string, string>("Leader", 5555);
            //leader.CreateCluster(CLUSTER_NAME, CLUSTER_PASSWD, 5);
            // setup node numeric up down UI
            SetNodeCountSelector();
            // setup status bar
            CreateStatusBar();
        }

        private void PopulateNodeInformation(RaftConsensus<string, string>[] nodes)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                for (int j = 0; j < nodes.Length; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    nodes[i].ManualAddPeer(nodes[j].GetNodeName(), new IPEndPoint(IPAddress.Parse(IP_TO_BIND), START_PORT + j));
                }
            }
        }
        
        private void SetNodeCountSelector()
        {
            // Set the Minimum, Maximum, and initial Value.
            numericUpDownNodes.Value = DEFAULT_NODES;
            numericUpDownNodes.Maximum = MAXIMUM_NODES;
            numericUpDownNodes.Minimum = MINIMUM_NODES;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void numericUpDownNodes_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDownNodes.Value == 9)
            {
                lWarningNodesNumber.ForeColor = Color.Red;
                lWarningNodesNumber.Visible = true;
                statusPanel.Text = MAX_NODES_WARNING;
                lWarningNodesNumber.Text= MAX_NODES_WARNING;
            }
            else if (numericUpDownNodes.Value == 3)
            {
                lWarningNodesNumber.ForeColor = Color.Red;
                lWarningNodesNumber.Visible = true;
                statusPanel.Text = MIN_NODES_WARNING;
                lWarningNodesNumber.Text = MIN_NODES_WARNING;
            }
            else
            {
                lWarningNodesNumber.Visible = false;
                statusPanel.Text = "";

            }
        }

        private void CreateStatusBar()

        {
            // Set first panel properties and add to StatusBar
            statusPanel.BorderStyle = StatusBarPanelBorderStyle.Sunken;
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

    }
}
