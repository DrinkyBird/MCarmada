using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MCarmada
{
    public class Settings
    {
        public ushort Port { get; set; }
        public string ServerName { get; set; }
        public string ServerMotd { get; set; }
        public bool Public { get; set; }
        public bool VerifyNames { get; set; }
        public int MaxPlayers { get; set; }
        public bool Broadcast { get; set; }
        public bool Whitelist { get; set; }

        public struct WorldSettings
        {
            public string Name { get; set; }
            public int Width { get; set; }
            public int Depth { get; set; }
            public int Height { get; set; }
            public string Generator { get; set; }
            public bool WaterFlow { get; set; }
            public bool LavaFlow { get; set; }
            public bool GrowTrees { get; set; }
            public bool BlockFall { get; set; }
            public bool EnableSave { get; set; }
            public int Seed { get; set; }
        }

        public WorldSettings World { get; set; }

        internal Settings() { }

        public static Settings Load()
        {
            Deserializer s = new DeserializerBuilder()
                .WithNamingConvention(new UnderscoredNamingConvention())
                .Build();

            string content = File.ReadAllText("Settings/settings.yml");
            Settings result;

            try
            {
                using (StringReader reader = new StringReader(content))
                {
                    result = s.Deserialize<Settings>(reader);
                }
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    throw e.InnerException;
                }

                throw e;
            }

            return result;
        }
    }
}
