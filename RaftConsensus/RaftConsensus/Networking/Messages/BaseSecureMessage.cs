namespace TeamDecided.RaftConsensus.Networking.Messages
{
    public abstract class BaseSecureMessage : BaseMessage
    {
        public string Session { get; private set; }
        public byte[] EncryptedData { get; private set; }

        //Used by derived types who don't have a session yet
        protected BaseSecureMessage(string to, string from)
            : base(to, from) { }

        //Used to send SecureMessages which have a session
        protected BaseSecureMessage(string to, string from, string session)
            : base(to, from)
        {
            Session = session;
        }

        //Used to receive encrypted data
        protected BaseSecureMessage(string session, byte[] encryptedData)
        {
            Session = session;
            EncryptedData = encryptedData;
        }
    }
}
