namespace TeamDecided.RaftConsensus.Networking.Enums
{
    enum EProcessingThreadArrayIndex
    {
        OnNetworkingStop = 0,
        OnMessageReceive = 1,
        OnMessageReceiveFailure = 2,
        OnMessageSendFailure = 3,
        OnNewConnectedPeer = 4
    }
}
