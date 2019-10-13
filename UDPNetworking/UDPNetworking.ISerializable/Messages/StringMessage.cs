using System;
using System.Runtime.Serialization;
using UDPNetworking.Identification.MessageTypeIdentification;
using UDPNetworking.Extensions;

namespace UDPNetworking.Messages
{
    [Serializable()]
    public class StringMessage : BaseMessage
    {
        public string Data { get; set; }
        private const string DataSerialisationStr = "Data";

        public StringMessage()
            : base(new TypeMessageTypeIdentification(typeof(StringMessage))) { }

        protected StringMessage(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info == null) return;
            Data = info.GetValue<string>(DataSerialisationStr);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue<string>(DataSerialisationStr, Data);
        }
    }
}
