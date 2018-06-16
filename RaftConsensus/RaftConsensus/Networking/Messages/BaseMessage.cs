using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace TeamDecided.RaftConsensus.Networking.Messages
{
    public abstract class BaseMessage
    {
        public string To { get; set; }
        public string From { get; set; }
        public Type MessageType { get; set; }
        internal IPEndPoint IPEndPoint { get; set; }

        protected BaseMessage() { }

        [JsonConstructor]
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
            return Compress(Encoding.UTF8.GetBytes(json));
        }

        public static BaseMessage Deserialize(byte[] data)
        {
            string json = Encoding.UTF8.GetString(Decompress(data));
            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            return JsonConvert.DeserializeObject<BaseMessage>(json, settings);
        }

        protected static byte[] Compress(byte[] message)
        {
            //https://www.dotnetperls.com/compress
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    gzip.Write(message, 0, message.Length);
                }
                return memory.ToArray();
            }
        }

        protected static byte[] Decompress(byte[] message)
        {
            //https://www.dotnetperls.com/decompress
            //Removed do while
            using (GZipStream stream = new GZipStream(new MemoryStream(message), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    while ((count = stream.Read(buffer, 0, size)) > 0)
                    {
                        memory.Write(buffer, 0, count);
                    }
                    return memory.ToArray();
                }
            }
        }
    }
}
