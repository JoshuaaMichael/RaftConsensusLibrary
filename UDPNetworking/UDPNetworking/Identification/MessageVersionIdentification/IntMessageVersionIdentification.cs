using System;
using System.Collections.Generic;
using System.Text;

namespace UDPNetworking.Identification.MessageVersionIdentification
{
    public class IntMessageVersionIdentification : IMessageVersionIdentification
    {
        private readonly int _versionIdentification;

        public IntMessageVersionIdentification(int versionIdentification)
        {
            _versionIdentification = versionIdentification;
        }

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case int i:
                    return i == _versionIdentification;
                case IntMessageVersionIdentification imvi:
                    return imvi._versionIdentification == _versionIdentification;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _versionIdentification;
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

            throw new ArgumentException("obj is not the same type as this instance");
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
