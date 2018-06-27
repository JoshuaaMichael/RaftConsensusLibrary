namespace TeamDecided.RaftConsensus.Networking.Messages.SRP
{
    class SRPStep4 : BaseSecureMessage
    {
        public byte[] M2;

        public SRPStep4(string to, string from, string session, byte[] M2)
            : base(to, from, session) { this.M2 = M2; }
    }
}
