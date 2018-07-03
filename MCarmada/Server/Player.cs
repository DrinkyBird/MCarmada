using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MCarmada.Api;
using MCarmada.Network;
using MCarmada.World;
using NLog;

namespace MCarmada.Server
{
    class Player : ITickable
    {
        private ClientConnection connection;
        private Server server;

        public string Name { get; private set; }

        private LevelCompresser levelCompresser;
        private Thread levelCompresserThead;
        public byte[] queuedLevelData = null;
        private int levelDataOffset = 0;
        private int chunksThisTick = 0;

        private int id = 0;

        private uint lastPing = 0;

        private Logger logger;

        public Player(Server server, ClientConnection connection, string name)
        {
            this.connection = connection;
            this.server = server;

            this.Name = name;

            logger = LogManager.GetLogger("Player[" + Name + "]");
        }

        public void Tick()
        {
            if (queuedLevelData != null)
            {
                for (chunksThisTick = 0; chunksThisTick < 3; chunksThisTick++)
                {
                    if (queuedLevelData == null)
                    {
                        break;
                    }

                    HandleLevelData();
                }
            }

            if (server.CurrentTick - lastPing >= 20)
            {
                Send(new Packet(PacketType.Header.Ping));
            }
        }

        public void SendLevel()
        {
            Send(new Packet(PacketType.Header.LevelInit));

            levelCompresser = new LevelCompresser(server.level, this);
            levelCompresserThead = new Thread(levelCompresser.Run);
            levelCompresserThead.Name = "LevelCompression for " + Name;
            levelCompresserThead.Start();
        }

        private void HandleLevelData()
        {
            int len = Math.Min(1024, queuedLevelData.Length - levelDataOffset);
            byte[] output = new byte[len];
            Array.Copy(queuedLevelData, levelDataOffset, output, 0, len);

            levelDataOffset += len;

            int percent = (int) (((double)levelDataOffset / (double)queuedLevelData.Length) * 100.0);
            int bpercent = (int) (((double)levelDataOffset / (double)queuedLevelData.Length) * 100/0);
            Packet chunk = new Packet(PacketType.Header.LevelChunk);
            chunk.Write((short) len);
            chunk.Write(output);
            chunk.Write((byte) bpercent);
            Send(chunk);

            logger.Debug("Sending level to " + Name + ": " + levelDataOffset + "/" + queuedLevelData.Length + " (" + percent + "%)");

            if (levelDataOffset == queuedLevelData.Length)
            {
                logger.Debug("Level sent");
                Packet finish = new Packet(PacketType.Header.LevelFinish);
                finish.Write((short) server.level.Width);
                finish.Write((short) server.level.Depth);
                finish.Write((short) server.level.Height);
                Send(finish);

                queuedLevelData = null;
                levelDataOffset = 0;

                SpawnPlayer();
            }
        }

        public void HandlePacket(Packet packet)
        {
            if (packet.Type == PacketType.Header.PlayerSetBlock)
            {
                Level level = server.level;

                int x = packet.ReadShort();
                int y = packet.ReadShort();
                int z = packet.ReadShort();
                bool destroyed = packet.ReadByte() == 0;
                byte newBlock = destroyed ? (byte) 0 : packet.ReadByte();

                level.SetBlock(x, y, z, newBlock);
            }
            else if (packet.Type == PacketType.Header.Message)
            {
                byte unused = packet.ReadByte();
                string message = packet.ReadString();

                server.BroadcastMessage("<" + Name + "> " + message);
            }
        }

        private void SpawnPlayer()
        {
            BlockPos pos = server.level.GetPlayerSpawn();
            logger.Info("Spawning player at " + pos);

            Packet spawn = new Packet(PacketType.Header.Teleport);
            spawn.Write((byte) 255);
            spawn.WriteFixedPointPos(pos);
            spawn.Write((short) 0);
            spawn.Write((short) 0);
            Send(spawn);
        }

        public void Send(Packet packet)
        {
            connection.Send(packet);
        }

        public void Despawn()
        {

        }
    }
}
