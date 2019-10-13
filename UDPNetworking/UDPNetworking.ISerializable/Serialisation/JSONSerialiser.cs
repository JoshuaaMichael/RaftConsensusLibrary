using Newtonsoft.Json;
using System.Text;
using UDPNetworking.Messages;

namespace UDPNetworking.Serialisation
{
    public class JSONSerialiser : ISerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public byte[] Serialize(IBaseMessage message)
        {
            string json = JsonConvert.SerializeObject(message, Settings);
            return Encoding.UTF8.GetBytes(json);
        }

        public IBaseMessage Deserialize(byte[] messageBytes)
        {
            string json = Encoding.UTF8.GetString(messageBytes);
            return JsonConvert.DeserializeObject<IBaseMessage>(json, Settings);
        }
    }
}
