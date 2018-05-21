using System;
using System.Net;
using TeamDecided.RaftConsensus.Enums;
using System.Threading.Tasks;

namespace TeamDecided.RaftConsensus.Interfaces
{
    public interface IConsensus<TKey, TValue> : IDisposable where TKey : ICloneable where TValue : ICloneable
    {
        Task<EJoinClusterResponse> JoinCluster(string clusterName, string clusterPassword);
        void CreateCluster(string clusterName, string clusterPassword, int maxNodes);
        void ManualAddPeer(IPEndPoint endPoint);

        string GetClusterName();
        string GetNodeName();

        TValue ReadEntryValue(TKey key);
        TValue[] ReadEntryValueHistory(TKey key);
        Task<ERaftAppendEntryState> AppendEntry(TKey key, TValue value);

        event EventHandler StartUAS;
        event EventHandler<EStopUASReason> StopUAS;
        event EventHandler<Tuple<TKey, TValue>> OnNewLogEntry;
    }
}
