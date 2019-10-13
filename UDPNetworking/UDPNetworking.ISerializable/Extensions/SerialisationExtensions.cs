using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace UDPNetworking.Extensions
{
    public static class SerialisationExtensions
    {
        public static void AddValue<T>(this SerializationInfo info, string name, T value)
        {
            info.AddValue(name, value, typeof(T));
        }

        public static T GetValue<T>(this SerializationInfo info, string name)
        {
            return (T)info.GetValue(name, typeof(T));
        }
    }
}
