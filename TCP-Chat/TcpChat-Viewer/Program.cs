﻿// Filename:  TcpChatViewer.cs        
// Author:    Benjamin N. Summerton <define-private-public>        
// License:   Unlicense (http://unlicense.org/)        

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TcpChatViewer
{
    class TcpChatViewer
    {
        //connection objects
        public readonly string ServerAddress;
        public readonly int Port;
        private TcpClient _client;
        public bool Running { get; private set; }
        public bool _disconnectedRequested = false;

        //buffer & messaging
        public readonly int BufferSize = 2 * 1024; //2kb
        private NetworkStream _msgStream = null;

        public TcpChatViewer(string serverAddress, int port)
        {
            //create a nonm-connedcted TcpClient
            _client = new TcpClient(); //other constructor will start a connection
            _client.SendBufferSize = BufferSize;
            _client.ReceiveBufferSize = BufferSize;
            Running = false;

            //set the other things
            ServerAddress = serverAddress;
            Port = port;
        }

        //connects to the chat server
        public void Connect()
        {
            //Now try to connect
            _client.Connect(ServerAddress, Port); //will resolve DNS for us; blocks
            EndPoint endPoint = _client.Client.RemoteEndPoint;

            //check that we're connected
            if (_client.Connected)
            {
                //got in!
                Console.WriteLine("Connected to the server at {0}.", endPoint);

                //send them the message that we're a viewer
                _msgStream = _client.GetStream();
                byte[] nameBytes = Encoding.UTF8.GetBytes("viewer");
                _msgStream.Write(nameBytes, 0, nameBytes.Length); //blocks

                //check that we're still connected, if the server has not kicked us, then we're in
                if (!_isDisconnected(_client))
                {
                    Running = true;
                    Console.WriteLine("Press Ctrl-C to exit the Viewer at any time.");
                }
                else
                {
                    //server doens't see us as a viewer, cleanup
                    _cleanupNetworkResources();
                    Console.WriteLine("The server didn't recognise us as a Viewer.\n:[");
                }
            }
            else
            {
                _cleanupNetworkResources();
                Console.WriteLine("Wasn't able to connect to the server at {0}.", endPoint);
            }
        }

        //request a disconnect
        public void Disconnect()
        {
            Running = false;
            _disconnectedRequested = true;
            Console.WriteLine("Disconnecting from the chat...");
        }

        //Main loop, listens and prints messages from the server
        public void ListenForMessages()
        {
            bool wasRunning = Running;

            //Listen for messages
            while (Running)
            {
                //do we have a message?
                int messageLength = _client.Available;
                if (messageLength > 0)
                {
                    //Console.WriteLine("New incoming message of {0} bytes", messageLength);

                    //read the whole message
                    byte[] msgBuffer = new byte[messageLength];
                    _msgStream.Read(msgBuffer, 0, messageLength); //blocks

                    // An alternative way of reading
                    //int bytesRead = 0;
                    //while (bytesRead < messageLength)
                    //{
                    //    bytesRead += _msgStream.Read(_msgBuffer,
                    //                                 bytesRead,
                    //                                 _msgBuffer.Length - bytesRead);
                    //    Thread.Sleep(1);    // Use less CPU
                    //}

                    //decode it and print it
                    string msg = Encoding.UTF8.GetString(msgBuffer);
                    Console.WriteLine(msg);
                }
                //Use less CPU
                Thread.Sleep(10);

                //check the server didn't disconnect us
                if (_isDisconnected(_client))
                {
                    Running = false;
                    Console.WriteLine("The server has disconnected us.\n:[");
                }

                //check that a cancel has been requested by the user
                Running &= !_disconnectedRequested;
            }

            //Cleanup
            _cleanupNetworkResources();
            if (wasRunning)
                Console.WriteLine("Disconnected from the chat server.");

        }

        // Cleans any leftover network resources
        private void _cleanupNetworkResources()
        {
            _msgStream?.Close();
            _msgStream = null;
            _client.Close();
        }

        // Checks if a socket has disconnected
        // Adapted from -- http://stackoverflow.com/questions/722240/instantly-detect-client-disconnection-from-server-socket
        private static bool _isDisconnected(TcpClient client)
        {
            try
            {
                Socket s = client.Client;
                return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
            }
            catch (SocketException se)
            {
                // We got a socket error, assume it's disconnected
                return true;
            }
        }

        public static TcpChatViewer viewer;

        protected static void InterruptHandler(object sender, ConsoleCancelEventArgs args)
        {
            viewer.Disconnect();
            args.Cancel = true; //prevent the process from terminating immediately
        }

        public static void Main(string[] args)
        {
            // Setup the Viewer
            string host = "localhost";//args[0].Trim();
            int port = 6000;//int.Parse(args[1].Trim());
            viewer = new TcpChatViewer(host, port);

            // Add a handler for a Ctrl-C press
            Console.CancelKeyPress += InterruptHandler;

            // Try to connect & view messages
            viewer.Connect();
            viewer.ListenForMessages();
        }



    }
}