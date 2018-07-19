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

            var classicWorld = new NbtCompound("ClassicWorld");
            classicWorld.Add(new NbtByte("FormatVersion", CLASSICWORLD_VERSION));
            classicWorld.Add(new NbtString("Name", Name));
            classicWorld.Add(new NbtShort("X", Width));
            classicWorld.Add(new NbtShort("Y", Depth));
            classicWorld.Add(new NbtShort("Z", Height));
            classicWorld.Add(new NbtByteArray("UUID", Guid.ToByteArray()));
            if (CreationTime != 0) classicWorld.Add(new NbtLong("TimeCreated", (int) CreationTime));
            if (ModificationTime != 0) classicWorld.Add(new NbtLong("LastModified", (int) ModificationTime));
            if (AccessedTime != 0) classicWorld.Add(new NbtLong("LastAccessed", (int) AccessedTime));

            classicWorld.Add(new NbtByteArray("BlockArray", blocks));

            var mapGenerator = new NbtCompound("MapGenerator");
            mapGenerator.Add(new NbtString("Software", "MCarmada"));
            mapGenerator.Add(new NbtString("MapGeneratorName", WorldGenerator.Generators.FirstOrDefault(x => x.Value == generator).Key));
            classicWorld.Add(mapGenerator);

            var spawn = new NbtCompound("Spawn");
            BlockPos spawnPos = GetPlayerSpawn();
            spawn.Add(new NbtShort("X", (short) spawnPos.X));
            spawn.Add(new NbtShort("Y", (short) spawnPos.Y));
            spawn.Add(new NbtShort("Z", (short) spawnPos.Z));

            classicWorld.Add(spawn);

            var metadata = new NbtCompound("Metadata");

            var mcaData = new NbtCompound("MCarmada");
            mcaData.Add(new NbtLong("BlockArrayHash", (long)XXHash.XXH64(blocks)));
            mcaData.Add(new NbtLong("LevelTick", (long)LevelTick));
            mcaData.Add(new NbtInt("LevelSeed", Seed));
            mcaData.Add(new NbtString("SoftwareVersion", Assembly.GetCallingAssembly().GetName().Version.ToString()));
            mcaData.Add(new NbtString("GeneratorName", WorldGenerator.Generators.FirstOrDefault(x => x.Value == generator).Key));
            mcaData.Add(new NbtString("GeneratorClassName", generator.GetType().FullName));

            var ticks = new NbtList("ScheduledTicks", NbtTagType.Compound);
            foreach (var tick in scheduledTicks)
            {
                ticks.Add(tick.ToCompound());
            }

            var sp = new NbtCompound("Players");
            foreach (var player in savedPlayers)
            {
                sp.Add(player.ToCompound());
            }

            mcaData.Add(sp);
            mcaData.Add(ticks);

            var cpe = new NbtCompound("CPE");

            var customBlocks = new NbtCompound("CustomBlocks");
            customBlocks.Add(new NbtInt("ExtensionVersion", Server.Server.GetExtension(CpeExtension.CustomBlocks).Version));
            customBlocks.Add(new NbtShort("SupportLevel", 1));

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

            cpe.Add(customBlocks);

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
            }

            Level lvl = new Level(server, settings, width, depth, height, blocks, seed, generator, tick, ticks)
            {
                Guid = guid,
                CreationTime = creationTime,
                ModificationTime = modifyTime,
                AccessedTime = accessTime
            };

            return lvl;
        }
    }
}
