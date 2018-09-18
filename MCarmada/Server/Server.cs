using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MCarmada.Api;
using MCarmada.Commands;
using MCarmada.Cpe;
using MCarmada.Network;
using MCarmada.Plugins;
using MCarmada.Utils;
using MCarmada.World;
using NLog;

namespace MCarmada.Server
{
    public partial class Server : ITickable, IDisposable
    {
        internal Listener listener;

        public Level level;
        public Player[] players;

        public uint CurrentTick { get; private set; }

        public string ServerName
        {
            get { return Program.Instance.Settings.ServerName; }
        }

        public string MessageOfTheDay
        {
            get { return Program.Instance.Settings.ServerMotd; }
        }

        private ushort port;

        private Logger logger = LogUtils.GetClassLogger();
        private string Salt;

        public NameList OpList { get; private set; }
        public NameList Whitelist { get; private set; }

        internal PluginManager PluginManager { get; private set; }
        internal CommandManager CommandManager { get; private set; }

        private Thread heartbeatThread;

        public static readonly CpeExtension[] CPE_EXTENSIONS =
        {
            new CpeExtension(CpeExtension.LongerMessages, 1),
            new CpeExtension(CpeExtension.CustomBlocks, 1),
            new CpeExtension(CpeExtension.FullCp437, 1),
            new CpeExtension(CpeExtension.MessageTypes, 1),
            new CpeExtension(CpeExtension.FastMap, 1),
            new CpeExtension(CpeExtension.ClickDistance, 1),
            new CpeExtension(CpeExtension.EnvWeatherType, 1), 
            new CpeExtension(CpeExtension.EnvColors, 1), 
            new CpeExtension(CpeExtension.EnvMapAspect, 1), 
        };

        public Server(ushort port)
        {
            this.port = port;
            Salt = GenerateSalt(32);

            Settings settings = Program.Instance.Settings;

            players = new Player[Program.Instance.Settings.MaxPlayers];

            CommandManager = new CommandManager();
            CommandManager.RegisterMainCommands();

            PluginManager = new PluginManager(this);
            PluginManager.LoadDirectory("Plugins/");

            if (!AttemptLoadLevel())
            {
                level = new Level(this, settings.World, (short)settings.World.Width, (short)settings.World.Depth, (short)settings.World.Height);
                SaveLevel();
            }

            OpList = new NameList("operators.txt");
            Whitelist = new NameList("whitelist.txt");

            listener = new Listener(this, port);
        }

        private bool AttemptLoadLevel()
        {
            Settings settings = Program.Instance.Settings;
            Settings.WorldSettings worldSettings = settings.World;

            if (!worldSettings.EnableSave)
            {
                return false;
            }

            string path = Path.GetFullPath("worlds/" + worldSettings.Name + "/world.cw");
            if (File.Exists(path))
            {
                try
                {
                    level = Level.Load(this, worldSettings, path);
                    logger.Info("Loaded world.");

                    return true;
                }
                catch (Exception e)
                {
                    logger.Error("Failed to load world: " + e);
                    return false;
                }
            }

            return false;
        }

        public void SaveLevel()
        {
            Settings settings = Program.Instance.Settings;
            Settings.WorldSettings worldSettings = settings.World;

            string path = Path.GetFullPath("worlds/" + worldSettings.Name);
            level.Save(path);
        }

        public void Tick()
        {
            double start = TimeUtil.GetTimeInMs();

            PluginManager.OnServerTick(this, CurrentTick);

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

            if ((CurrentTick - lastHeartbeat > 45 * 20 || lastHeartbeat == 0) && Program.Instance.Settings.Broadcast)
            {
                lastHeartbeat = CurrentTick;

                heartbeatThread = new Thread(SendHeartbeat);
                heartbeatThread.Name = "Heartbeat Thread";
                heartbeatThread.Start();
            }

            level.Tick();

            CurrentTick++;

            double end = TimeUtil.GetTimeInMs();
            double delta = end - start;

            if (delta >= 1000.0 / 20.0)
            {
                logger.Warn("Tick " + (CurrentTick - 1) + " took too long: Expected <= " + (1000.0 / 20.0) + " ms, but it took " + delta + " ms!");
            }
        }

        internal Player CreatePlayer(ClientConnection connection, string name)
        {
            int id = FindIdForPlayer();

            if (id == -1)
            {
                connection.Disconnect("Server is full");
                return null;
            }

            Player player = new Player(this, connection, name, id);

            players[id] = player;

            PluginManager.OnPlayerConnect(player);

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

            if (GetOnlinePlayers() == 0)
            {
                SaveLevel();
            }
        }

        public void BroadcastMessage(string message, MessageType type = MessageType.Chat)
        {
            logger.Info(message);

            foreach (var player in players)
            {
                if (player == null)
                {
                    continue;
                }

                player.SendMessage(message, type);
            }
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

        public void BroadcastOpEvent(Player op, string ev)
        {
            logger.Info("[" + op.Name + ": " + ev + "]");
            op.SendMessage("&f" + ev);
            foreach (var p in players.Where(x => (x != null && x.Name != op.Name && x.IsOp)))
            {
                p.SendMessage("&7[" + op.Name + ": " + ev + "]");
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

        public void BroadcastBlockChange(int x, int y, int z, Block block)
        {
            Block fallback = block;
            byte b = (byte) block;

            if (b >= (byte) Block.CobblestoneSlab && b <= (byte) Block.StoneBricks)
            {
                fallback = BlockConfig.CpeFallbacks[block];
            }

            foreach (var player in players)
            {
                if (player == null)
                {
                    continue;
                }

                Packet packet = new Packet(PacketType.Header.ServerSetBlock);
                packet.Write((short)x);
                packet.Write((short)y);
                packet.Write((short)z);
                packet.Write((byte) (player.SupportsExtension(CpeExtension.CustomBlocks) ? block : fallback));
                player.Send(packet);
            }
        }

        internal void UpdatePlayerOpStatus()
        {
            foreach (var player in players.Where(x => x != null))
            {
                player.IsOp = OpList.Contains(player.Name);
            }
        }

        private void UpdateConsoleTitle()
        {
            Console.Title = "MCarmada - " + GetOnlinePlayers() + " players";
        }

        public void Dispose()
        {
            foreach (var player in players)
            {
                if (player == null)
                {
                    continue;
                }

                player.Disconnect("Server shutting down");
            }
            
            SaveLevel();

            listener.Dispose();
        }

        public static CpeExtension GetExtension(string name)
        {
            foreach (var extension in CPE_EXTENSIONS)
            {
                if (extension.Name == name)
                {
                    return extension;
                }
            }

            return null;
        }
    }
}
