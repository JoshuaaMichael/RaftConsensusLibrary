namespace TeamDecided.RaftConsensus.Networking.Messages.SRP
{
    internal class SRPStep1 : SecureMessage
    {
        //public string UserName; this can be implied from BaseClass's From
        public byte[] A;

        public SRPStep1(string to, string from, byte[] A)
            : base(to, from) { this.A = A; }
    }
}
