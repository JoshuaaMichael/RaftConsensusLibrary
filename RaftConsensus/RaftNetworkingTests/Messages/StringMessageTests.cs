using System;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Networking.Messages;

namespace TeamDecided.RaftConsensus.Networking.Tests.Messages
{
    [TestFixture]
    class StringMessageTests
    {
        string _to;
        string _from;
        string _data;

        StringMessage _sut;

        [SetUp]
        public void BeforeTest()
        {
            _to = new Guid().ToString();
            _from = new Guid().ToString();
            _data = new Guid().ToString();

            _sut = new StringMessage(_to, _from, _data);
        }

        [Test]
        public void IT_SerialiseDeserialise_AllMembersAreEqual()
        {
            //Arrange

            //Act
            byte[] serialise = _sut.Serialize();
            StringMessage deserialised = BaseMessage.Deserialize<StringMessage>(serialise);

            //Assert
            Assert.AreEqual(_to, deserialised.To);
            Assert.AreEqual(_from, deserialised.From);
            Assert.AreEqual(typeof(StringMessage), deserialised.MessageType);
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
            Assert.AreEqual(typeof(StringMessage), _sut.MessageType);
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

            Assert.DoesNotThrow(() => { BaseMessage.Deserialize<StringMessage>(serialized); });
        }

        [Test]
        public void UT_Deserialize_ReturnsStringWithData()
        {
            byte[] serialized = _sut.Serialize();
            StringMessage deserialized = BaseMessage.Deserialize<StringMessage>(serialized);

            Assert.IsNotNull(deserialized);
            Assert.IsTrue(deserialized.Data.Length > 0);
        }
    }
}
