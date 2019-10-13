using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace UDPNetworking
{
    [MessagePackObject]
    [Union(0, typeof(Car))]
    [Union(1, typeof(Honda))]
    public abstract class Vehicle
    {
        [Key(0)]
        public string Make { get; set; }
        [Key(1)]
        public string Model { get; set; }
    }
}
