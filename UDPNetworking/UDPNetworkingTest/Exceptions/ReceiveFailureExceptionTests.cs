using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UDPNetworking.Exceptions;

namespace UDPNetworkingTest.Exceptions
{
    [TestClass]
    public class ReceiveFailureExceptionTests
    {
        class ReceiveFailureExceptionData
        {
            private static readonly Faker Faker = new Faker();

            public static readonly string MessageOutterException = Faker.Random.Word();
            public static readonly string MessageInnerException = Faker.Random.Word();
            public static readonly Exception InnerException = new Exception(MessageInnerException);

            public ReceiveFailureException Generate()
            {
                return new ReceiveFailureException(MessageOutterException, InnerException);
            }
        }

        [TestMethod]
        public void TestMethod1()
        {
            //Arrange

            //Act

            //Assert
        }

        //Are all members the type they should be
        //If I put a value in there, does it come back
    }
}
