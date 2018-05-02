using System;
using System.Text;
using Newtonsoft.Json;

namespace TeamDecided.RaftNetworking.Messages
{
    public class BaseMessage
    {
        public string To { get; private set; }
        public string From { get; private set; }
        public Type MessageType { get; private set; }

        public BaseMessage(string to, string from)
        {
            To = to;
            From = from;
            MessageType = GetType();
        }

        public byte[] Serialise()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            string json = JsonConvert.SerializeObject(this, settings);
            return Encoding.UTF8.GetBytes(json);
        }

        public static T Deserialise<T>(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            return JsonConvert.DeserializeObject<T>(json, settings);
        }
    }
}
