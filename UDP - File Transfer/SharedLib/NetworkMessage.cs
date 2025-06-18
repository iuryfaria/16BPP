using System.Net;

namespace UdpFileTransfer
{
    //this is a simple datastructure tath is used in packet queues
    public class NetworkMessage
    {
        public IPEndPoint Sender { get; set; }
        public Packet Packet { get; set; }

    }
}