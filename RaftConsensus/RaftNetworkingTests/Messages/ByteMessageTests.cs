using System;
using NUnit.Framework;
using TeamDecided.RaftNetworking.Messages;

namespace TeamDecided.RaftNetworking.Tests.Messages
{
    [TestFixture]
    class ByteMessageTests
    {
        string to;
        string from;
        byte[] data;

        ByteMessage sut;

        static Random rand = new Random();

        [SetUp]
        public void BeforeTest()
        {
            to = Guid.NewGuid().ToString();
            from = Guid.NewGuid().ToString();
            data = new byte[256];
            rand.NextBytes(data);

            sut = new ByteMessage(to, from, data);
        }

        [Test]
        public void IT_SerialiseDeserialise_AllMembersAreEqual()
        {
            //Arrange

            //Act
            byte[] serialise = sut.Serialize();
            ByteMessage deserialised = BaseMessage.Deserialize<ByteMessage>(serialise);

            //Assert
            Assert.AreEqual(to, deserialised.To);
            Assert.AreEqual(from, deserialised.From);
            Assert.AreEqual(typeof(ByteMessage), deserialised.MessageType);
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
            Assert.AreEqual(typeof(ByteMessage), sut.MessageType);
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

            Assert.DoesNotThrow(() => { BaseMessage.Deserialize<ByteMessage>(serialized); });
        }

        [Test]
        public void UT_Deserialize_ReturnsStringWithData()
        {
            byte[] serialized = sut.Serialize();
            ByteMessage deserialized = BaseMessage.Deserialize<ByteMessage>(serialized);

            Assert.IsNotNull(deserialized);
            Assert.IsTrue(deserialized.Data.Length > 0);
        }
    }
}
