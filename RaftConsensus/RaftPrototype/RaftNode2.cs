using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TeamDecided.RaftCommon.Logging;
using TeamDecided.RaftConsensus;
using TeamDecided.RaftConsensus.Enums;
using TeamDecided.RaftConsensus.Interfaces;

namespace RaftPrototype
{
    public partial class RaftNode2 : Form
    {
        private IConsensus<string, string> node;
        private StringBuilder debugLog = new StringBuilder();
        private SynchronizationContext mainThread;

        private string servername;
        private string serverip;
        private int serverport;
        private string configurationFile;
        private string logfile;
        private bool wasStopped = false;
        public RaftNode2(string serverName, string configFile, string logFile)
        {
            //set local attributes
            this.servername = serverName;
            this.configurationFile = configFile;
            this.logfile = logFile;

            mainThread = SynchronizationContext.Current;
            if (mainThread == null) { mainThread = new SynchronizationContext(); }
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            //TODO: This is where we need to get the current IConsensus log
            this.Text = string.Format("{0} - {1}", this.Text, servername);
            
            SetupDebug(logfile);

            //run the configuration setup on background thread stop GUI from blocking
            Task task = new TaskFactory().StartNew(new Action<object>((test) =>
            {
                LoadConfig();
            }), TaskCreationOptions.None);
        }

