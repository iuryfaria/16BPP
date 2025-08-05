
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    class TcpSocketServerExample
    {
        public static int PortNumer = 6000;
        public static bool Running = false;
        public static Socket ServerSocket;

        //An interrupt handler for Ctrl-C presses
        public static void InterruptHandler(object sender, ConsoleCancelEventArgs args)
        {
            Console.WriteLine("Interrupt received, stopping server...");

            //Cleanup
            Running = false;
            ServerSocket.Shutdown(SocketShutdown.Both);
            ServerSocket.Close();
        }

        //Main method
        public static void Main(string[] args)
        {
            Socket clientSocket;
            byte[] msg = Encoding.ASCII.GetBytes("Hello, Client!\n");

            //set the endpoint options
            IPEndPoint serv = new IPEndPoint(IPAddress.Any, PortNumer);

            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ServerSocket.Bind(serv);

            //Start listening for incoming connections
            ServerSocket.Listen(5);

            //Setup the Ctrl-C
            Console.CancelKeyPress += InterruptHandler;
            Running = true;
            Console.WriteLine("Runnign the TCP server.");

            //Main loop
            while (Running)
            {
                //Wait for a new client (blocks)
                clientSocket = ServerSocket.Accept();

                //Print some infor about the remote client
                Console.WriteLine("Incoming connection from {0}, replying.", clientSocket.RemoteEndPoint);

                //Send a reply (blocks)
                clientSocket.Send(msg, SocketFlags.None);

                //Close the connection
                clientSocket.Close();
            }
        }
    }
}
