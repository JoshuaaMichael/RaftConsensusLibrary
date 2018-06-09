namespace TeamDecided.RaftConsensus.Networking.Messages
{
    public class StringMessage : BaseMessage
    {
        public string Data { get; private set; }
        public StringMessage(string to, string from, string data)
            : base(to, from) { Data = data; }
    }
}
