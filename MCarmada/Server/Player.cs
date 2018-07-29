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
using MCarmada.Utils.Maths;
using MCarmada.World;
using NLog;

namespace MCarmada.Server
{
    public class Player : ITickable
    {
        private ClientConnection connection;
        private Server server;

        public string Name { get; private set; }

        private ILevelCompressor levelCompressor;
        private bool compressed = false;
        private bool levelSent = false;
        private int dataOffset = 0;

        public int ID { get; private set; }

        public float X, Y, Z, Yaw, Pitch;

        private uint lastPing = 0;

        private Logger logger;
        public CpeExtension[] Extensions { get; private set; }

        private string messageBuffer = String.Empty;

        public int CpeBlockSupportLevel { get; private set; }

        private bool _isOp = false;
        public bool IsOp
        {
            get { return _isOp; }
            set
            {
                _isOp = value;
                Send(new Packet(PacketType.Header.SetUserType).Write((byte) (_isOp ? 0x64 : 0x00)));
            }
        }

        private float _clickDistance = 5.0f;
        public float ClickDistance
        {
            get { return _clickDistance; }
            set
            {
                if (!SupportsExtension(CpeExtension.ClickDistance)) return;

                _clickDistance = value; 
                Send(new Packet(PacketType.Header.CpeClickDistance).Write(FixedPoint.ToFixedPoint(_clickDistance)));
            }
        }

        internal Player(Server server, ClientConnection connection, string name, int id)
        {
            this.connection = connection;
            this.server = server;

            Name = name;
            ID = id;

            logger = LogManager.GetLogger("Player[" + Name + "]");

            Extensions = new CpeExtension[connection.clientSupportedExtensions.Count];
            for (int i = 0; i < Extensions.Length; i++)
            {
                KeyValuePair<string, int> pair = connection.clientSupportedExtensions[i];
                CpeExtension ext = new CpeExtension(pair.Key, pair.Value);
                Extensions[i] = ext;
            }
        }

        public void Tick()
        {
            if (!levelSent)
            {
                levelCompressor.Process();

                if (levelCompressor.IsComplete())
                {
                    levelSent = true;

                    SpawnPlayer();

                    levelCompressor.Dispose();
                    levelCompressor = null;
                }
            }

            if (server.CurrentTick - lastPing >= 20)
            {
                Send(new Packet(PacketType.Header.Ping));
                lastPing = server.CurrentTick;
            }
        }

