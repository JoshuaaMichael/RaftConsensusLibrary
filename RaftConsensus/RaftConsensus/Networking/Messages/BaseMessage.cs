using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;

namespace TeamDecided.RaftConsensus.Networking.Messages
{
    public abstract class BaseMessage
    {
        public string To;
        public string From;
        public string TypeString;
        internal IPEndPoint IPEndPoint;

        protected bool Compressable;

        private Type _type;

        protected BaseMessage()
        {
            TypeString = GetType().FullName;
            Compressable = true;
        }

        public BaseMessage(string to, string from)
            : this()
        {
            To = to;
            From = from;
        }

        protected BaseMessage(IPEndPoint to, string from)
            : this()
        {
            IPEndPoint = to;
        }

        public Type GetMessageType()
        {
            if(_type == null)
            {
                _type = Type.GetType(TypeString);
            }
            return _type;
        }

        public byte[] Serialize()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            string json = JsonConvert.SerializeObject(this, settings);
            byte[] message = Encoding.UTF8.GetBytes(json);
            byte[] flaggedResult;

            if(Compressable)
            {
                message = Compress(message);
            }

            flaggedResult = new byte[message.Length + 1];
            flaggedResult[0] = (byte)((Compressable) ? 1 : 0);

            Buffer.BlockCopy(message, 0, flaggedResult, 1, message.Length);

            return flaggedResult;
        }

        public static BaseMessage Deserialize(byte[] data)
        {
            byte[] message;
            if (data[0] == 1) //if compressable
            {
                message = Decompress(data);
            }
            else
            {
                message = new byte[data.Length - 1];
                Buffer.BlockCopy(data, 1, message, 0, data.Length - 1);
            }

            string json = Encoding.UTF8.GetString(message);
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
            using (MemoryStream messageStream = new MemoryStream(message, 1, message.Length - 1))
            {
                using (GZipStream stream = new GZipStream(messageStream, CompressionMode.Decompress))
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
}
