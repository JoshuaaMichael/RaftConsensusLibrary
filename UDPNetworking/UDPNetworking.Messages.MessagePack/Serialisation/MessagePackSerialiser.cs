using MessagePack;
using UDPNetworking.Messages;

namespace UDPNetworking.Serialisation
{
    public class MessagePackSerialiser : ISerializer
    {
        public byte[] Serialize(IBaseMessage message)
        {
            return MessagePackSerializer.Serialize(message);
        }

        public IBaseMessage Deserialize(byte[] messageBytes)
        {
            return MessagePackSerializer.Deserialize<IBaseMessage>(messageBytes);
        }
    }
}
