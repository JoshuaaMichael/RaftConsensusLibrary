# RaftConsensus for C# (.NET Standard)

# Project Overview  
This project is a fully featured library implementation of the [Raft Consensus algorithm](https://raft.github.io) ([from the Paper](https://raft.github.io/raft.pdf)); the package written in  
C# and published as open source, under Apache2 license works as drop-in style library to allow developers to add high  
availability, clustering, fault-tolerance, consensus services to their application seamlessly.  
  
***

## Installation


The library is available for download through NuGet simply import the TeamDecided.RaftConsensus package into your project. 
```
		Install-Package TeamDecided.RaftConsensus  
```
***
## Quick Start

Each node needs:  

- It's own name  
- The name of the cluster it will be joining  
- The password to connect to the Cluster (only required when using encryption)  
- A port to bind to on the local computer  
- The number of total nodes in the cluster
- The name, IP address and port of the other nodes in the cluster
- Maximum number of join retrys attemps  

The below code example uses the following node information  

Name  | IP Address | Port |    |   
:-----|:------------|:----:|---|---
node1 | 127.0.0.1     | 3030 |  |    
node2 | 192.168.10.7  | 3030 |  |   
node3 | 192.168.10.25 | 3030 |  |   

### Code Example  
  
  
```csharp
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TeamDecided.RaftConsensus.Consensus;
using TeamDecided.RaftConsensus.Consensus.Enums;
using TeamDecided.RaftConsensus.Consensus.Interfaces;

namespace RaftConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
			const string nodeName = "Node1";
			const string clusterName = "ClusterName";
			const string clusterPassword = "ClusterPassword";
            const int localPort = 3030;
            const int numberOfNodes = 3;
            const int maxJoinRetry = 1;

            IConsensus<string, string> node = new RaftConsensus<string, string>(nodeName, localPort);
            node.EnablePersistentStorage(@"D:\RaftPrototype\raftconsensus.db");

            node.OnNewCommitedEntry += Node_OnNewCommitedEntry;
            node.OnStartUAS += Node_OnStartUAS;
            node.OnStopUAS += Node_OnStopUAS;

            //Configure peer IP details for each node
            node.ManualAddPeer("Node2", new IPEndPoint(IPAddress.Parse("192.168.10.7"), 3030));
            node.ManualAddPeer("Node3", new IPEndPoint(IPAddress.Parse("192.168.10.25"), 3030));

            Console.WriteLine("Trying to join cluster. Contacting other nodes...");
            Task<EJoinClusterResponse> joinClusterResponses =
                node.JoinCluster(clusterName, clusterPassword, numberOfNodes, maxJoinRetry);

            joinClusterResponses.Wait();

            if (joinClusterResponses.Result == EJoinClusterResponse.NoResponse)
            {
                Console.WriteLine("Please ensure Node2 and Node3 are started, and their IP details configured for each node");
                Console.WriteLine("exiting...");
                Console.ReadKey();
                return;
            }

            while (true)
            {
                if (node.IsUASRunning())
                {
                    //The is pretend work of a UAS
                    string key = "Node1 says";
                    string value = "hello world";
                    Task<ERaftAppendEntryState> append = node.AppendEntry(key, value); //Send a new append message
                    append.Wait();
                }

                //OnStartUAS and OnStopUAS events are provided instead of sleeping
                //Due to constaints of keeping example simple, we're avoiding using ManualResetEvents
                Thread.Sleep(1000);
            }

        }

        private static void Node_OnStartUAS(object sender, EventArgs e)
        {
            Console.WriteLine("I'm in command of the cluster, time to start my UAS");
        }

        private static void Node_OnStopUAS(object sender, EStopUasReason e)
        {
            Console.WriteLine("I've lost command of the cluster, time to stop my UAS ");
        }

        private static void Node_OnNewCommitedEntry(object sender, Tuple<string, string> e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}
```
  
***
## Detailed Code  

After adding the NuGet package we need to import the required namespaces:
```csharp
using TeamDecided.RaftConsensus.Consensus;
using TeamDecided.RaftConsensus.Consensus.Enums;
using TeamDecided.RaftConsensus.Consensus.Interfaces;
```
We now need to set up the basic node information for the local node:
```csharp
	const string nodeName = "Node1";					//own node name
	const string clusterName = "ClusterName";			//name of the cluster to join
	const string clusterPassword = "ClusterPassword";	//password to enable encryption
	const int startPort = 3030;        					//port to listen on locally
	const int numberOfNodes = 3;       					//total number of nodes in cluster
	const int maxJoinRetry = 3;        					//number of times to retry join cluster if failure occurs
```
Next we instantiate our local node:
```csharp
	IConsensus<string, string> node = new RaftConsensus<string, string>(nodeName, localPort);
```
Once instantiated we can now register a local event handler to the nodes OnNewCommitedEntry, OnStartUAS and OnStopUAS  
events. These events correspond to Use Case items **Start UAS, Stop UAS, Receive Commit Entries**:  
```csharp
	node.OnNewCommitedEntry += Node_OnNewCommitedEntry;
	node.OnStartUAS += Node_OnStartUAS;
	node.OnStopUAS += Node_OnStopUAS;
```
At this stage we are ready to add cluster member information to the node:
```csharp
	node.ManualAddPeer("Node2", new IPEndPoint(IPAddress.Parse("192.168.10.7"), 3030));
	node.ManualAddPeer("Node3", new IPEndPoint(IPAddress.Parse("192.168.10.25"), 3030));
```
Alternatively, we could for demonstrative purposes fire up multiple nodes locally:
```csharp
	IConsensus<string, string>[] nodes = new RaftConsensus<string, string>[numberOfNodes];
	
	for (int i = 0 ; i < numberOfNodes; i++)
	{
		//as all nodes in this example would be running on the same machine we 
		//must assign each its own port number to listen on.
		nodes[i] = new RaftConsensus<string, string>("Node" + (i + 1), 3030 + i);
		
		for (int j = 0 ; j < numberOfNodes; j++)
		{
			if (j == i)
			{
				continue;
			}
			IPEndPoint ipEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3030+j);
			_node.ManualAddPeer("Node"+(j+1), ipEndpoint);
		}
	}
```
Our node is now ready to attempt to **Join cluster**. In order to successfully join the cluster each member must be  
available and in the same attempting state, otherwise an attempt timeout will occur:
```csharp
	Task<EJoinClusterResponse> joinClusterResponses =
			node.JoinCluster("TheClusterName", "TheClusterPassword", numberOfNodes, maxJoinRetry);
	joinClusterResponses.Wait();

	//we can now check the result of our attempt to join the cluster
	if (joinClusterResponses.Result == EJoinClusterResponse.NoResponse)
	{
		Console.WriteLine("Please ensure Node2 and Node3 are started, and their IP details configured for each node");
		Console.ReadKey();
		return;
	}
```
After successfully joining cluster one of the nodes will be elected as leader. This leader node will be able to send  
**Append entry** messages which contain the generic key value data pairs to the cluster. After consensus is reached with  
majority of cluster members these messages will be commited to the consensus log:
```csharp
	Task<ERaftAppendEntryState> append = node.AppendEntry(key, value); //Send a new append message
	append.Wait();
```

***

## Business Scenario
The consensus library will be integrated into the User Application Service, and will be used to logically tie multiple  
User Application Servers (UAS) together into a Cluster of Application Services (CAS). The image below further depicts the  
scenario of a dedicated linked servers which provide services for clients, there are multiple distinct servers which run  
the UASs forming a CAS. The User Application Clients communicate with the CAS through simple IP failover style.  

![Example2](https://cdn.discordapp.com/attachments/471607717056741387/485346818427846656/unknown.png)  

***

## Under the Code
The library is accessed through the public API depicted in the below.  
  
![Consensus API](https://cdn.discordapp.com/attachments/471607717056741387/485351787768053760/unknown.png)

The nodes are always in one of the following three states:

![Consensus API1](https://cdn.discordapp.com/attachments/471607717056741387/485400702399807498/ConsensusNodeState300s.jpg)

***