﻿namespace TeamDecided.RaftNetworking.Messages
{
    internal class SecureServerHelloResponse : SecureMessage
    {
        public byte[] ServerName;
        public byte[] SessionInitial;
        public byte[] Challenge;
        public byte[] SymetricKey;
        public byte[] HMACSecret;
    }
}
