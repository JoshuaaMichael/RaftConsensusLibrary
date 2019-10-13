using System;
using System.Runtime.Serialization;
using UDPNetworking.Extensions;

namespace UDPNetworking.Identification.MessageVersionIdentification
{
    [Serializable()]
    public class IntMessageVersionIdentification : IMessageVersionIdentification
    {
        private readonly int _versionIdentification;
        private const string VersionIdentificationSerialisationStr = "_versionIdentification";

        public IntMessageVersionIdentification(int versionIdentification)
        {
            _versionIdentification = versionIdentification;
        }

        protected IntMessageVersionIdentification(SerializationInfo info, StreamingContext ctxt)
        {
            _versionIdentification = info.GetValue<int>(VersionIdentificationSerialisationStr);
        }

        public override bool Equals(object obj)
        {
            return CompareTo(obj) == 0;
        }

        public override int GetHashCode()
        {
            return _versionIdentification;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue<int>(VersionIdentificationSerialisationStr, _versionIdentification);
        }

        public int CompareTo(object obj)
        {
            switch (obj)
            {
                case int i:
                    return _versionIdentification.CompareTo(i);
                case IntMessageVersionIdentification imvi:
                    return _versionIdentification.CompareTo(imvi._versionIdentification);
            }

            throw new ArgumentException("obj is not a compatible type with this instance");
        }

        public object GetIdentification()
        {
            return _versionIdentification;
        }

        public bool Equals(IMessageVersionIdentification obj)
        {
            return Equals((object)obj);
        }

        public int CompareTo(IMessageVersionIdentification obj)
        {
            return CompareTo((object)obj);
        }
    }
}
