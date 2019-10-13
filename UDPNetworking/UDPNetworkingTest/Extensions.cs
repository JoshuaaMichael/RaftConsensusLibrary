using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using UDPNetworking.Identification.PeerIdentification;

namespace UDPNetworkingTest
{
    public static class Extensions
    {
        private static readonly Faker Faker = new Faker();

        public static IPEndPoint IpEndPoint(this Bogus.Randomizer randomizer)
        {
            return new IPEndPoint(
                new IPAddress(Faker.Random.Bytes(4)),
                Faker.Random.Int(IPEndPoint.MinPort + 1, IPEndPoint.MaxPort));
        }

        public static IPEndPoint Ipv6EndPoint(this Bogus.Randomizer randomizer)
        {
            return new IPEndPoint(
                new IPAddress(Faker.Random.Bytes(16)),
                Faker.Random.Int(IPEndPoint.MinPort + 1, IPEndPoint.MaxPort));
        }

        public static IPeerIdentification PeerIdentification(this Bogus.Randomizer randomizer)
        {
            return StringPeerIdentification.Generate();
        }
    }
}
