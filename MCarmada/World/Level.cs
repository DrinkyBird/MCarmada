using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Extensions.Data;
using MCarmada.Api;
using MCarmada.Network;
using MCarmada.Utils;
using MCarmada.World.Generation;
using NLog;

namespace MCarmada.World
{
    partial class Level : ITickable
    {
        public static readonly int LEVEL_VERSION = 1;

        public short Width { get; private set; }
        public short Depth { get; private set; }
        public short Height { get; private set; }

        public Block[] Blocks;
        public Random Rng { get; private set; }
        public int Seed { get; private set; }
        public bool Generated { get; private set; }

        private WorldGenerator generator;

        private Server.Server server;
        private static Logger logger = LogUtils.GetClassLogger();

        private Settings.WorldSettings settings;

        public ulong LevelTick { get; private set; }

        public Level(Server.Server server, Settings.WorldSettings settings, short w,  short d, short h)
        {
            this.settings = settings;
            this.server = server;
            Width = w;
            Depth = d;
            Height = h;

            Seed = settings.Seed;
            if (Seed == 0) Seed = (int)DateTime.Now.Ticks;
            logger.Info("Creating world with seed " + Seed + "...");
            Rng = new Random(Seed);

            generator = WorldGenerator.Generators[settings.Generator];

            Init();
        }

        private Level(Server.Server server, Settings.WorldSettings settings, short w, short d, short h, Block[] blocks, int seed, string generator, ulong tick, List<ScheduledTick> ticks)
        {
            this.settings = settings;
            this.server = server;
            Width = w;
            Depth = d;
            Height = h;
            Blocks = blocks;
            Seed = seed;
            LevelTick = tick;
            scheduledTicks = ticks;

            Generated = true;

            Rng = new Random(Seed);
            this.generator = WorldGenerator.Generators[generator];
        }

        private void Init()
        {
            Blocks = new Block[Width * Height * Depth];
            for (int i = 0; i < Blocks.Length; i++)
            {
                Blocks[i] = (byte) 0;
            }

            Generated = false;
            Generate();
        }

        private void Generate()
        {
            double start = TimeUtil.GetTimeInMs();
            generator.Generate(this);
            double end = TimeUtil.GetTimeInMs();
            double delta = end - start;

            logger.Info("Generated world in " + delta + " ms.");
            Generated = true;
        }

        public bool IsValidBlock(int x, int y, int z)
        {
            return !(x < 0 || y < 0 || z < 0 || x >= Width || y >= Depth || z >= Height);
        }

        public int GetBlockIndex(int x, int y, int z)
        {
            return (y * Height + z) * Width + x;
        }

        public bool SetBlock(int x, int y, int z, Block block)
        {
            if (!IsValidBlock(x, y, z))
            {
                return false;
            }

            Blocks[(y * Height + z) * Width + x] = block;

            ScheduleBlockTick(x, y, z);

            if (Generated)
            {
                server.BroadcastBlockChange(x, y, z, block);
            }

            return true;
        }

        public Block GetBlock(int x, int y, int z)
        {
            if (!IsValidBlock(x, y, z))
            {
                return 0;
            }

            return Blocks[(y * Height + z) * Width + x];
        }

        public BlockPos GetPlayerSpawn()
        {
            int radius = Rng.Next(0, 10);
            int xc = Width / 2;
            int zc = Height / 2;

            int x = Rng.Next(xc - radius, xc + radius);
            int z = Rng.Next(zc - radius, zc + radius);

            int y = FindTopBlock(x, z) + 2;

            return new BlockPos(x, y, z);
        }

        public int FindTopBlock(int x, int z)
        {
            int y = Depth;

            while (GetBlock(x, y, z) == 0)
            {
                y--;
            }

            return y;
        }

