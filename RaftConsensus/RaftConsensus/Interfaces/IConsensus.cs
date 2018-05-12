using System;
using TeamDecided.RaftConsensus.Enums;
using TeamDecided.RaftNetworking.Interfaces;

namespace TeamDecided.RaftConsensus.Interfaces
{
    public interface IConsensus<TKey, TValue> where TKey : ICloneable where TValue : ICloneable
    {
        Action<EJoinClusterResponse> JoinCluster(string clusterName, string clusterPassword, string clientName, IUDPNetworking networking);
        void CreateCluster(string clusterName, string clusterPassword, string clientName, int maxNodes);
        void StopCluster();

        string GetClusterName();

        TValue ReadEntryValue(TKey key);
        TValue[] ReadEntryValueHistory(TKey key);
        void AppendEntry(TKey key, TValue value, Action<ERaftAppendEntryState> callback);

        event EventHandler StartUAS;
        event EventHandler<EStopUASReason> StopUAS;
        event EventHandler<Tuple<TKey, TValue>> OnNewLogEntry;
    }
}
