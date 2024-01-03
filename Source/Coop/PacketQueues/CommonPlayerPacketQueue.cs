using StayInTarkov.Networking.Packets;
using System.Collections.Generic;

namespace StayInTarkov.Coop.PacketQueues
{
    public class CommonPlayerPacketQueue : Queue<CommonPlayerPacket>
    {
        public CommonPlayerPacketQueue(int capacity) : base(capacity)
        {

        }
    }
}
