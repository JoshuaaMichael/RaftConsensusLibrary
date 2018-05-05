using System;
using System.Collections.Generic;
using System.Text;

namespace TeamDecided.RaftNetworking.Messages
{
    class SecureHello : BaseMessage
    {
        public SecureHello(string to, string from)
            : base(to, from)
        {

        }
    }
}
