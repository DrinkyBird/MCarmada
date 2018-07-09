using MCarmada.Server;

namespace MCarmada.Plugins
{
    public abstract partial class Plugin : EventListener
    {
        public abstract string Name { get; }

        public abstract void OnLoad(PluginInterface pluginInterface);

        public abstract void OnUnload();
    }
}
