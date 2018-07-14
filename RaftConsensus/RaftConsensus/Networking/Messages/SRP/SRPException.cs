namespace TeamDecided.RaftConsensus.Networking.Messages.SRP
{
    internal class SRPException : SecureMessage
    {
        public string Message;

        public SRPException(string to, string from, string message)
            : base(to, from) { Message = message; }

        public SRPException(string to, string from, string session, string message)
            : base(to, from, session) { Message = message; }
    }
}
