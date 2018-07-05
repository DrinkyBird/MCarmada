using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using MCarmada.Api;
using MCarmada.Cpe;
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
        private bool compressed = false;
        private bool levelSent = false;
        private int dataOffset = 0;

        public int ID { get; private set; }

        public float X, Y, Z, Yaw, Pitch;

        private uint lastPing = 0;

        private Logger logger;
        private CpeExtension[] extensions;

        private string messageBuffer = String.Empty;

        public Player(Server server, ClientConnection connection, string name, int id)
        {
            this.connection = connection;
            this.server = server;

            Name = name;
            ID = id;

            logger = LogManager.GetLogger("Player[" + Name + "]");

            extensions = new CpeExtension[connection.clientSupportedExtensions.Count];
            for (int i = 0; i < extensions.Length; i++)
            {
                KeyValuePair<string, int> pair = connection.clientSupportedExtensions[i];
                CpeExtension ext = new CpeExtension(pair.Key, pair.Value);
                extensions[i] = ext;
            }
        }

        public void Tick()
        {
            if (!levelSent)
            {
                HandleLevelData();
            }

            if (server.CurrentTick - lastPing >= 20)
            {
                Send(new Packet(PacketType.Header.Ping));
            }
        }

        public void SendLevel()
        {
            Send(new Packet(PacketType.Header.LevelInit));

            levelCompresser = new LevelCompresser(server.level);
        }

        private void HandleLevelData()
        {
            if (!compressed)
            {
                int total = server.level.Blocks.Length;
                int length = Math.Min(16 * 1024, total - dataOffset);

                byte[] array = new byte[length];
                Array.Copy(server.level.Blocks, dataOffset, array, 0, array.Length);
                dataOffset += length;

                levelCompresser.AddChunk(array);

                if (dataOffset == total)
                {
                    levelCompresser.Flush();
                    compressed = true;
                }
            }
            else
            {
                int length;
                for (int i = 0; i < 2; i++)
                {
                    byte[] chunk = levelCompresser.GetChunk(out length);

                    Packet packet = new Packet(PacketType.Header.LevelChunk);
                    packet.Write((short)length);
                    packet.Write(chunk);
                    packet.Write((byte)0);
                    Send(packet);

                    logger.Info(levelCompresser.Position + " / " + levelCompresser.Length);

                    if (levelCompresser.Position >= levelCompresser.Length)
                    {
                        levelSent = true;

                        Packet finish = new Packet(PacketType.Header.LevelFinish);
                        finish.Write((short)server.level.Width);
                        finish.Write((short)server.level.Depth);
                        finish.Write((short)server.level.Height);
                        Send(finish);

                        SpawnPlayer();

                        break;
                    }
                }
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
                Block newBlock = destroyed ? Block.Air : (Block) packet.ReadByte();

                level.SetBlock(x, y, z, newBlock);
            }
            else if (packet.Type == PacketType.Header.Message)
            {
                byte unused = packet.ReadByte();
                string message = packet.ReadString();

                if (SupportsExtension(CpeExtension.LongerMessages))
                {
                    if (unused == 0x01)
                    {
                        messageBuffer += message;
                    }
                    else if (unused == 0x00)
                    {
                        if (messageBuffer != String.Empty)
                        {
                            messageBuffer += message;
                            messageBuffer = "<" + Name + "> &f" + messageBuffer;

                            List<string> lines = new List<string>();
                            lines.Add(messageBuffer.Substring(0, 64));
                            messageBuffer = messageBuffer.Substring(64);
                            string buf = String.Empty;

                            const string linePrefix = " &f";
                            int lineLen = 64 - linePrefix.Length;

                            for (int i = 0; i < messageBuffer.Length; i++)
                            {
                                buf += messageBuffer[i];

                                if (i % lineLen == 0 && i != 0)
                                {
                                    buf = linePrefix + buf;
                                    lines.Add(buf);
                                    buf = string.Empty;
                                }
                            }

                            foreach (var line in lines)
                            {
                                server.BroadcastMessage(line);
                            }

                            messageBuffer = String.Empty;
                        }
                        else
                        {
                            server.BroadcastMessage("<" + Name + "> &f" + message);
                        }
                    }
                }
                else
                {
                    server.BroadcastMessage("<" + Name + "> &f" + message);
                }
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

        public bool SupportsExtension(string name, int version = 1)
        {
            foreach (var extension in extensions)
            {
                if (extension.Name == name && extension.Version == version)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
