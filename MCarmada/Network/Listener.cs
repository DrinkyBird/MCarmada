using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NLog;

namespace MCarmada.Network
{
    class Listener
    {
        private Server.Server server;
        private TcpListener socket;

        public List<ClientConnection> Connections = new List<ClientConnection>();

        private Logger logger = Utils.LogUtils.GetClassLogger();

        public Listener(Server.Server server, ushort port)
        {
            this.server = server;
            socket = new TcpListener(IPAddress.Any, port);
            socket.Start();

            logger.Info("Now listening on port " + port);
        }

        public void AcceptNewConnections()
        {
            while (socket.Pending())
            {
                Socket client = socket.AcceptSocket();
                client.Blocking = false;

                IPEndPoint endPoint = (IPEndPoint) client.RemoteEndPoint;
                IPAddress address = endPoint.Address;

                ClientConnection connection = new ClientConnection(server, client);
                Connections.Add(connection);
            }
        }
    }
}
