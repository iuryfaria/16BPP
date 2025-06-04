using System;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Data.SqlTypes;
using System.Runtime.InteropServices;
using System.Net.Http.Json;


namespace TcpChatServer
{
    class TcpChatServer
    {
        //What listens in
        private TcpListener _listener;


        //types of clientes connected
        private List<TcpClient> _viewers = new List<TcpClient>();
        private List<TcpClient> _messengers = new List<TcpClient>();

        //names that are taken by other messengers
        private Dictionary<TcpClient, string> _names = new Dictionary<TcpClient, string>();

        //messages that need to be sent
        private Queue<String> _messageQueue = new Queue<string>();

        //extra fun data
        public readonly string ChatName;
        public readonly int Port;
        public bool Running { get; private set; }

        //buff
        public readonly int BufferSize = 2 * 1024; //2KB

        //make a new TCP chat server, with our provided name
        public TcpChatServer(string chatName, int port)
        {
            //set the basic data
            ChatName = chatName;
            Port = port;
            Running = false;

            //make the listener listen for connections on any network device
            _listener = new TcpListener(IPAddress.Any, port);
        }

        //if the server is running, this will shut down the server
        public void Shutdown()
        {
            Running = false;
            Console.WriteLine("Shutting down server...");
        }

        //start running the server. Will stop when `Shutdown` is called
        public void Run()
        {
            //some info
            Console.WriteLine("Starting the \"{0}\" TCP Chat Server on port {1}.", ChatName, Port);
            Console.WriteLine("Press Ctrl-C to shut down the server any time.");

            //make the server run 
            _listener.Start();
            Running = true;

            //Main server loop
            while (Running)
            {
                //check for new clients
                if (_listener.Pending())
                    _handleNewConnection();

                // Do the rest
                _checkForDisconnects();
                _checkForNewMessage();
                _sendMessages();

                // Use less CPU
                Thread.Sleep(10);
            }

            //stop the server, and clean up any connected clients
            foreach (TcpClient v in _viewers)
                _cleanupClient(v);
            foreach (TcpClient m in _messengers)
                _cleanupClient(m);
            _listener.Stop();

            // Some info
            Console.WriteLine("Server is shut down.");
        }

        private void _handleNewConnection()
        {
            //there is (at least) one, see what they want
            bool good = false;
            TcpClient newClient = _listener.AcceptTcpClient(); //blocks
            NetworkStream netStream = newClient.GetStream();

            //Modify the default buffer sizes
            newClient.SendBufferSize = BufferSize;
            newClient.ReceiveBufferSize = BufferSize;

            //print some info
            EndPoint endPoint = newClient.Client.RemoteEndPoint;
            Console.WriteLine("Handling a new client from {0}...", endPoint);

            //let them identify themselves
            byte[] msgBuffer = new byte[BufferSize];
            int bytesRead = netStream.Read(msgBuffer, 0, msgBuffer.Length); //blocks
            //Console.WriteLine("Got {0} bytes.", bytesRead);

            if (bytesRead > 0)
            {
                string msg = Encoding.UTF8.GetString(msgBuffer, 0, bytesRead);


                if (msg == "viewer")
                {
                    //they just want to watch
                    good = true;
                    _viewers.Add(newClient);

                    Console.WriteLine("{0} is a Viewer.", endPoint);

                    //Send them a "Hello Mesasage"
                    msg = string.Format("Welcome to the \"{0}\" Chat Server!", ChatName);
                    msgBuffer = Encoding.UTF8.GetBytes(msg);
                    netStream.Write(msgBuffer, 0, msgBuffer.Length); //blocks

                }
                else if (msg.StartsWith("name:"))
                {
                    //okay, so they might be a messenger
                    string name = msg.Substring(msg.IndexOf(':') + 1);

                    if ((name != string.Empty) && (!_names.ContainsValue(name)))
                    {
                        //the're new here, add them in
                        good = true;
                        _names.Add(newClient, name);
                        _messengers.Add(newClient);

                        Console.WriteLine("{0} is a Messenger with the name {1}.", endPoint, name);

                        //tell the viewers we have a new messenger
                        _messageQueue.Enqueue(String.Format("{0} has joined the chat.", name));
                    }
                }
                else
                {
                    //wans't either a viewer or messenger, celan up anyways
                    Console.WriteLine("Wasn't able to identify {0} as a Viewer or Messenger", endPoint);
                    _cleanupClient(newClient);
                }


            }

            //do we really want them?
            if (!good)
                newClient.Close();

        }

        //Sees if any of the clients have left the chat server
        private void _checkForDisconnects()
        {
            //check the viewers first
            foreach (TcpClient v in _viewers.ToArray())
            {
                if (_isDisconnected(v))
                {
                    Console.WriteLine("Viewer {0} has left.", v.Client.RemoteEndPoint);

                    //cleanup on our end
                    _viewers.Remove(v); //Remnive From List
                    _cleanupClient(v);
                }
            }

            //Check the messengers second
            foreach (TcpClient m in _messengers.ToArray())
            {
                if (_isDisconnected(m))
                {
                    //get info about the messenger
                    string name = _names[m];

                    //tell the viewers someone has left
                    Console.WriteLine("Messeger {0} has left.", name);
                    _messageQueue.Enqueue(String.Format("{0} has left the chat.", name));

                    //cleanup on our end
                    _messengers.Remove(m); //Remove from list
                    _names.Remove(m); //Remove taken names
                    _cleanupClient(m);
                }
            }
        }

        //See if any of our messengers have sents us a new messeage, put in the queue
        private void _checkForNewMessage()
        {
            foreach (TcpClient m in _messengers)
            {
                int messageLength = m.Available; //how many bytes are available to read
                if (messageLength > 0)
                {
                    //there is a one! get it
                    byte[] msgBuffer = new byte[messageLength];
                    m.GetStream().Read(msgBuffer, 0, msgBuffer.Length); //blocks

                    //Attach a name to it and shove it into the queue
                    string msg = String.Format("{0}: {1}", _names[m], Encoding.UTF8.GetString(msgBuffer));
                    _messageQueue.Enqueue(msg);
                }
            }
        }
        
        //Clears out the message queue (and sends it to all of the viewers)
        private void _sendMessages()
        {
            foreach (string msg in _messageQueue)
            {
                //encode the messsage
                byte[] msgBuffer = Encoding.UTF8.GetBytes(msg);

                //send the message to each viewer
                foreach (TcpClient v in _viewers)
                    v.GetStream().Write(msgBuffer, 0, msgBuffer.Length); //blocks
            }

            //clear out the queue
            _messageQueue.Clear();
        }

        //checks if a socket has disconnected
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
                //we got a socket error, assume it's disconnected
                return true;
            }
        }

        //cleans up resources for a TcpClient
        private static void _cleanupClient(TcpClient client)
        {
            client.GetStream().Close(); //close network stream
            client.Close(); //close client
        }



        public static TcpChatServer chat;

        protected static void InterruptHandler(object sender, ConsoleCancelEventArgs args)
        {
            chat.Shutdown();
            args.Cancel = true; 
        }

        public static void Main(string[] args)
        {
            //Create the server
            string name = "Bad IRC";//args[0].Trim();
            int port = 6000;//int.Parse(args[1].Trim());
            chat = new TcpChatServer(name, port);

            //Add a handler for a Ctrl-C press
            Console.CancelKeyPress += InterruptHandler;

            //run the chat server
            chat.Run();
        }
    }
}