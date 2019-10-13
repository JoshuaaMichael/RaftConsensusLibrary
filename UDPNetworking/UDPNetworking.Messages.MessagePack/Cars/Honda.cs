using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace UDPNetworking
{
    [MessagePackObject]
    public class Honda : Car
    {
        [Key(4)]
        public string PetName { get; set; }
    }
}
