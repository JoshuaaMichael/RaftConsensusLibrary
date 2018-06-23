namespace TeamDecided.RaftConsensus.Networking.Messages.SRP
{
    class SRPStep3 : BaseSecureMessage
    {
        public byte[] M1;

        public SRPStep3(string to, string from, string session, byte[] M1)
            : base(to, from, session) { this.M1 = M1; }
    }
}
