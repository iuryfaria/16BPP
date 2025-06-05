using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpGames
{
   interface IGame
    {
        #region Properties
        //Name of the game
        string Name { get; }

        //How many players are needed to start
        int RequiredPlayers { get; }
        #endregion // Properties

        #region Functions
        //Adds a player to the game (should be before ir starts)
        bool AddPlayer(TcpClient player);

        //tells the server to disconnect a player
        void DisconnectClient(TcpClient client);


        //the main game loop
        void Run();
        #endregion // Functions
    }
}
