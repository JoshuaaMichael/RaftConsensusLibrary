using System;

namespace UDPNetworking.Identification.MessageVersionIdentification
{
    public interface IMessageVersionIdentification : IIdentification, IComparable
    {
        bool Equals(IMessageVersionIdentification obj);
        int CompareTo(IMessageVersionIdentification obj);
    }
}
