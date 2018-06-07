using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
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
        private SynchronizationContext mainThread;

        private List<Tuple<string, string>> log;
        private static object updateWindowLockObject = new object();

        private RaftBootstrapConfig config;
        private string servername;
        private string serverip;
        private int serverport;
        private int index;
        private string configurationFile;
        private string logfile;

        private static Mutex mutex = new Mutex();
        private bool onClosing;
        private bool isStopped;

        public RaftNode(string serverName, string configFile, string logFile)
        {
            //set local attributes
            servername = serverName;
            configurationFile = configFile;
            logfile = logFile;
            log = new List<Tuple<string, string>>();
            isStopped = false;

            mainThread = SynchronizationContext.Current;
            if (mainThread == null) { mainThread = new SynchronizationContext(); }


            onClosing = false;

            InitializeComponent();
            Initialize();
        }

        #region Setup Node

        private void Initialize()
        {
            Text = string.Format("{0} - {1}", this.Text, servername);
            btStart.Enabled = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            SetupLogging();
            LoadConfig();

            Task task = new TaskFactory().StartNew(new Action<object>((test) =>
            {
                StartNode();
            }), TaskCreationOptions.None);
        }

        public void LoadConfig()
        {
            string json = File.ReadAllText(configurationFile);
            config = JsonConvert.DeserializeObject<RaftBootstrapConfig>(json);
            index = int.Parse(servername.Substring(servername.Length - 1)) - 1;
            serverport = config.nodePorts[index];
            serverip = config.nodeIPAddresses[index];
            //RaftLogging.Instance.Info("{0} is adding peers", config.nodeNames[index]);
        }

        private void StartNode()
        {
            try
            {
                while (true)
                {
                    //Instantiate node and set up peer information
                    //subscribe to RaftLogging Log Info event
                    CreateNode();

                    //call the leader to join cluster
                    Task<EJoinClusterResponse> joinTask = node.JoinCluster(config.clusterName, config.clusterPassword, config.maxNodes);
                    joinTask.Wait();

                    //check the result of the attempt to join the cluster
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

                //update the main UI
                mainThread.Send((object state) =>
                {
                    lock (updateWindowLockObject)
                    {
                        UpdateNodeWindow();
                    }
                }, null);
            }
            catch (Exception e)
            {
                MessageBox.Show(servername + "\n" + e.ToString());
            }
        }

        private void CreateNode()
        {
            //Instantiate node
            node = new RaftConsensus<string, string>(config.nodeNames[index], config.nodePorts[index]);
            //Add peer to the node
            AddPeers(config, index);

            //Subscribe to the node UAS start/stop event
            node.StopUAS += HandleUASStop;
            node.StartUAS += HandleUASStart;
            node.OnNewCommitedEntry += HandleNewCommitEntry;
        }

        private void SetupLogging()
        {
            //string path = string.Format(@"{0}", Environment.CurrentDirectory);
            //string debug = Path.Combine(Environment.CurrentDirectory, "debug.log");
            //string debug = Path.Combine("C:\\Users\\Tori\\Downloads\\debug.log");

            RaftLogging.Instance.OverwriteLoggingFile(logfile);
            RaftLogging.Instance.EnableBuffer(50);
            RaftLogging.Instance.SetDoInfo(true);
            RaftLogging.Instance.SetDoDebug(true);

        }

        #endregion

        private void UpdateNodeWindow()
        {
            lbNodeName.Text = servername;
            if (node != null && node.IsUASRunning())
            {
                lbServerState.Text = "Leader";
                gbAppendEntry.Enabled = true;
                btStart.Enabled = false;
                btStop.Enabled = true;
            }
            else
            {
                if (isStopped)
                {
                    lbServerState.Text = "Offline";
                    btStart.Enabled = true;
                    btStop.Enabled = false;
                }
                else
                {
                    lbServerState.Text = "Follower";
                    btStart.Enabled = false;
                    btStop.Enabled = true;
                }

                gbAppendEntry.Enabled = false;
            }

            tbKey.Clear();
            tbValue.Clear();
            logDataGrid.DataSource = null;
            logDataGrid.DataSource = log;
            logDataGrid.Columns[0].HeaderText = "Key";
            logDataGrid.Columns[1].HeaderText = "Value";
        }

        #region event methods

        private void HandleUASStart(object sender, EventArgs e)
        {
            //this.log.Clear();
            mainThread.Post((object state) =>
            {
                UpdateNodeWindow();
            }, null);
        }

        private void HandleUASStop(object sender, EStopUASReason e)
        {
            mainThread.Post((object state) =>
            {
                UpdateNodeWindow();
            }, null);
        }

        private void HandleInfoLogUpdate(object sender, string e)
        {
            try
            {
                if (mutex.WaitOne())
                {
                    if (!onClosing)
                    { 
                        mainThread.Post((object state) =>
                        {
                            if (CheckLogEntry(e))
                            {
                                try
                                {
                                    tbLog.AppendText(e);
                                }
                                catch
                                {
                                    Console.WriteLine("bad shit happened");
                                }
                            }
                        }, null);
                    }
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        private void HandleNewCommitEntry(object sender, Tuple<string, string> e)
        {
            string n = servername;
            log.Add(e);
            mainThread.Post((object state) =>
            {
                UpdateNodeWindow();
            }, null);
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            this.isStopped = true;

            node.Dispose();
            lock (updateWindowLockObject)
            {
                UpdateNodeWindow();
            }
        }

        private void Start_Click(object sender, EventArgs e)
        {
            isStopped = false;
            log.Clear();

            //run the configuration setup on background thread stop GUI from blocking
            Task task = new TaskFactory().StartNew(new Action<object>((test) =>
            {
                StartNode();
            }), TaskCreationOptions.None);
        }

        private void AppendMessage_Click(object sender, EventArgs e)
        {
            try
            {
                Task<ERaftAppendEntryState> append = node.AppendEntry(tbKey.Text, tbValue.Text);
                tbKey.Clear();
                tbValue.Clear();
            }
            catch (InvalidOperationException ex)
            {
                RaftLogging.Instance.Debug(string.Format("{0} {1}", servername, ex.ToString()));
            }
        }

        private void Debug_CheckedChanged(object sender, EventArgs e)
        {
            if (cbDebug.Checked)
            {
                RaftLogging.Instance.OnNewLineInfo += HandleInfoLogUpdate;
            }
            else
            {
                RaftLogging.Instance.OnNewLineInfo -= HandleInfoLogUpdate;
            }
        }

        #endregion

        #region utilities

        private bool CheckLogEntry(string logEntryLine)
        {
            if ( logEntryLine.IndexOf(servername) == 15)
            {
                return true;
            }
            return false;
        }

        #endregion

        #region consensus stuff

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
        
        #endregion

        #region closing

        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                mutex.WaitOne();
                onClosing = true;
                RaftLogging.Instance.OnNewLineInfo -= HandleInfoLogUpdate;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
            base.OnClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            node.Dispose();
            base.OnFormClosed(e);
        }

        #endregion
    }
}

