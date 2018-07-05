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
        public static Program Instance { get; private set; }

        static void Main(string[] args)
        {
            new Program();
        }

        private Server.Server server;
        private bool running = true;
        public Settings Settings;

        private Program()
        {
            Instance = this;

            Initialise();
            DoLoop();
        }

        private bool Initialise()
        {
            Settings = Settings.Load();
            server = new Server.Server(Settings.Port);

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
