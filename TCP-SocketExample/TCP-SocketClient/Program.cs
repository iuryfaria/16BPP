using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Client
{
    class TcpSocketClientExample
    {
        public static int MaxReceiveLEngth = 255;
        public static int PortNumeber = 6000;

        //Main method
        public static void Main(string[] args)
        {
            int len;
            byte[] buffer = new byte[MaxReceiveLEngth + 1];

            //Create a TCP/IP socket. 
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serv = new IPEndPoint(IPAddress.Loopback, PortNumeber);

            //Connect to the server
            Console.WriteLine("Connecting to the server...");
            clientSocket.Connect(serv);

            //Get a message (blocks)
            len = clientSocket.Receive(buffer);
            Console.WriteLine("Got a message from the sever[{0} bytes]:\n{1}", len, Encoding.ASCII.GetString(buffer, 0, len));

            //cleanup
            clientSocket.Close();

        }
    }
}