        public void Save(string outDir)
        {
            if (!settings.EnableSave)
            {
                return;
            }

            logger.Info("Saving world to " + outDir);
            Directory.CreateDirectory(outDir);

            string metadataFile = Path.Combine(outDir, "world.mcarmada");
            string blocksFile = Path.Combine(outDir, "blocks.mcarmada");
            string ticksFile = Path.Combine(outDir, "ticks.mcarmada");

            byte[] blocks = BlocksAsByteArray();

            FileStream stream;
            GZipStream gzip;
            BinaryWriter writer;

            // 1. write metadata
            stream = new FileStream(metadataFile, FileMode.Create);
            writer = new BinaryWriter(stream);
            
            writer.Write(new[] {'M', 'C', 'a', 'W'});
            writer.Write(LEVEL_VERSION);

            writer.Write(Width);
            writer.Write(Depth);
            writer.Write(Height);
            writer.Write(Seed);
            writer.Write(generator.GetType().FullName);
            writer.Write(WorldGenerator.Generators.FirstOrDefault(x => x.Value == generator).Key);
            writer.Write(LevelTick);
            writer.Write(scheduledTicks.Count);

            writer.Dispose();
            stream.Dispose();
            
            // 2. write blocks

            stream = new FileStream(blocksFile, FileMode.Create);
            gzip = new GZipStream(stream, CompressionMode.Compress);
            writer = new BinaryWriter(gzip);

            writer.Write(LEVEL_VERSION);
            writer.Write(Blocks.Length);
            writer.Write(XXHash.XXH64(blocks));
            writer.Write(blocks);

            writer.Dispose();
            gzip.Dispose();
            stream.Dispose();

            // 3. write ticks

            stream = new FileStream(ticksFile, FileMode.Create);
            writer = new BinaryWriter(stream);

            writer.Write(LEVEL_VERSION);

            foreach (var tick in scheduledTicks)
            {
                tick.Write(writer);
            }

            writer.Dispose();
            stream.Dispose();
        }

        public static Level Load(Server.Server server, Settings.WorldSettings settings, string dir)
        {
            logger.Info("Loading world from " + dir + "...");

            string metadataFile = Path.Combine(dir, "world.mcarmada");
            string blocksFile = Path.Combine(dir, "blocks.mcarmada");
            string ticksFile = Path.Combine(dir, "ticks.mcarmada");

            FileStream stream;
            GZipStream gzip;
            BinaryReader reader;

            stream = new FileStream(metadataFile, FileMode.Open);
            reader = new BinaryReader(stream);

            char a = reader.ReadChar();
            char b = reader.ReadChar();
            char c = reader.ReadChar();
            char d = reader.ReadChar();

            if (a != 'M' || b != 'C' || c != 'a' || d != 'W')
            {
                throw new InvalidDataException("File header does not match");
            }

            int version = reader.ReadInt32();
            if (version != LEVEL_VERSION)
            {
                throw new InvalidDataException("File version is unsupported");
            }

            short width = reader.ReadInt16();
            short depth = reader.ReadInt16();
            short height = reader.ReadInt16();
            int seed = reader.ReadInt32();
            string generatorClass = reader.ReadString();
            string generatorName = reader.ReadString();
            ulong tick = reader.ReadUInt64();
            int numTicks = reader.ReadInt32();

            reader.Dispose();
            stream.Dispose();

            stream = new FileStream(blocksFile, FileMode.Open);
            gzip = new GZipStream(stream, CompressionMode.Decompress);
            reader = new BinaryReader(gzip);

            int bversion = reader.ReadInt32();
            if (bversion != LEVEL_VERSION)
            {
                throw new InvalidDataException("Blocks f version is unsupported");
            }

            int length = reader.ReadInt32();
            ulong hash = reader.ReadUInt64();

            byte[] bytes = new byte[length];
            Block[] blocks = new Block[length];
            for (int i = 0; i < length; i++)
            {
                byte byt = reader.ReadByte();
                bytes[i] = byt;
                blocks[i] = (Block) byt;
            }

            if (XXHash.XXH64(bytes) != hash)
            {
                throw new InvalidDataException("Blocks hash does not match");
            }

            reader.Dispose();
            gzip.Dispose();
            stream.Dispose();

            stream = new FileStream(ticksFile, FileMode.Open);
            reader = new BinaryReader(stream);

            version = reader.ReadInt32();

            if (version != LEVEL_VERSION)
            {
                throw new InvalidDataException("Blocks f version is unsupported");
            }

            List<ScheduledTick> ticks = new List<ScheduledTick>();
            for (int i = 0; i < numTicks; i++)
            {
                ticks.Add(ScheduledTick.Read(reader));
            }

            reader.Dispose();
            stream.Dispose();

            Level level = new Level(server, settings, width, depth, height, blocks, seed, generatorName, tick, ticks);
            logger.Info("World loaded!");

            return level;
        }

        public byte[] BlocksAsByteArray()
        {
            byte[] output = new byte[Blocks.Length];

            for (int i = 0; i < Blocks.Length; i++)
            {
                output[i] = (byte) Blocks[i];
            }

            return output;
        }
    }
}
