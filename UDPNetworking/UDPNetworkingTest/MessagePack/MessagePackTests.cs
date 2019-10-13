using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UDPNetworking;

namespace UDPNetworkingTest.MessagePack
{
    [TestClass]
    public class MessagePackTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            Vehicle v = new Honda()
            {
                Make = Guid.NewGuid().ToString(),
                Model = Guid.NewGuid().ToString(),
                Name = Guid.NewGuid().ToString(),
                PetName = Guid.NewGuid().ToString()
            };

            byte[] bytes = MessagePackSerializer.Serialize(v);
            Honda mc2 = (Honda)MessagePackSerializer.Deserialize<Vehicle>(bytes);
        }
    }
}
