using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MCarmada.Api;
using MCarmada.Network;
using MCarmada.Utils;
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
        public double levelDataTime = 0;
        private int levelDataOffset = 0;
        private int chunksThisTick = 0;

        public int ID { get; private set; }

        public float X, Y, Z, Yaw, Pitch;

        private uint lastPing = 0;

        private Logger logger;

        public Player(Server server, ClientConnection connection, string name, int id)
        {
            this.connection = connection;
            this.server = server;

            Name = name;
            ID = id;

            logger = LogManager.GetLogger("Player[" + Name + "]");
        }

        public void Tick()
        {
            if (queuedLevelData != null)
            {
                for (chunksThisTick = 0; chunksThisTick < 1; chunksThisTick++)
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
            if (TimeUtil.GetTimeInMs() - levelDataTime < 1000.0)
            {
                return;
            }

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

            if (levelDataOffset == queuedLevelData.Length)
            {
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
            else if (packet.Type == PacketType.Header.PlayerPosition)
            {
                byte id = packet.ReadByte();
                X = FixedPoint.ToFloatingPoint(packet.ReadShort());
                Y = FixedPoint.ToFloatingPoint(packet.ReadShort());
                Z = FixedPoint.ToFloatingPoint(packet.ReadShort());
                Yaw = packet.ReadByte();
                Pitch = packet.ReadByte();

                Packet update = new Packet(PacketType.Header.PlayerPosition);
                update.Write((byte) ID);
                update.Write(FixedPoint.ToFixedPoint(X));
                update.Write(FixedPoint.ToFixedPoint(Y));
                update.Write(FixedPoint.ToFixedPoint(Z));
                update.Write((byte) Yaw);
                update.Write((byte) Pitch);
                server.BroadcastPacketExcept(update, this);
            }
        }

        private void SpawnPlayer()
        {
            BlockPos pos = server.level.GetPlayerSpawn();
            logger.Info("Spawning player at " + pos);

            server.BroadcastMessage(Name + " has connected.");

            foreach (var player in server.players)
            {
                if (player == null)
                {
                    continue;
                }

                if (player == this)
                {
                    Packet s = new Packet(PacketType.Header.SpawnPlayer);
                    s.Write((sbyte) -1);
                    s.Write(Name);
                    s.WriteFixedPointPos(pos);
                    s.Write((byte)0);
                    s.Write((byte)0);
                    Send(s);
                }
                else
                {
                    Packet s = new Packet(PacketType.Header.SpawnPlayer);
                    s.Write((sbyte) player.ID);
                    s.Write(player.Name);
                    s.WriteFixedPointPos(pos);
                    s.Write((byte) 0);
                    s.Write((byte)0);
                    Send(s);

                    Packet os = new Packet(PacketType.Header.SpawnPlayer);
                    os.Write((sbyte)ID);
                    os.Write(Name);
                    os.WriteFixedPointPos(pos);
                    os.Write((byte)0);
                    os.Write((byte)0);
                    player.Send(os);
                }
            }
        }

        public void Send(Packet packet)
        {
            connection.Send(packet);
        }

        public void Despawn()
        {
            Packet despawn = new Packet(PacketType.Header.DespawnPlayer);
            despawn.Write((byte) ID);
            server.BroadcastPacket(despawn);
        }
    }
}
