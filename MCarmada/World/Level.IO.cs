using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Extensions.Data;
using fNbt;
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
            classicWorld.Add(new NbtShort("X", Width));
            classicWorld.Add(new NbtShort("Y", Depth));
            classicWorld.Add(new NbtShort("Z", Height));
            classicWorld.Add(new NbtByteArray("UUID", Guid.ToByteArray()));

            classicWorld.Add(new NbtByteArray("BlockArray", blocks));

            var spawn = new NbtCompound("Spawn");
            spawn.Add(new NbtShort("X", (short)(Width / 2)));
            spawn.Add(new NbtShort("Y", (short)(Depth / 2)));
            spawn.Add(new NbtShort("Z", (short)(Height / 2)));

            classicWorld.Add(spawn);

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

            classicWorld.Add(mcaData);

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

            if (root.Contains("MCarmada"))
            {
                NbtCompound mca = root.Get<NbtCompound>("MCarmada");

                ulong hash = (ulong) mca["BlockArrayHash"].LongValue;

                if (XXHash.XXH64(blockBytes) != hash)
                {
                    throw new InvalidDataException("Blocks hash mismatch!");
                }

                generator = mca["GeneratorName"].StringValue;
                seed = mca["LevelSeed"].IntValue;
                tick = (ulong) mca["LevelTick"].LongValue;

                NbtList tickTags = mca.Get<NbtList>("ScheduledTicks");
                for (int i = 0; i < tickTags.Count; i++)
                {
                    NbtCompound compound = tickTags.Get<NbtCompound>(i);

                    ScheduledTick st = new ScheduledTick()
                    {
                        X = compound["X"].IntValue,
                        Y = compound["Y"].IntValue,
                        Z = compound["Z"].IntValue,
                        Timing = (TickTiming) compound["Timing"].ByteValue,
                        Event = (TickEvent) compound["Event"].ByteValue,
                        Tick = (ulong) compound["When"].LongValue
                    };

                    ticks.Add(st);
                }
            }

            Level lvl = new Level(server, settings, width, depth, height, blocks, seed, generator, tick, ticks);
            return lvl;
        }
    }
}
