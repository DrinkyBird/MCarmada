using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Extensions.Data;
using MCarmada.Network;
using MCarmada.Utils;
using MCarmada.World.Generation;
using NLog;

namespace MCarmada.World
{
    class Level
    {
        public static readonly int LEVEL_VERSION = 1;

        public short Width { get; private set; }
        public short Depth { get; private set; }
        public short Height { get; private set; }

        public Block[] Blocks;
        public Random Rng { get; private set; }
        public int Seed { get; private set; }

        private WorldGenerator generator;

        private Server.Server server;
        private Logger logger = LogUtils.GetClassLogger();

        public Level(Server.Server server, Settings.WorldSettings settings, short w,  short d, short h)
        {
            this.server = server;
            Width = w;
            Depth = d;
            Height = h;

            Seed = (int) DateTime.Now.Ticks;
            logger.Info("Creating world with seed " + Seed + "...");
            Rng = new Random(Seed);

            generator = WorldGenerator.Generators[settings.Generator];

            Init();
        }

        private void Init()
        {
            Blocks = new Block[Width * Height * Depth];
            for (int i = 0; i < Blocks.Length; i++)
            {
                Blocks[i] = (byte) 0;
            }

            Generate();
        }

        private void Generate()
        {
            double start = TimeUtil.GetTimeInMs();
            generator.Generate(this);
            double end = TimeUtil.GetTimeInMs();
            double delta = end - start;

            logger.Info("Generated world in " + delta + " ms.");
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

            Packet packet = new Packet(PacketType.Header.ServerSetBlock);
            packet.Write((short) x);
            packet.Write((short) y);
            packet.Write((short) z);
            packet.Write(block);
            server.BroadcastPacket(packet);

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

            int y = 256;

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
            Directory.CreateDirectory(outDir);

            string metadataFile = Path.Combine(outDir, "world.mcarmada");
            string blocksFile = Path.Combine(outDir, "blocks.mcarmada");

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
