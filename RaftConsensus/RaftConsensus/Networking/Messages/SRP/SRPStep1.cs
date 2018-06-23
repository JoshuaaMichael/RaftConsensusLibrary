namespace TeamDecided.RaftConsensus.Networking.Messages.SRP
{
    public class SRPStep1 : BaseSecureMessage
    {
        //public string UserName; this can be implied from BaseClass's From
        public byte[] A;

        public SRPStep1(string to, string from, byte[] A)
            : base(to, from) { this.A = A; }
    }
}
