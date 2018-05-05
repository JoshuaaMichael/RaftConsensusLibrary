using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TeamDecided.RaftNetworking.Messages;

namespace TeamDecided.RaftNetworking.Tests.Messages
{
    [TestFixture]
    class StringMessageTests
    {
        string to;
        string from;
        string data;

        StringMessage sut;

        [SetUp]
        public void BeforeTest()
        {
            to = new Guid().ToString();
            from = new Guid().ToString();
            data = new Guid().ToString();

            sut = new StringMessage(to, from, data);
        }

        [Test]
        public void IT_SerialiseDeserialise_AllMembersAreEqual()
        {
            //Arrange

            //Act
            byte[] serialise = sut.Serialize();
            StringMessage deserialised = BaseMessage.Deserialize<StringMessage>(serialise);

            //Assert
            Assert.AreEqual(to, deserialised.To);
            Assert.AreEqual(from, deserialised.From);
            Assert.AreEqual(typeof(StringMessage), deserialised.MessageType);
            Assert.AreEqual(data, deserialised.Data);
        }

        [Test]
        public void UT_GetTo_MemberIsEqual()
        {
            Assert.AreEqual(to, sut.To);
        }

        [Test]
        public void UT_GetFrom_MemberIsEqual()
        {
            Assert.AreEqual(from, sut.From);
        }

        [Test]
        public void UT_GetMessageType_MemberIsEqual()
        {
            Assert.AreEqual(typeof(StringMessage), sut.MessageType);
        }

        [Test]
        public void UT_GetData_MemberIsEqual()
        {
            Assert.AreEqual(data, sut.Data);
        }

        [Test]
        public void UT_Serialize_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => { sut.Serialize(); });
        }

        [Test]
        public void UT_Serialize_ReturnsByteArrayWithData()
        {
            byte[] serialized = sut.Serialize();
            Assert.IsNotNull(serialized);
            Assert.IsTrue(serialized.Length > 0);
        }

        [Test]
        public void UT_Deserialize_DoesNotThrow()
        {
            byte[] serialized = sut.Serialize();

            Assert.DoesNotThrow(() => { BaseMessage.Deserialize<StringMessage>(serialized); });
        }

        [Test]
        public void UT_Deserialize_ReturnsStringWithData()
        {
            byte[] serialized = sut.Serialize();
            StringMessage deserialized = BaseMessage.Deserialize<StringMessage>(serialized);

            Assert.IsNotNull(deserialized);
            Assert.IsTrue(deserialized.Data.Length > 0);
        }
    }
}
