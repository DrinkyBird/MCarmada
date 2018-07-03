using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MCarmada.Api;
using MCarmada.Server;

namespace MCarmada
{
    class Program : ITickable
    {
        static void Main(string[] args)
        {
            new Program();
        }

        private Server.Server server;
        private bool running = true;

        private Program()
        {
            Initialise();
            DoLoop();
        }

        private bool Initialise()
        {
            server = new Server.Server(25565);

            return true;
        }

        private void DoLoop()
        {
            while (running)
            {
                Tick();

                Thread.Sleep(1000 / 20);
            }
        }

        public void Tick()
        {
            server.Tick();
        }

        private void InitNLog()
        {

        }
    }
}