        public void LoadConfig()
        {
            try
            {
                string json = File.ReadAllText(configurationFile);
                RaftBootstrapConfig config = JsonConvert.DeserializeObject<RaftBootstrapConfig>(json);
                //Get the node id from the node name string
                int index = int.Parse(servername.Substring(servername.Length - 1)) - 1;
                //populate the peer information
                RaftLogging.Instance.Info("{0} is adding peers", config.nodeNames[index]);

                serverport = config.nodePorts[index];
                serverip = config.nodeIPAddresses[index];
                
                //always making the first entry the cluster manager (Leader)
                if (config.nodeNames[0] == servername)
                {
                    //create cluster
                    node = new RaftConsensus<string, string>(config.nodeNames[index], config.nodePorts[index]);
                    //AddPeers(config, index);
                    AddPeers(config);
                    node.CreateCluster(config.clusterName, config.clusterPassword, config.maxNodes);
                    RaftLogging.Instance.Info("Cluster created by {0}", config.nodeNames[0]);
                }
                else
                {
                    while (true)
                    {
                        node = new RaftConsensus<string, string>(config.nodeNames[index], config.nodePorts[index]);
                        AddPeers(config, index);
                        Task<EJoinClusterResponse> joinTask = node.JoinCluster(config.clusterName, config.clusterPassword, config.maxNodes);
                        joinTask.Wait();
                        EJoinClusterResponse result = joinTask.Result;
                        if (result == EJoinClusterResponse.ACCEPT)
                        {
                            break;
                        }
                        else
                        {
                            if (MessageBox.Show("Failed to join cluster, do you want to retry?", "Error " + servername, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                            {
                                node.Dispose();
                                continue;
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                    RaftLogging.Instance.Info("{0} joined Cluster ", config.nodeNames[index]);
                    node.StopUAS += HandleUASChange;
                    node.StartUAS += HandleUASStart;
                }

                //update the main UI
                mainThread.Send((object state) => {
                    UpdateNodeWindow();
                }, null);
            }
            catch (Exception e)
            {
                MessageBox.Show(servername + "\n" + e.ToString());
            }
        }

        private void HandleUASStart(object sender, EventArgs e)
        {
            mainThread.Send((object state) =>
            {
                UpdateNodeWindow();
            }, null);
            //UpdateNodeWindow();
            //LoadConfig();
        }

        private void HandleUASChange(object sender, EStopUASReason e)
        {
            UpdateNodeWindow();
        }

        private void UpdateNodeWindow()
        {
            bool isUas = node.IsUASRunning();
            lbNodeName.Text = servername;
            if ( isUas)
            {
                this.lbServerState.Text = "UAS running.";
                this.gbAppendEntry.Enabled = true;
                this.btStop.Enabled = true;
            }
            else
            {
                this.lbServerState.Text = "UAS not running.";
                this.gbAppendEntry.Enabled = false;
                this.btStop.Enabled = false;
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
            }
        }

        private void AddPeers(RaftBootstrapConfig config)
        {
            for (int i = 0; i < config.maxNodes; i++)
            {
                //Add the list of nodes into the PeerList
                if (string.Equals(config.nodeNames[i], servername))
                {
                    continue;
                }
                IPEndPoint ipEndpoint = new IPEndPoint(IPAddress.Parse(config.nodeIPAddresses[i]), config.nodePorts[i]);
                node.ManualAddPeer(config.nodeNames[i], ipEndpoint);
            }
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
            //RaftLogging.Instance.DeleteExistingLogFile();
            RaftLogging.Instance.SetDoInfo(true);
            RaftLogging.Instance.SetDoDebug(true);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            node.Dispose();
        }

        private void WatchLog(object sender, EventArgs e)
        {
            string log = string.Format("{0:G} | {1} ", DateTime.Now, e);
            debugLog.AppendLine(e.ToString());
            //tbDebugLog.Text = debugLog.ToString();
        }
        
        private void Stop_Click(object sender, EventArgs e)
        {
            if ( node.IsUASRunning() )
            {
                node.Dispose();
                wasStopped = true;
                UpdateNodeWindow();
                //run the configuration setup on background thread stop GUI from blocking
                Task task = new TaskFactory().StartNew(new Action<object>((test) =>
                {
                    LoadConfig();
                }), TaskCreationOptions.None);
            }
        }

        private void Start_Click(object sender, EventArgs e)
        {
            MessageBox.Show(string.Format("Start button pressed {0}", servername), "Trying to start UAS.", MessageBoxButtons.OK);

            //run the configuration setup on background thread stop GUI from blocking
            Task task = new TaskFactory().StartNew(new Action<object>((test) =>
            {
                LoadConfig();
            }), TaskCreationOptions.None);

            //if (!wasStopped)
            //{
            //    MessageBox.Show("Can't start from this state", "UAS Start called", MessageBoxButtons.OK);
            //    return;
            //}
            //string json = File.ReadAllText(configurationFile);
            //RaftBootstrapConfig config = JsonConvert.DeserializeObject<RaftBootstrapConfig>(json);
            //while (true)
            //{
            //    node = new RaftConsensus<string, string>(servername, serverport);
            //    AddPeers(config);
            //    Task<EJoinClusterResponse> joinTask = node.JoinCluster(config.clusterName, config.clusterPassword, config.maxNodes);
            //    joinTask.Wait();
            //    EJoinClusterResponse result = joinTask.Result;
            //    if (result == EJoinClusterResponse.ACCEPT)
            //    {
            //        break;
            //    }
            //    else
            //    {
            //        if (MessageBox.Show("Failed to join cluster, do you want to retry?", "Error " + servername, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
            //        {
            //            node.Dispose();
            //            continue;
            //        }
            //        else
            //        {
            //            return;
            //        }
            //    }
            //}

            //string json = File.ReadAllText(configurationFile);
            //RaftBootstrapConfig config = JsonConvert.DeserializeObject<RaftBootstrapConfig>(json);

            //if (!node.IsUASRunning())
            //{
            //    if (node == null)
            //    {
            //        while (true)
            //        {
            //            node = new RaftConsensus<string, string>(servername, serverport);
            //            AddPeers(config);
            //            Task<EJoinClusterResponse> joinTask = node.JoinCluster(config.clusterName, config.clusterPassword, config.maxNodes);
            //            joinTask.Wait();
            //            EJoinClusterResponse result = joinTask.Result;
            //            if (result == EJoinClusterResponse.ACCEPT)
            //            {
            //                break;
            //            }
            //            else
            //            {
            //                if (MessageBox.Show("Failed to join cluster, do you want to retry?", "Error " + servername, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
            //                {
            //                    node.Dispose();
            //                    continue;
            //                }
            //                else
            //                {
            //                    return;
            //                }
            //            }
            //        }
            //    }

            //    //run the configuration setup on background thread stop GUI from blocking
            //    Task task = new TaskFactory().StartNew(new Action<object>((test) =>
            //    {
            //        //LoadConfig(serverName, configFile);
            //    }), TaskCreationOptions.None);
            //    //UpdateNodeWindow();
                //}
            }


        }
    
}

