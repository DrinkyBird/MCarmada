using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Extensions.Data;
using fNbt;
using MCarmada.Cpe;
using MCarmada.World.Generation;

namespace MCarmada.World
{
    public partial class Level
    {
        public void Save(string outDir)
        {
            if (!settings.EnableSave)
            {
                return;
            }

            byte[] blocks = BlocksAsByteArray();
            BlockPos spawnPos = GetPlayerSpawn();

            var classicWorld = new NbtCompound("ClassicWorld")
            {
                new NbtByte("FormatVersion", CLASSICWORLD_VERSION),
                new NbtString("Name", Name),
                new NbtShort("X", Width),
                new NbtShort("Y", Depth),
                new NbtShort("Z", Height),
                new NbtByteArray("UUID", Guid.ToByteArray()),
                new NbtByteArray("BlockArray", blocks),

                new NbtCompound("Spawn")
                {
                    new NbtShort("X", (short) spawnPos.X),
                    new NbtShort("Y", (short) spawnPos.Y),
                    new NbtShort("Z", (short) spawnPos.Z)
                },

                new NbtCompound("MapGenerator")
                {
                    new NbtString("Software", "MCarmada"),
                    new NbtString("MapGeneratorName", WorldGenerator.Generators.FirstOrDefault(x => x.Value == generator).Key)
                }
            };

            if (CreationTime != 0) classicWorld.Add(new NbtLong("TimeCreated", (int) CreationTime));
            if (ModificationTime != 0) classicWorld.Add(new NbtLong("LastModified", (int) ModificationTime));
            if (AccessedTime != 0) classicWorld.Add(new NbtLong("LastAccessed", (int) AccessedTime));

            var metadata = new NbtCompound("Metadata");

            var mcaData = new NbtCompound("MCarmada")
            {
                new NbtLong("BlockArrayHash", (long)XXHash.XXH64(blocks)),
                new NbtLong("LevelTick", (long)LevelTick),
                new NbtInt("LevelSeed", Seed),
                new NbtString("SoftwareVersion", Assembly.GetCallingAssembly().GetName().Version.ToString()),
                new NbtString("GeneratorName", WorldGenerator.Generators.FirstOrDefault(x => x.Value == generator).Key),
                new NbtString("GeneratorClassName", generator.GetType().FullName)
            };

            var ticks = new NbtList("ScheduledTicks", NbtTagType.Compound);
            foreach (var tick in scheduledTicks)
            {
                ticks.Add(tick.ToCompound());
            }

            var sp = new NbtCompound("Players");
            foreach (var player in savedPlayers)
            {
                // mitigate a crash bug
                if (sp.Contains(player.Name))
                {
                    continue;
                }

                sp.Add(player.ToCompound());
            }

            mcaData.Add(sp);
            mcaData.Add(ticks);

            var customBlocks = new NbtCompound("CustomBlocks")
            {
                new NbtInt("ExtensionVersion", Server.Server.GetExtension(CpeExtension.CustomBlocks).Version),
                new NbtShort("SupportLevel", 1)
            };

            byte[] fallback = new byte[256];
            for (int i = 0; i < fallback.Length; i++)
            {
                if (BlockConfig.CpeFallbacks.ContainsKey((Block) i))
                {
                    fallback[i] = (byte) BlockConfig.CpeFallbacks[(Block) i];
                }
                else
                {
                    fallback[i] = (byte) i;
                }
            }
            customBlocks.Add(new NbtByteArray("Fallback", fallback));

            var weatherType = new NbtCompound("EnvWeatherType")
            {
                new NbtInt("ExtensionVersion", Server.Server.GetExtension(CpeExtension.EnvWeatherType).Version),
                new NbtByte("WeatherType", (byte) Weather),
            };

            var cpe = new NbtCompound("CPE")
            {
                customBlocks,
                weatherType,

                new NbtCompound("EnvColors")
                {
                    new NbtInt("ExtensionVersion", Server.Server.GetExtension(CpeExtension.EnvColors).Version),

                    SkyColour.ToCompound("Sky"),
                    CloudColour.ToCompound("Cloud"),
                    FogColour.ToCompound("Fog"),
                    AmbientColour.ToCompound("Ambient"),
                    DiffuseColour.ToCompound("Sunlight")
                }
            };

            metadata.Add(cpe);
            metadata.Add(mcaData);

            classicWorld.Add(metadata);

            Directory.CreateDirectory(outDir);
            string file = Path.Combine(outDir, "world.cw");

            var worldFile = new NbtFile(classicWorld);
            worldFile.SaveToFile(file, NbtCompression.GZip);
        }

