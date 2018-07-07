using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using MCarmada.Api;
using MCarmada.Server;
using MCarmada.Utils;
using NLog;

namespace MCarmada
{
    class Program : ITickable
    {
        public static Program Instance { get; private set; }

        enum ConsoleCtrlHandlerCode : uint
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
        delegate bool ConsoleCtrlHandlerDelegate(ConsoleCtrlHandlerCode eventCode);
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerDelegate handlerProc, bool add);
        static ConsoleCtrlHandlerDelegate _consoleHandler;  

        static void Main(string[] args)
        {
            new Program();
        }

        private Server.Server server;
        private bool running = true;
        public Settings Settings;

        private Logger logger = LogUtils.GetClassLogger();

        private Program()
        {
            Instance = this;

            Initialise();
            DoLoop();
        }

        private bool Initialise()
        {
            AppDomain.CurrentDomain.ProcessExit += Shutdown; 
            _consoleHandler = new ConsoleCtrlHandlerDelegate(ConsoleEventHandler);
            SetConsoleCtrlHandler(_consoleHandler, true);

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

        private void Shutdown(object sender, EventArgs e)
        {
            running = false;
            logger.Info("Shutting down.....");

            if (server != null)
            {
                server.Dispose();
            }
        }

        bool ConsoleEventHandler(ConsoleCtrlHandlerCode eventCode)
        {
            // Handle close event here...
            switch (eventCode)
            {
                case ConsoleCtrlHandlerCode.CTRL_CLOSE_EVENT:
                case ConsoleCtrlHandlerCode.CTRL_BREAK_EVENT:
                case ConsoleCtrlHandlerCode.CTRL_LOGOFF_EVENT:
                case ConsoleCtrlHandlerCode.CTRL_SHUTDOWN_EVENT:
                    Shutdown(null, null);
                    Environment.Exit(0);
                    break;
            }

            return (false);
        }

        private void InitNLog()
        {

        }
    }
}
