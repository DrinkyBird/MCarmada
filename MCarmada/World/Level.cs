using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using Extensions.Data;
using fNbt;
using MCarmada.Api;
using MCarmada.Network;
using MCarmada.Server;
using MCarmada.Utils;
using MCarmada.World.Generation;
using NLog;

namespace MCarmada.World
{
    public partial class Level : ITickable
    {
        public static readonly byte CLASSICWORLD_VERSION = (byte) 1;

        public string Name { get; private set; }

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

        public Guid Guid { get; private set; }

        public int CreationTime { get; private set; }
        public int ModificationTime { get; private set; }
        public int AccessedTime { get; private set; }

        public Level(Server.Server server, Settings.WorldSettings settings, short w,  short d, short h)
        {
            this.settings = settings;
            this.server = server;
            Name = settings.Name;
            Width = w;
            Depth = d;
            Height = h;

            CreationTime = TimeUtil.GetUnixTime();

            Seed = settings.Seed;
            if (Seed == 0) Seed = (int)DateTime.Now.Ticks;
            logger.Info("Creating world with seed " + Seed + "...");
            Rng = new Random(Seed);

            generator = WorldGenerator.Generators[settings.Generator];

            Init();

            server.PluginManager.OnLevelLoaded(this);
        }

        private Level(Server.Server server, Settings.WorldSettings settings, short w, short d, short h, Block[] blocks, int seed, string generator, ulong tick, List<ScheduledTick> ticks)
        {
            this.settings = settings;
            this.server = server;
            Name = settings.Name;
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

            server.PluginManager.OnLevelLoaded(this);
        }

        private void Init()
        {
            Blocks = new Block[Width * Height * Depth];
            for (int i = 0; i < Blocks.Length; i++)
            {
                Blocks[i] = Block.Air;
            }

            Guid = Guid.NewGuid();
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
            ModificationTime = TimeUtil.GetUnixTime();

            if (Generated)
            {
                ScheduleBlockTick(x, y, z);
                server.BroadcastBlockChange(x, y, z, block);
            }

            server.PluginManager.OnLevelBlockChange(this, x, y, z, block);

            return true;
        }

        /// <summary>
        /// Intended for player-changed blocks
        /// </summary>
        internal bool ChangeBlock(int x, int y, int z, Block block, Player changer)
        {
            if (!IsValidBlock(x, y, z))
            {
                return false;
            }

            Block former = GetBlock(x, y, z);
            Block below = GetBlock(x, y - 1, z);
            if (BlockConfig.IsBlockSlab(block) && BlockConfig.IsBlockSlab(below))
            {
                Packet reset = new Packet(PacketType.Header.ServerSetBlock);
                reset.Write((short) x);
                reset.Write((short) y);
                reset.Write((short) z);
                reset.Write(former);
                changer.Send(reset);

                SetBlock(x, y - 1, z, BlockConfig.GetFullSlabType(block));

                return true;
            }

            SetBlock(x, y, z, block);

            return true;
        }

        public Block GetBlock(int x, int y, int z)
        {
            if (!IsValidBlock(x, y, z))
            {
                return 0;
            }

            AccessedTime = TimeUtil.GetUnixTime();
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
