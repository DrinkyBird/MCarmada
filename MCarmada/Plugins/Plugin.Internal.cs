using MCarmada.Commands;

namespace MCarmada.Plugins
{
    public abstract partial class Plugin
    {
        private PluginInterface Internal__interface;

        protected void _OnLoad(PluginInterface pluginInterface)
        {
            Internal__interface = pluginInterface;
        }

        public void AddEventListener(EventListener listener)
        {
            Internal__interface.AddEventListener(listener);
        }

        public PluginInterface GetPluginInterface()
        {
            return Internal__interface;
        }

        public void AddCommand(string name, Command command)
        {
            Internal__interface.AddCommand(name, command);
        }

        public Server.Server GetServer()
        {
            return Internal__interface.Server;
        }
    }
}
