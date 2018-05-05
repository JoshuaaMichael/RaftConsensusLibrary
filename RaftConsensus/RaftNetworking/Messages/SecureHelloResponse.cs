using System;
using System.Collections.Generic;
using System.Text;

namespace TeamDecided.RaftNetworking.Messages
{
    class SecureHelloResponse : BaseMessage
    {
        public SecureHelloResponse(string to, string from)
            : base(to, from)
        {

        }
    }
}
