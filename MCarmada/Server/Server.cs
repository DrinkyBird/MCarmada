using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.Api;
using MCarmada.Cpe;
using MCarmada.Network;
using MCarmada.Utils;
using MCarmada.World;
using NLog;

namespace MCarmada.Server
{
    partial class Server : ITickable
    {
        public Listener listener;

        public Level level;
        public Player[] players;

        public uint CurrentTick { get; private set; }

        private Logger logger = LogUtils.GetClassLogger();
        private string Salt;

        public static readonly CpeExtension[] CPE_EXTENSIONS =
        {
        };

        public Server(ushort port)
        {
            Salt = GenerateSalt(32);

            players = new Player[32];
            listener = new Listener(this, port);
            level = new Level(this, 512, 64, 512);
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
                if (player == null) continue;

                player.Tick();
            }

            UpdateConsoleTitle();
            SendHeartbeat();

            CurrentTick++;
        }

        public Player CreatePlayer(ClientConnection connection, string name)
        {
            int id = FindIdForPlayer();

            if (id == -1)
            {
                connection.Disconnect("Server is full");
                return null;
            }

            Player player = new Player(this, connection, name, id);

            players[id] = player;

            level.Save("worlds");

            return player;
        }

        private int FindIdForPlayer()
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        public Player FindPlayerByName(string name)
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null)
                {
                    continue;
                }

                if (players[i].Name == name)
                {
                    return players[i];
                }
            }

            return null;
        }

        public void DestroyPlayer(Player player)
        {
            player.Despawn();

            players[player.ID] = null;
        }

        public void BroadcastMessage(sbyte id, string message)
        {
            logger.Info(message);
            Packet msg = new Packet(PacketType.Header.Message);
            msg.Write(id);
            msg.Write(message);

            BroadcastPacket(msg);
        }

        public void BroadcastMessage(string message)
        {
            BroadcastMessage(-1, message);
        }

        public void BroadcastPacket(Packet packet)
        {
            foreach (var player in players)
            {
                if (player == null) continue;

                player.Send(packet);
            }
        }

        public void BroadcastPacketExcept(Packet packet, Player except)
        {
            foreach (var player in players)
            {
                if (player == null) continue;

                if (player == except)
                {
                    continue;
                }

                player.Send(packet);
            }
        }

        public int GetOnlinePlayers()
        {
            int num = 0;

            foreach (var player in players)
            {
                if (player != null)
                {
                    num++;
                }
            }

            return num;
        }

        private void UpdateConsoleTitle()
        {
            Console.Title = "MCarmada - " + GetOnlinePlayers() + " players";
        }
    }
}
