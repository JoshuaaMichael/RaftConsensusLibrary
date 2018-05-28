using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using TeamDecided.RaftConsensus;
using TeamDecided.RaftConsensus.Interfaces;

namespace RaftPrototype
{
    public partial class RaftNode : Form
    {
        //List<KeyValuePair<string, string>> log = new List<KeyValuePair<string, string>>();
        IConsensus<string, string> node;


        public RaftNode(string serverName, string configFile)
        {
            InitializeComponent();
            Initialize(serverName, configFile);
        }

        private void Initialize(string serverName, string configFile)
        {
            //TODO: This is where we need to get the current IConsensus log

            LoadConfig(serverName, configFile);

            //// this is a temp data store
            //RaftData data = new RaftData();
            ////adding a dictionary content as a list of KeyValuePairs
            //log.AddRange(data.Data());
            //// setting the datagrid source
            //dataGridView1.DataSource = log;
        }

        public void LoadConfig(string serverName, string configFile)
        {
            string json = File.ReadAllText(configFile);
            RaftBootstrapConfig config = JsonConvert.DeserializeObject<RaftBootstrapConfig>(json);

            if(config.nodeNames[0] == serverName)
            {
                //nodes = RaftConsensus<string, string>.MakeNodesForTest(config.maxNodes, config.nodePorts[0]);
                node = new RaftConsensus<string, string>(config.nodeNames[0], config.nodePorts[0]);

                for (int i = 1; i < config.maxNodes; i++)
                {
                    IPEndPoint ipEndpoint = new IPEndPoint(IPAddress.Parse(config.nodeIPAddresses[i]), config.nodePorts[i]);
                    //Add the list of nodes into the PeerList
                    node.ManualAddPeer(config.nodeNames[i], ipEndpoint);
                }

                //create cluster
                node.CreateCluster(config.clusterName, config.clusterPassword, config.maxNodes);

            }
            else
            {
                int id = int.Parse(serverName.Substring(serverName.Length - 2)) - 1;
                node = new RaftConsensus<string, string>(config.nodeNames[id], config.nodePorts[id]);

                for (int i = 0; i < config.maxNodes; i++)
                {
                    IPEndPoint ipEndpoint = new IPEndPoint(IPAddress.Parse(config.nodeIPAddresses[i]), config.nodePorts[i]);
                    //Add the list of nodes into the PeerList
                    if (i != id)
                    {
                        node.ManualAddPeer(config.nodeNames[i], ipEndpoint);
                    }
                }


                //join cluster
                node.JoinCluster(config.clusterName, config.clusterPassword, config.maxNodes);
            }

            //The event that is for start/stop UAS
            //Subsribe to it, and have that method update the UI to disable the text entry feild (and update "I am leader")

            //Read out the IP address of everyone else
            //Read out if you are the leader
        }

        private void bSendMsg_Click(object sender, EventArgs e)
        {
            //TODO: This is where we'll send a message using IConsensus member.

            //KeyValuePair<string, string> temp = new KeyValuePair<string, string>("new", DateTime.Now.Ticks.ToString());
            //log.Add(temp);
            
            //// clear data source
            //dataGridView1.DataSource = null;
            //// setting the datagrid source
            //dataGridView1.DataSource = log;
        }
    }
}
        //public void SetLog(IConsensus<string, string> log)
        //{
        //    this.distributedLog = log;
        //}
