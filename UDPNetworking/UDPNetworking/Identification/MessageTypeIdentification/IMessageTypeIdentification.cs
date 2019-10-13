namespace UDPNetworking.Identification.MessageTypeIdentification
{
    public interface IMessageTypeIdentification : IIdentification
    {
        bool Equals(IMessageTypeIdentification obj);
    }
}
