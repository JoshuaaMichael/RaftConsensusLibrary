using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UDPNetworking.Messages;
using BF = System.Runtime.Serialization.Formatters.Binary.BinaryFormatter;

namespace UDPNetworking.Serialisation
{
    public class BinaryFormatter : ISerializer
    {
        private static readonly BF Formatter = new BF();

        public byte[] Serialize(IBaseMessage message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Formatter.Serialize(ms, message);
                return ms.ToArray();
            }
        }

        public IBaseMessage Deserialize(byte[] messageBytes)
        {
            using (MemoryStream ms = new MemoryStream(messageBytes))
            {
                return (IBaseMessage) Formatter.Deserialize(ms);
            }
        }
    }
}
