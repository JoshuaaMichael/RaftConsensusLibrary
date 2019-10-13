using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace UDPNetworking
{
    [MessagePackObject]
    public class Car : Vehicle
    {
        [Key(3)]
        public string Name { get; set; }
    }
}
