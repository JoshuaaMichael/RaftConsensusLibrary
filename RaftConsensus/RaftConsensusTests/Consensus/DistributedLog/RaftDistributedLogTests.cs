﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamDecided.RaftConsensus.Consensus.DistributedLog;

namespace TeamDecided.RaftConsensus.Tests.Consensus.DistributedLog
{
    internal class RaftDistributedLogTests : BaseRaftDistributedLogTests
    {
        public override void BeforeEachTest()
        {
            Log = new RaftDistributedLog<string, string>();
        }
    }
}