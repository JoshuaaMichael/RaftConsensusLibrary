using System;
using System.Collections.Generic;
using System.Text;

namespace UDPNetworking.Identification.MessageTypeIdentification
{
    public class TypeMessageTypeIdentification<T> : StringMessageTypeIdentification
    {
        public TypeMessageTypeIdentification()
            :base(typeof(T).FullName) { }
    }
}
