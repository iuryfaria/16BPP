using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace TcpGames
{
   public class GuessMyNumberGame : IGame
    {
        //objects for the game
        private TcpGamesServer _server;
        private TcpClient _player;
        private Random _rng;
        private bool _needToDisconnectClient = false;

        //name of the game
        public string Name
        {
            get { return "Guess My Number"; }
        }

        //Just needs only one player
        public int RequiredPlayers
        {
            get { return 1; }
        }

        //Constructor
        public GuessMyNumberGame(TcpGamesServer server)
        {
            _server = server;
            _rng = new Random();
        }

        //Adds only a single player to the game
        public bool AddPlayer(TcpClient client)
        {
            //Make Sure only one player was added
            if (_player == null)
            {
                _player = client;
                return true;
            }

            return false;
        }

        //if the cliehnt who disconnected is ours, we need to quit our game
        public void DisconnectClient(TcpClient client)
        {
            _needToDisconnectClient = (client == _player);
        }

        //main loop of the game
        //packets are sent synchronously though
        public void Run()
        {
            //make sure we have a player
            bool running = (_player != null);
            if (running)
            {
                //send a instrucion placket
                Packet introPacket = new Packet("message",
                    "Welcome player, I want you to guess my number" +
                    "It's somewhere between (and including) 1 and 100.\n");
                _server.SendPacket(_player, introPacket).GetAwaiter().GetResult();
            }
            else
                return;

            //should be [1, 100]
            int theNumber = _rng.Next(1, 101);
            Console.WriteLine("Our Number is: {0}", theNumber);

            //some bools for game state
            bool correct = false;
            bool clientConnected = true;
            bool clientDisconnectedGracefully = false;

            //Main game loop
            while (running)
            {
                //Poll for input
                Packet inputPacket = new Packet("input", "Your guess: ");
                _server.SendPacket(_player, inputPacket).GetAwaiter().GetResult();

                //Read their answer
                Packet answerPacket = null;
                while (answerPacket == null)
                {
                    answerPacket = _server.ReceivePacket(_player).GetAwaiter().GetResult();
                    Thread.Sleep(100);
                }

                //check for graceful disconnect
                if (answerPacket.Command == "bye")
                {
                    _server.HandleDisconnectedClient(_player);
                    clientDisconnectedGracefully = true;
                }

                //check input
                if (answerPacket.Command == "input")
                {
                    Packet responsePacket = new Packet("message");

                    int theirGuess;
                    if (int.TryParse(answerPacket.Message, out theirGuess))
                    {
                        //see if they won
                        if (theirGuess== theNumber)
                        {
                            correct = true;
                            responsePacket.Message = "Correct! You Win!\n";
                        }
                        else if (theirGuess < theNumber)
                            responsePacket.Message = "Too low, try again.\n";
                        else if (theirGuess > theNumber)
                            responsePacket.Message = "Too high, try again.\n";
                    }
                    
                    else
                        responsePacket.Message = "Invalid input, please try again.\n";

                    //send the message
                    _server.SendPacket(_player, responsePacket).GetAwaiter().GetResult();
                }
                
                //take a small nap
                Thread.Sleep(10);

                //if they aren't correct, keep them here
                running &= !correct;

                //check for disconnect, may have happend gracefully before
                if (!_needToDisconnectClient && !clientDisconnectedGracefully)
                    clientConnected &= !TcpGamesServer.IsDisconnected(_player);
                else
                    clientConnected = false;

                running &= clientConnected;

            }

            //Thank the player and dsiconnect them
            if (clientConnected)
                _server.DisconnectClient(_player, "Thanks for playing the \"Guess My Number\" game!");
            else
                Console.WriteLine("Client disconnected from game.");

            Console.WriteLine("Game \"{0}\" has ended.", Name);

        }

    }

}
