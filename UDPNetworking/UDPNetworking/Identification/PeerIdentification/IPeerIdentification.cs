namespace UDPNetworking.Identification.PeerIdentification
{
    public interface IPeerIdentification : IIdentification
    {
        bool Equals(IPeerIdentification obj);
    }
}
