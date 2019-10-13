using System.Runtime.Serialization;

namespace UDPNetworking.Identification
{
    public interface IIdentification : ISerializable
    {
        object GetIdentification();
    }
}
