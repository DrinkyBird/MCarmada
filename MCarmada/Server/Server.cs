using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.Api;
using MCarmada.Network;
using MCarmada.World;

namespace MCarmada.Server
{
    class Server : ITickable
    {
        public Listener listener;

        public Level level;
        public List<Player> players = new List<Player>();

        public uint CurrentTick { get; private set; }

        public Server(ushort port)
        {
            listener = new Listener(this, port);
            level = new Level(128, 128, 128);
        }

        public void Tick()
        {
            listener.AcceptNewConnections();

            for (int i = 0; i < listener.Connections.Count; i++)
            {
                ClientConnection connection = listener.Connections[i];

                connection.Flush();
                connection.Receive();
            }

            foreach (var player in players)
            {
                player.Tick();
            }

            UpdateConsoleTitle();

            CurrentTick++;
        }

        public Player CreatePlayer(ClientConnection connection, string name)
        {
            Player player = new Player(this, connection, name);

            players.Add(player);

            level.Save("worlds");

            return player;
        }

        public void DestroyPlayer(Player player)
        {
            player.Despawn();

            players.Remove(player);
        }

        public void BroadcastMessage(sbyte id, string message)
        {
            Packet msg = new Packet(PacketType.Header.Message);
            msg.Write(id);
            msg.Write(message);

            foreach (var player in players)
            {
                player.Send(msg);
            }
        }

        public void BroadcastMessage(string message)
        {
            BroadcastMessage(-1, message);
        }

        private void UpdateConsoleTitle()
        {
            Console.Title = "MCarmada - " + players.Count + " players";
        }
    }
}