        public void SendLevel()
        {
            bool fastMap = SupportsExtension(CpeExtension.FastMap, 1);
            bool useFallbacks = !SupportsExtension(CpeExtension.CustomBlocks);

            if (fastMap)
            {
                int volume = server.level.Width * server.level.Depth * server.level.Height;

                Send(new Packet(PacketType.Header.LevelInit).Write(volume), false);
                levelCompressor = new LevelCompressorFast(this, server.level, useFallbacks);
            }
            else
            {
                Send(new Packet(PacketType.Header.LevelInit));
                levelCompressor = new LevelCompressorSlow(this, server.level, useFallbacks);
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
                Block oldBlock = level.GetBlock(x, y, z);

                Vector3 bp = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                float dist = Vector3.Distance(bp, new Vector3(X, Y, Z));

                if (dist > ClickDistance + 0.5f) // 0.5f for lag comp
                {
                    logger.Warn("Client tried to set block outside click distance (" + dist + " > " + (ClickDistance + 0.5f) + ")");

                    Packet correction = new Packet(PacketType.Header.ServerSetBlock);
                    correction.Write((short)x);
                    correction.Write((short)y);
                    correction.Write((short)z);
                    correction.Write(oldBlock);
                    Send(correction);

                    return;
                }

                if ((newBlock == Block.Water || newBlock == Block.WaterStill || newBlock == Block.Lava ||
                     newBlock == Block.LavaStill || newBlock == Block.Bedrock) && !IsOp)
                {
                    logger.Warn("Client tried to set illegal block " + newBlock + " at [" + x + ", " + y + ", z]" );
                    Packet correction = new Packet(PacketType.Header.ServerSetBlock);
                    correction.Write((short) x);
                    correction.Write((short) y);
                    correction.Write((short) z);
                    correction.Write(oldBlock);
                    Send(correction);

                    SendMessage("&cIllegal block: &f" + newBlock);
                    return;
                }

                if (destroyed)
                {
                    server.PluginManager.OnPlayerDestroyBlock(this, x, y, z, oldBlock);
                }
                else
                {
                    server.PluginManager.OnPlayerPlaceBlock(this, x, y, z, newBlock);
                }

                server.PluginManager.OnPlayerChangeBlock(this, x, y, z, oldBlock, newBlock);

                level.ChangeBlock(x, y, z, newBlock, this);
            }
            else if (packet.Type == PacketType.Header.Message)
            {
                byte unused = packet.ReadByte();
                string message = packet.ReadString();

                if (message.StartsWith("/"))
                {
                    logger.Info("Player issued command " + message);
                    server.CommandManager.Execute(this, message);
                    return;
                }

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
                Vector3 oldPosition = new Vector3(X, Y, Z);
                Vector2 oldRotation = new Vector2(Yaw, Pitch);

                byte id = packet.ReadByte();
                X = FixedPoint.ToFloatingPoint(packet.ReadShort());
                Y = FixedPoint.ToFloatingPoint(packet.ReadShort());
                Z = FixedPoint.ToFloatingPoint(packet.ReadShort());
                byte byteYaw = packet.ReadByte();
                byte bytePitch = packet.ReadByte();

                Yaw = FixedPoint.AngleToFloat(byteYaw);
                Pitch = FixedPoint.AngleToFloat(bytePitch);

                Vector3 newPosition = new Vector3(X, Y, Z);
                Vector2 newRotation = new Vector2(Yaw, Pitch);

                Packet update = new Packet(PacketType.Header.PlayerPosition);
                update.Write((byte) ID);
                update.Write(FixedPoint.ToFixedPoint(X));
                update.Write(FixedPoint.ToFixedPoint(Y));
                update.Write(FixedPoint.ToFixedPoint(Z));
                update.Write(byteYaw);
                update.Write(bytePitch);
                server.BroadcastPacketExcept(update, this);

                if (oldPosition != newPosition)
                {
                    server.PluginManager.OnPlayerMove(this, oldPosition, newPosition);
                }

                if (oldRotation != newRotation)
                {
                    server.PluginManager.OnPlayerRotate(this, oldRotation, newRotation);
                }
            }
            else if (packet.Type == PacketType.Header.CpeCustomBlockSupportLevel)
            {
                CpeBlockSupportLevel = packet.ReadByte();
            }
        }

        private void SpawnPlayer()
        {
            BlockPos pos = server.level.GetPlayerSpawn();
            logger.Info("Spawning player at " + pos);

            server.BroadcastMessage(Name + " has connected.");

            Send(new Packet(PacketType.Header.CpeEnvWeatherSetType).Write((byte) server.level.Weather));
            server.level.InformPlayerOfEnvironment(this);

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

            server.PluginManager.OnPlayerSpawn(this);
        }

        public void Send(Packet packet)
        {
            Send(packet, true);
        }

        private void Send(Packet packet, bool checkPacketSize)
        {
            connection.Send(packet, checkPacketSize);
        }

        public void SendMessage(string message, MessageType type = MessageType.Chat)
        {
            if (!SupportsExtension(CpeExtension.FullCp437))
            {
                char[] arr = message.ToCharArray();
                for (var i = 0; i < arr.Length; i++)
                {
                    char c = arr[i];
                    if (c > 127)
                    {
                        arr[i] = '?';
                    }
                }
                message = new string(arr);
            }

            Packet msg = new Packet(PacketType.Header.Message);
            msg.Write((sbyte) (SupportsExtension(CpeExtension.MessageTypes) ? type : 0));
            msg.Write(message);
            Send(msg);
        }

        public void Despawn()
        {
            server.PluginManager.OnPlayerQuit(this);

            Level.SavedPlayer saved = new Level.SavedPlayer()
            {
                Name = Name,
                X = X,
                Y = Y,
                Z = Z,
                Yaw = Yaw,
                Pitch = Pitch
            };
            server.level.savedPlayers.Add(saved);

            Packet despawn = new Packet(PacketType.Header.DespawnPlayer);
            despawn.Write((byte) ID);
            server.BroadcastPacket(despawn);
        }

        public void Disconnect(string reason)
        {
            connection.Disconnect(reason);
        }

        public bool SupportsExtension(string name, int version = 1)
        {
            foreach (var extension in Extensions)
            {
                if (String.Equals(extension.Name, name, StringComparison.CurrentCultureIgnoreCase) && extension.Version == version)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