        public static Level Load(Server.Server server, Settings.WorldSettings settings, string path)
        {
            logger.Info("Loading world from " + path);
            var file = new NbtFile();

            try
            {
                file.LoadFromFile(path);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }

            var root = file.RootTag;

            byte fileVersion = root["FormatVersion"].ByteValue;
            if (fileVersion != CLASSICWORLD_VERSION)
            {
                throw new InvalidDataException("ClassicWorld version is inequal: file is v" + fileVersion + " but we support v" + CLASSICWORLD_VERSION);
            }

            short width = root["X"].ShortValue;
            short depth = root["Y"].ShortValue;
            short height = root["Z"].ShortValue;
            Guid guid = new Guid(root["UUID"].ByteArrayValue);
            int seed = 0;
            string generator = String.Empty;
            ulong tick = 0;
            List<ScheduledTick> ticks = new List<ScheduledTick>();
            int creationTime = root.Contains("TimeCreated") ? (int) root["TimeCreated"].LongValue : 0;
            int modifyTime = root.Contains("LastModified") ? (int) root["LastModified"].LongValue : 0;
            int accessTime = root.Contains("LastAccessed") ? (int) root["LastAccessed"].LongValue : 0;
            WeatherType weather = WeatherType.Clear;
            EnvColor skyColor = EnvColor.CreateDefault();
            EnvColor cloudColor = EnvColor.CreateDefault();
            EnvColor fogColor = EnvColor.CreateDefault();
            EnvColor ambientColor = EnvColor.CreateDefault();
            EnvColor diffuseColor = EnvColor.CreateDefault();

            byte[] blockBytes = root["BlockArray"].ByteArrayValue;
            Block[] blocks = new Block[blockBytes.Length];
            for (int i = 0; i < blockBytes.Length; i++)
            {
                blocks[i] = (Block) blockBytes[i];
            }

            if (root.Contains("Spawn"))
            {
                NbtCompound spawn = root.Get<NbtCompound>("Spawn");
                short spawnX = spawn["X"].ShortValue;
                short spawnY = spawn["Y"].ShortValue;
                short spawnZ = spawn["Z"].ShortValue;
            }

            if (root.Contains("Metadata"))
            {
                var metadata = root.Get<NbtCompound>("Metadata");

                if (metadata.Contains("MCarmada"))
                {
                    NbtCompound mca = metadata.Get<NbtCompound>("MCarmada");

                    ulong hash = (ulong)mca["BlockArrayHash"].LongValue;

                    if (XXHash.XXH64(blockBytes) != hash)
                    {
                        throw new InvalidDataException("Blocks hash mismatch!");
                    }

                    generator = mca["GeneratorName"].StringValue;
                    seed = mca["LevelSeed"].IntValue;
                    tick = (ulong)mca["LevelTick"].LongValue;

                    NbtList tickTags = mca.Get<NbtList>("ScheduledTicks");
                    for (int i = 0; i < tickTags.Count; i++)
                    {
                        NbtCompound compound = tickTags.Get<NbtCompound>(i);

                        ScheduledTick st = new ScheduledTick()
                        {
                            X = compound["X"].IntValue,
                            Y = compound["Y"].IntValue,
                            Z = compound["Z"].IntValue,
                            Timing = (TickTiming)compound["Timing"].ByteValue,
                            Event = (TickEvent)compound["Event"].ByteValue,
                            Tick = (ulong)compound["When"].LongValue
                        };

                        ticks.Add(st);
                    }
                }

                if (metadata.Contains("CPE"))
                {
                    var cpe = metadata.Get<NbtCompound>("CPE");

                    if (cpe.Contains("EnvWeatherType"))
                    {
                        var envWeatherType = cpe.Get<NbtCompound>("EnvWeatherType");

                        weather = (WeatherType) envWeatherType["WeatherType"].ByteValue;
                    }

                    if (cpe.Contains("EnvColors"))
                    {
                        var envColors = cpe.Get<NbtCompound>("EnvColors");

                        skyColor = EnvColor.Get(envColors, "Sky");
                        cloudColor = EnvColor.Get(envColors, "Cloud");
                        fogColor = EnvColor.Get(envColors, "Fog");
                        ambientColor = EnvColor.Get(envColors, "Ambient");
                        diffuseColor = EnvColor.Get(envColors, "Sunlight");
                    }
                }
            }

            Level lvl = new Level(server, settings, width, depth, height, blocks, seed, generator, tick, ticks)
            {
                Guid = guid,
                CreationTime = creationTime,
                ModificationTime = modifyTime,
                AccessedTime = accessTime,
                Weather = weather,
                
                SkyColour = skyColor,
                CloudColour = cloudColor,
                FogColour = fogColor,
                AmbientColour = ambientColor,
                DiffuseColour = diffuseColor
            };

            return lvl;
        }
    }
}
