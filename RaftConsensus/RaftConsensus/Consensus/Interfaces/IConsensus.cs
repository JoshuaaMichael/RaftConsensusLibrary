﻿using System;
using System.Net;
using TeamDecided.RaftConsensus.Consensus.Enums;
using System.Threading.Tasks;

namespace TeamDecided.RaftConsensus.Consensus.Interfaces
{
    public interface IConsensus<TKey, TValue> : IDisposable where TKey : ICloneable where TValue : ICloneable
    {
        Task<EJoinClusterResponse> JoinCluster(string clusterName, string clusterPassword, int maxNodes, bool useEncryption);
        void ManualAddPeer(string name, IPEndPoint endPoint);

        string GetClusterName();
        string GetNodeName();

        TValue ReadEntryValue(TKey key);
        TValue[] ReadEntryValueHistory(TKey key);
        Task<ERaftAppendEntryState> AppendEntry(TKey key, TValue value);
        event EventHandler<Tuple<TKey, TValue>> OnNewCommitedEntry;

        bool IsUasRunning();

        event EventHandler StartUas;
        event EventHandler<EStopUasReason> StopUas;
    }
}