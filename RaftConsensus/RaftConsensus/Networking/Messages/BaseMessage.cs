using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace TeamDecided.RaftConsensus.Networking.Messages
{
    public abstract class BaseMessage
    {
        public string To { get; set; }
        public string From { get; set; }
        public Type MessageType { get; private set; }
        internal IPEndPoint IPEndPoint { get; set; }

        public BaseMessage()
        {
            MessageType = GetType();
        }

        public BaseMessage(string to, string from)
        {
            To = to;
            From = from;
            MessageType = GetType();
        }

        public byte[] Serialize()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            string json = JsonConvert.SerializeObject(this, settings);
            return Encoding.UTF8.GetBytes(json);
        }

        public static T Deserialize<T>(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            return JsonConvert.DeserializeObject<T>(json, settings);
        }
    }
}
