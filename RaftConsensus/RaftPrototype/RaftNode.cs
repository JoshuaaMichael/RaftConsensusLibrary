using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TeamDecided.RaftCommon.Logging;
using TeamDecided.RaftConsensus;
using TeamDecided.RaftConsensus.Enums;
using TeamDecided.RaftConsensus.Interfaces;

namespace RaftPrototype
{
    public partial class RaftNode : Form
    {
        private IConsensus<string, string> node;
        private StringBuilder debugLog = new StringBuilder();

        public RaftNode(string serverName, string configFile, string logFile)
        {
            InitializeComponent();
            Initialize(serverName, configFile, logFile);
        }

        private void Initialize(string serverName, string configFile, string logFile)
        {
            //TODO: This is where we need to get the current IConsensus log
            this.Text = string.Format("{0} - {1}", this.Text, serverName);
            lbNodeName.Text = serverName;

            if (serverName == "Node0")
            {
                //RaftLogging.Instance.LogEntryEvent += WatchLog;
            }

            SetupDebug(logFile);
            LoadConfig(serverName, configFile);

        }

        private void WatchLog(object sender, EventArgs e)
        {
            string log = string.Format("{0:G} | {1} ", DateTime.Now, e);
            debugLog.AppendLine(e.ToString());
            tbDebugLog.Text = debugLog.ToString();
            
        }

        public void LoadConfig(string serverName, string configFile)
        {
            try
            {
                string json = File.ReadAllText(configFile);
                RaftBootstrapConfig config = JsonConvert.DeserializeObject<RaftBootstrapConfig>(json);

                //Get the node id from the node name string
                int index = int.Parse(serverName.Substring(serverName.Length - 1)) - 1;

                //always making the first entry the cluster manager (Leader)
                if (config.nodeNames[0] == serverName)
                {
                    //create cluster
                    node = new RaftConsensus<string, string>(config.nodeNames[index], config.nodePorts[index]);
                    RaftLogging.Instance.Info("{0} is adding peers", config.nodeNames[index]);
                    AddPeers(config, index);
                    node.CreateCluster(config.clusterName, config.clusterPassword, config.maxNodes);
                    RaftLogging.Instance.Info("Cluster created by {0}", config.nodeNames[0]);
                }
                else
                {
                    while (true)
                    {
                        node = new RaftConsensus<string, string>(config.nodeNames[index], config.nodePorts[index]);
                        AddPeers(config, index);
                        RaftLogging.Instance.Info("{0} is adding peers", config.nodeNames[index]);
                        Task<EJoinClusterResponse> joinTask = node.JoinCluster(config.clusterName, config.clusterPassword, config.maxNodes);
                        joinTask.Wait();
                        EJoinClusterResponse result = joinTask.Result;
                        if (result == EJoinClusterResponse.ACCEPT)
                        {
                            break;
                        }
                        else
                        {
                            if (MessageBox.Show("Failed to join cluster, do you want to retry?", "Error " + serverName, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                            {
                                node.Dispose();
                                continue;
                            }
                            else
                            {
                                Close();
                                return;
                            }
                        }
                    }
                    RaftLogging.Instance.Info("{0} joined Cluster ", config.nodeNames[index]);
                }

                //The event that is for start/stop UAS
                //Subsribe to it, and have that method update the UI to disable the text entry feild (and update "I am leader")

                //Read out the IP address of everyone else
                //Read out if you are the leader
            }
            catch(Exception e)
            {
                if (node != null)
                {
                    node.Dispose();
                }
                MessageBox.Show(e.ToString());
            }
        }

        private void AddPeers(RaftBootstrapConfig config, int id)
        {
            for (int i = 0; i < config.maxNodes; i++)
            {
                //Add the list of nodes into the PeerList
                if (i == id)
                {
                    continue;
                }
                IPEndPoint ipEndpoint = new IPEndPoint(IPAddress.Parse(config.nodeIPAddresses[i]), config.nodePorts[i]);
                node.ManualAddPeer(config.nodeNames[i], ipEndpoint);
                //RaftLogging.Instance.Info("{0} added {1} to peer list", config.nodeNames[id], config.nodeNames[i]);
                //Console.WriteLine(string.Format("{0} adding {1} to peers", config.nodeNames[id], config.nodeNames[i]));
            }
            //Console.WriteLine("finished adding peers to {0}", config.nodeNames[id]);
        }

        private void SendMsg_Click(object sender, EventArgs e)
        {
            //TODO: This is where we'll send a message using IConsensus member.

        }

        private void SetupDebug(string logFile)
        {
            //string path = string.Format(@"{0}", Environment.CurrentDirectory);
            //string debug = Path.Combine(Environment.CurrentDirectory, "debug.log");
            //string debug = Path.Combine("C:\\Users\\Tori\\Downloads\\debug.log");

            RaftLogging.Instance.OverwriteLoggingFile(logFile);
            RaftLogging.Instance.DeleteExistingLogFile();
            RaftLogging.Instance.SetDoInfo(true);
            RaftLogging.Instance.SetDoDebug(true);
        }
    }
}