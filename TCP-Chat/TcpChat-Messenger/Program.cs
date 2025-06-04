
// Filename:  TcpChatMessenger.cs        
// Author:    Benjamin N. Summerton <define-private-public>        
// License:   Unlicense (http://unlicense.org/)        

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TcpChatMessenger
{
    class TcpChatMessenger
    {
        //connection objects
        public readonly string ServerAddress;
        public readonly int Port;
        private TcpClient _client;
        public bool Running { get; private set; }

        //buffer & messaging
        public readonly int BufferSize = 2 * 1024;//2kb 
        private NetworkStream _msgStream = null;

        //Personal data
        public readonly string Name;

        public TcpChatMessenger(string serverAddress, int port, string name)
        {
            //create a non-connedcted TcpClient
            _client = new TcpClient(); //other constructor will start a connection
            _client.SendBufferSize = BufferSize;
            _client.ReceiveBufferSize = BufferSize;
            Running = false;

            //set the other things
            ServerAddress = serverAddress;
            Port = port;
            Name = name;
        }

        public void Connect()
        {
            //Try to connect
            _client.Connect(ServerAddress, Port); //will resolve DNS for us; blocks
            EndPoint endPoint = _client.Client.RemoteEndPoint;

            //make sure we're connected
            if (_client.Connected)
            {
                //got in!
                Console.WriteLine("Connected to the server at {0}.", endPoint);

                //tell them that we're a messenger
                _msgStream = _client.GetStream();
                byte[] nameBytes = Encoding.UTF8.GetBytes(String.Format("name:{0}", Name));
                _msgStream.Write(nameBytes, 0, nameBytes.Length); //blocks

                //if we're still connected after sending our name, that means the server accepted us
                if (!_isDisconnected(_client))
                    Running = true;
                else
                {
                    //Name was probably taken...
                    _cleanupNetworkResources();
                    Console.WriteLine("The server rejected us; \"{0}\" is probably in use");
                }
            }
            else
            {
                _cleanupNetworkResources();
                Console.WriteLine("Wasn't able to connect to the server at {0}.", endPoint);
            }
        }

        public void SendMessages()
        {
            bool wasRunning = Running;

            while (Running)
            {
                //Poll for user input
                Console.Write("{0}>", Name);
                string msg = Console.ReadLine();

                //quit or send a message
                if ((msg.ToLower() == "quit") || (msg.ToLower() == "exit"))
                {
                    //user wants to quit
                    Console.WriteLine("Exiting...");
                    Running = false;
                }
                else if (msg != string.Empty)
                {
                    //send the message
                    byte[] msgBuffer = Encoding.UTF8.GetBytes(msg);
                    _msgStream.Write(msgBuffer, 0, msgBuffer.Length); //blocks

                }

                //use less CPU
                Thread.Sleep(100);

                //check the server didn't disconnect us
                if (_isDisconnected(_client))
                {
                    Running = false;
                    Console.WriteLine("The server has disconnected us.");
                }
            }

            _cleanupNetworkResources();
            if (wasRunning)
                Console.WriteLine("Disconnected.");
        }

        //cleans any leftover network resources
        private void _cleanupNetworkResources()
        {
            _msgStream?.Close();
            _msgStream = null;
            _client.Close();
        }
        
        //checks if a socket has disocnnected
        // Adapted from -- http://stackoverflow.com/questions/722240/instantly-detect-client-disconnection-from-server-socket

        private static bool _isDisconnected(TcpClient client)
        {
            try
            {
                Socket s = client.Client;
                return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
            }
            catch(SocketException se)
            {
                //we got a socket error, assume it's disconnected
                return false;
            }
        }


        public static void Main(string[] args)
        {
            // Get a name
            Console.Write("Enter a name to use: ");
            string name = Console.ReadLine();

            // Setup the Messenger
            string host = "localhost";//args[0].Trim();
            int port = 6000;//int.Parse(args[1].Trim());
            TcpChatMessenger messenger = new TcpChatMessenger(host, port, name);

            // connect and send messages
            messenger.Connect();
            messenger.SendMessages();
        }
    }
}
