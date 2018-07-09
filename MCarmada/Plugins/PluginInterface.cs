using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.Commands;

namespace MCarmada.Plugins
{
    public class PluginInterface
    {
        private Plugin owner;
        private PluginManager manager;
        internal Server.Server Server { get; private set; }

        internal PluginInterface(PluginManager manager, Server.Server server, Plugin owner)
        {
            this.owner = owner;
            this.manager = manager;
            this.Server = server;
        }

        internal void AddEventListener(EventListener listener)
        {
            manager.AddEventListener(owner, listener);
        }

        internal void AddCommand(string name, Command command)
        {
            manager.AddCommand(owner, name, command);
        }
    }
}
