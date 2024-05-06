using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JammerV1.Models
{
    public class BlockAckPacket {
        public string SourceAddress { get; set; }
        public string DestinationAddress {get; set;}
        public int Power {get; set;}
        public DateTime DetectedAt { get; set; }
    }
}