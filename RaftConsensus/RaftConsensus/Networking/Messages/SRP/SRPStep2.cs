using System;

namespace TeamDecided.RaftConsensus.Networking.Messages.SRP
{
    class SRPStep2 : BaseSecureMessage
    {
        public byte[] s;
        public byte[] B;

        public SRPStep2(string to, string from, byte[] s, byte[] B)
            : base(to, from, Guid.NewGuid().ToString())
        {
            this.s = s;
            this.B = B;
        }
    }
}
