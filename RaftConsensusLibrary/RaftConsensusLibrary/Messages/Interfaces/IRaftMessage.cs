using UDPMessaging.Messages;

namespace RaftConsensusLibrary.Messages
{
    internal interface IRaftMessage : IBaseMessage
    {
        int Term { get; }
    }
}
