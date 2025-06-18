using System;
using System.Net;
using System.Net.Sockets;

namespace PongGame
{
    // Data structure used to store Packets along with their sender
    public class NetworkMessage
    {
        public IPEndPoint Sender { get; set; }
        public Packet Packet { get; set; }
        public DateTime ReceiveTime { get; set; }
    }
}