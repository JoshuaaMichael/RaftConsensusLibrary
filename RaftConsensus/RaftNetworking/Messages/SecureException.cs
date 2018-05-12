using System;
using System.Collections.Generic;
using System.Text;

namespace TeamDecided.RaftNetworking.Messages
{
    internal class SecureException : SecureMessage
    {
        public string ClientName;
    }
}
