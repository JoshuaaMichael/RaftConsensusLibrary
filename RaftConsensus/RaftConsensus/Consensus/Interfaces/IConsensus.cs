using System;
using System.Net;
using TeamDecided.RaftConsensus.Consensus.Enums;
using System.Threading.Tasks;

namespace TeamDecided.RaftConsensus.Consensus.Interfaces
{
    public interface IConsensus<TKey, TValue> : IDisposable where TKey : ICloneable where TValue : ICloneable
    {
        Task<EJoinClusterResponse> JoinCluster(string clusterName, int maxNodes, int attempts);
        Task<EJoinClusterResponse> JoinCluster(string clusterName, string clusterPassword, int maxNodes, int attempts);
        void ManualAddPeer(string name, IPEndPoint endPoint);
        void EnablePersistentStorage(string filename);

        string ClusterName { get; }
        int JoiningClusterTimeout { get; }
        string NodeName { get; }

        bool DoesEntryValueExist(TKey key);
        TValue ReadEntryValue(TKey key);
        TValue[] ReadEntryValueHistory(TKey key);
        Task<ERaftAppendEntryState> AppendEntry(TKey key, TValue value);
        event EventHandler<Tuple<TKey, TValue>> OnNewCommitedEntry;
        int NumberOfCommits();
        Tuple<TKey, TValue> GetEntryByCommitIndex(int commitIndex);

        bool HasJoinedCluster();
        bool IsUASRunning();

        event EventHandler OnStartUAS;
        event EventHandler<EStopUasReason> OnStopUAS;
    }
}
