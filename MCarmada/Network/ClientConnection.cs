using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MCarmada.Cpe;
using MCarmada.Server;
using MCarmada.Utils;
using NLog;

namespace MCarmada.Network
{
    class ClientConnection
    {
        private Server.Server server;
        private Socket socket;
        private IPAddress address;

        private MemoryStream inBuffer = new MemoryStream();
        private MemoryStream outBuffer = new MemoryStream();

        public List<KeyValuePair<string, int>> clientSupportedExtensions = new List<KeyValuePair<string, int>>();

        private string clientName;
        private Player player = null;

        public string ClientSoftware { get; private set; }

        public bool Connected { get; private set; }

        private Logger logger;

        public ClientConnection(Server.Server server, Socket socket)
        {
            this.server = server;
            this.socket = socket;

            IPEndPoint endPoint = (IPEndPoint) socket.RemoteEndPoint;
            address = endPoint.Address;

            this.logger = LogManager.GetLogger("ClientConnection[" + address.ToString() + "]");

            Connected = true;
        }

        public void Receive()
        {
            if (!Connected)
            {
                logger.Error("note: attempt to Receieve when disconnected!");
                return;
            }

            if (socket.Available < 1)
            {
                return;
            }

            inBuffer.SetLength(0);
            while (socket.Available > 0)
            {
                try
                {
                    byte[] buf = new byte[1024];
                    int recvd = socket.Receive(buf, 0, buf.Length, SocketFlags.None);
                    inBuffer.Write(buf, 0, recvd);
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.WouldBlock)
                    {
                        // nobody cares
                    }
                    else if (e.SocketErrorCode == SocketError.ConnectionReset || e.SocketErrorCode == SocketError.ConnectionAborted)
                    {
                        Connected = false;
                        Disconnect("Client disconnected");
                        return;
                    }
                    else
                    {
                        Disconnect("An unexpected socket error occured (" + e.SocketErrorCode.ToString() + ")");
                        return;
                    }
                }
            }
            inBuffer.Position = 0;

            int len = (int) inBuffer.Length;

            while (inBuffer.Position < len)
            {
                PacketType.Header type = (PacketType.Header) inBuffer.ReadByte();

                int packetLen = PacketType.GetPacketSize(type);
                byte[] data = new byte[packetLen + 1];
                inBuffer.Read(data, 1, packetLen);
                data[0] = (byte) type;

                Packet packet = new Packet(data);
                HandlePacket(packet);
                // in case we got disconnected in the middle of reading packets
                if (!Connected)
                {
                    return;
                }
            }

            inBuffer.SetLength(0);
        }

        private void HandlePacket(Packet packet)
        {
            if (packet.Type == PacketType.Header.PlayerIdent)
            {
                byte version = packet.ReadByte();
                clientName = packet.ReadString();
                string key = packet.ReadString();
                byte unused = packet.ReadByte();

                bool isCpe = unused == 0x42;

                logger.Info("Incoming connection from " + clientName);

                if (version != 0x07)
                {
                    Disconnect("Invalid version");
                    return;
                }

                if (server.FindPlayerByName(clientName) != null)
                {
                    Disconnect("There is already a player with that name!");
                    return;
                }

                if (!server.AuthenticateClient(clientName, key))
                {
                    Disconnect("Authentication failure");
                    return;
                }

                if (clientName != "csnxs" && clientName != "Tribeam")
                {
                    Disconnect("You are not on the whitelist!");
                    return;
                }

                if (isCpe)
                {
                    Packet cpeIdent = new Packet(PacketType.Header.CpeExtInfo);
                    cpeIdent.Write("MCarmada");
                    cpeIdent.Write((short)Server.Server.CPE_EXTENSIONS.Length);
                    Send(cpeIdent);

                    foreach (var extension in Server.Server.CPE_EXTENSIONS)
                    {
                        Packet entry = new Packet(PacketType.Header.CpeExtEntry);
                        entry.Write(extension.Name);
                        entry.Write(extension.Version);
                        Send(entry);
                    }
                }
                else
                {
                    BeginIdent();
                }
            }
            else if (packet.Type == PacketType.Header.CpeExtInfo)
            {
                ClientSoftware = packet.ReadString();
                int numExtensions = packet.ReadShort();

                logger.Info("Client using " + ClientSoftware + " supports " + numExtensions + " extensions");

                clientSupportedExtensions.Capacity = numExtensions;
            }
            else if (packet.Type == PacketType.Header.CpeExtEntry)
            {
                string name = packet.ReadString();
                int version = packet.ReadInt();

                clientSupportedExtensions.Add(new KeyValuePair<string, int>(name, version));

                if (clientSupportedExtensions.Count == clientSupportedExtensions.Capacity)
                {
                    BeginIdent();
                }
            }
            else if (player != null)
            {
                player.HandlePacket(packet);
            }
        }

        private void BeginIdent()
        {
            Packet ident = new Packet(PacketType.Header.ServerIdent);
            ident.Write((byte) 0x07);
            ident.Write("my MCarmada server");
            ident.Write("my MCarmada server");
            ident.Write((byte) 0x64);
            Send(ident);

            player = server.CreatePlayer(this, clientName);
            player.SendLevel();
        }

        public void Send(Packet packet)
        {
            if (packet.GetLength() - 1 != PacketType.GetPacketSize(packet.Type))
            {
                throw new InvalidOperationException("Length of packet " + packet.Type + " is wrong: " + packet.GetLength() + " should be " +
                                                 PacketType.GetPacketSize(packet.Type));
                return;
            }

            byte[] bytes = packet.GetBytes();
            int len = bytes.Length;

            if (outBuffer.Length + len > outBuffer.Capacity)
            {
                Flush();
            }

            outBuffer.Write(bytes, 0, len);
        }

        public void Flush()
        {
            if (!Connected)
            {
                return;
            }

            if (outBuffer.Length < 1)
            {
                return;
            }

            byte[] bytes = outBuffer.GetBuffer();
            int len = (int) outBuffer.Length;

            try
            {
                socket.Send(bytes, 0, len, SocketFlags.None);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.WouldBlock)
                {
                    // nobody cares
                }
                else if (e.SocketErrorCode == SocketError.ConnectionReset || e.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    Connected = false;
                    Disconnect("Client disconnected");
                    return;
                }
                else
                {
                    Disconnect("An unexpected socket error occured (" + e.SocketErrorCode.ToString() + ")");
                    return;
                }
            }

            outBuffer.SetLength(0);
        }

        public void Disconnect(string reason)
        {
            if (Connected)
            {
                Packet dc = new Packet(PacketType.Header.DisconnectPlayer);
                dc.Write(reason);
                Send(dc);
                Flush();
            }

            DestroyClient();

            logger.Info("Client connection lost.");

            server.BroadcastMessage(clientName + " has disconnected. (" + reason + ")");
        }

        private void DestroyClient()
        {
            Connected = false;

            if (player != null)
            {
                server.DestroyPlayer(player);
            }

            server.listener.Connections.Remove(this);

            socket.Dispose();
            inBuffer.Dispose();
            outBuffer.Dispose();
        }
    }
}
