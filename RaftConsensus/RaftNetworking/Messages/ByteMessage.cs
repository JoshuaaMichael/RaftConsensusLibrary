namespace TeamDecided.RaftNetworking.Messages
{
    class ByteMessage : BaseMessage
    {
        public byte[] Data { get; private set; }

        public ByteMessage(string to, string from, byte[] data)
            : base(to, from) { Data = data; }
    }
}
