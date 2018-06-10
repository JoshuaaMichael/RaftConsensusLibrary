using System;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Tests.Networking.Messages
{
    [TestFixture]
    class ByteMessageTests
    {
        string _to;
        string _from;
        byte[] _data;

        ByteMessage _sut;

        static Random _rand = new Random();

        [SetUp]
        public void BeforeTest()
        {
            _to = Guid.NewGuid().ToString();
            _from = Guid.NewGuid().ToString();
            _data = new byte[256];
            _rand.NextBytes(_data);

            _sut = new ByteMessage(_to, _from, _data);
        }

        [Test]
        public void IT_SerialiseDeserialise_AllMembersAreEqual()
        {
            //Arrange

            //Act
            byte[] serialise = _sut.Serialize();
            ByteMessage deserialised = BaseMessage.Deserialize<ByteMessage>(serialise);

            //Assert
            Assert.AreEqual(_to, deserialised.To);
            Assert.AreEqual(_from, deserialised.From);
            Assert.AreEqual(typeof(ByteMessage), deserialised.MessageType);
            Assert.AreEqual(_data, deserialised.Data);
        }

        [Test]
        public void UT_GetTo_MemberIsEqual()
        {
            Assert.AreEqual(_to, _sut.To);
        }

        [Test]
        public void UT_GetFrom_MemberIsEqual()
        {
            Assert.AreEqual(_from, _sut.From);
        }

        [Test]
        public void UT_GetMessageType_MemberIsEqual()
        {
            Assert.AreEqual(typeof(ByteMessage), _sut.MessageType);
        }

        [Test]
        public void UT_GetData_MemberIsEqual()
        {
            Assert.AreEqual(_data, _sut.Data);
        }

        [Test]
        public void UT_Serialize_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => { _sut.Serialize(); });
        }

        [Test]
        public void UT_Serialize_ReturnsByteArrayWithData()
        {
            byte[] serialized = _sut.Serialize();
            Assert.IsNotNull(serialized);
            Assert.IsTrue(serialized.Length > 0);
        }

        [Test]
        public void UT_Deserialize_DoesNotThrow()
        {
            byte[] serialized = _sut.Serialize();

            Assert.DoesNotThrow(() => { BaseMessage.Deserialize<ByteMessage>(serialized); });
        }

        [Test]
        public void UT_Deserialize_ReturnsStringWithData()
        {
            byte[] serialized = _sut.Serialize();
            ByteMessage deserialized = BaseMessage.Deserialize<ByteMessage>(serialized);

            Assert.IsNotNull(deserialized);
            Assert.IsTrue(deserialized.Data.Length > 0);
        }
    }
}
