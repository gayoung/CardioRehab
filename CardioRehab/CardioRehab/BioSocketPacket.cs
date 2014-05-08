using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardioRehab
{
    class BioSocketPacket
    {
        public System.Net.Sockets.Socket packetSocket;
        public byte[] dataBuffer = new byte[666];
    }
}
