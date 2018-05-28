using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using TeamDecided.RaftConsensus;
using TeamDecided.RaftConsensus.Enums;
using TeamDecided.RaftConsensus.Interfaces;

namespace RaftPrototype
{
    public partial class RaftNode : Form
    {
        private IConsensus<string, string> node;

        public RaftNode(string serverName, string configFile)
        {
            InitializeComponent();
            Initialize(serverName, configFile);
        }

        private void Initialize(string serverName, string configFile)
        {
            //TODO: This is where we need to get the current IConsensus log
            lbNodeName.Text = serverName;
            LoadConfig(serverName, configFile);

        }

        public void LoadConfig(string serverName, string configFile)
        {
            string json = File.ReadAllText(configFile);
            RaftBootstrapConfig config = JsonConvert.DeserializeObject<RaftBootstrapConfig>(json);

            //Get the node id from the node name string
            int id = int.Parse(serverName.Substring(serverName.Length - 1));

            node = new RaftConsensus<string, string>(config.nodeNames[id], config.nodePorts[id]);

            //populate the peer information
            AddPeers(config, id);

            //always making the first entry the cluster manager (Leader)
            if (config.nodeNames[0] == serverName)
            {
                //create cluster
                node.CreateCluster(config.clusterName, config.clusterPassword, config.maxNodes);
            }
            else
            {
                //join cluster
                node.JoinCluster(config.clusterName, config.clusterPassword, config.maxNodes);
                //Task<EJoinClusterResponse> joinTask = node.JoinCluster(config.clusterName, config.clusterPassword, config.maxNodes);
                //joinTask.Wait();
            }

            //The event that is for start/stop UAS
            //Subsribe to it, and have that method update the UI to disable the text entry feild (and update "I am leader")

            //Read out the IP address of everyone else
            //Read out if you are the leader
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
                //Console.WriteLine(string.Format("{0} adding {1} to peers", config.nodeNames[id], config.nodeNames[i]));
            }
            Console.WriteLine("finished adding peers to {0}", config.nodeNames[id]);
        }


        private void bSendMsg_Click(object sender, EventArgs e)
        {
            //TODO: This is where we'll send a message using IConsensus member.

        }
    }
}