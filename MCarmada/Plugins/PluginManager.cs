using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MCarmada.Commands;
using MCarmada.Utils;
using NLog;

namespace MCarmada.Plugins
{
    internal partial class PluginManager
    {
        private static Logger logger = LogUtils.GetClassLogger();

        private List<Plugin> loadedPlugins = new List<Plugin>();
        private List<EventListener> eventListeners = new List<EventListener>();

        private Server.Server server;

        internal PluginManager(Server.Server server)
        {
            this.server = server;
        }

        internal void LoadDirectory(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<string> dlls = new List<string>();

            foreach (var file in files)
            {
                string ext = Path.GetExtension(file);

                if (ext.Equals(".dll", StringComparison.InvariantCultureIgnoreCase))
                {
                    dlls.Add(Path.GetFullPath(file));
                }
            }

            List<Plugin> plugins = new List<Plugin>();

            for (int i = 0; i < dlls.Count; i++)
            {
                string dll = dlls[i];
                Assembly assembly = null;
                try
                {
                    assembly = Assembly.LoadFrom(dll);
                }
                catch (Exception ex)
                {
                    logger.Error("Failed to load DLL: " + ex);
                    continue;
                }

                List<Plugin> assemblyPlugins = LoadAssembly(assembly);

                foreach (var plugin in assemblyPlugins)
                {
                    plugins.Add(plugin);
                }
            }

            loadedPlugins.AddRange(plugins);
        }

        internal List<Plugin> LoadAssembly(Assembly assembly)
        {
            List<Plugin> instances = new List<Plugin>();

            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsSubclassOf(typeof(Plugin)))
                {
                    continue;
                }

                object instance;

                try
                {

                    instance = Activator.CreateInstance(type);
                }
                catch (Exception ex)
                {
                    logger.Error("Failed to load  " + type.Name + " from " + assembly.FullName + ":");
                    logger.Error(ex);

                    continue;
                }

                Plugin plugin = (Plugin) instance;
                instances.Add(plugin);

                PluginInterface iface = new PluginInterface(this, server, plugin);
                logger.Info("Loading plugin " + plugin.Name);
                plugin.OnLoad(iface);
                logger.Info("Loaded");
            }

            return instances;
        }

        internal void AddEventListener(Plugin source, EventListener listener)
        {
            eventListeners.Add(listener);
        }

        internal void AddCommand(Plugin plugin, string name, Command command)
        {
            server.CommandManager.AddCommand(name, command);
        }
    }
}
