using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using TeamDecided.RaftConsensus.Consensus.DistributedLog;

namespace TeamDecided.RaftConsensus.Tests.Consensus.DistributedLog
{
    internal class RaftDistributedLogPersistentTests : BaseRaftDistributedLogTests
    {
        //This may be used to use a physical SQLite file, however there is an issue running the tests so fast and creating/destorying the test
        //Even when closing, disposing, nulling and then garbage collecting sometime it still doesn't clear properlly
        private readonly string _storageFilename = TestContext.CurrentContext.TestDirectory + @"\persistentStorageLogTests.db";

        public override void BeforeEachTest()
        {
            if (File.Exists(_storageFilename))
            {
                File.Delete(_storageFilename);
            }
            Log = new RaftDistributedLogPersistent<string, string>("test", "");
        }

        public override void AfterEachTest()
        {
            Log.Dispose();
        }
    }
}
