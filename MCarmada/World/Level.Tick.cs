using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace MCarmada.World
{
    partial class Level
    {
        private static readonly ulong WATER_TICK_DELAY = 4;
        private static readonly ulong LAVA_TICK_DELAY = 10;

        public enum TickEvent
        {
            GrowTree,
            PlaceWater,
            PlaceLava,
            BlockFall
        }

        public enum TickTiming
        {
            Absolute,
            Modulus
        }

        public struct ScheduledTick
        {
            public ulong Tick;
            public int X, Y, Z;
            public TickEvent Event;
            public TickTiming Timing;
            public ulong TimeAdded;

            public void Write(BinaryWriter writer)
            {
                writer.Write(Tick);
                writer.Write(X);
                writer.Write(Y);
                writer.Write(Z);
                writer.Write((byte) Event);
                writer.Write((byte) Timing);
                writer.Write(TimeAdded);
            }

            public static ScheduledTick Read(BinaryReader reader)
            {
                ScheduledTick t = new ScheduledTick();
                t.Tick = reader.ReadUInt64();
                t.X = reader.ReadInt32();
                t.Y = reader.ReadInt32();
                t.Z = reader.ReadInt32();
                t.Event = (TickEvent) reader.ReadByte();
                t.Timing = (TickTiming) reader.ReadByte();
                t.TimeAdded = reader.ReadUInt64();
                return t;
            }
        }

        private List<ScheduledTick> scheduledTicks = new List<ScheduledTick>();

        public void Tick()
        {
            for (int i = 0; i < scheduledTicks.Count; i++)
            {
                ScheduledTick tick = scheduledTicks[i];

                bool doTick = false;
                if (tick.Timing == TickTiming.Absolute) doTick = (LevelTick >= tick.Tick);
                else if (tick.Timing == TickTiming.Modulus) doTick = (LevelTick % tick.Tick == 0 && LevelTick > tick.TimeAdded);

                if (doTick)
                {
                    PerformScheduledTick(tick);
                    scheduledTicks.RemoveAt(i);
                }
            }

            LevelTick++;
        }

        private void AddScheduledTick(int x, int y, int z, ulong when, TickEvent tickEvent, TickTiming timing = TickTiming.Absolute)
        {
            foreach (var scheduledTick in scheduledTicks)
            {
                if (scheduledTick.X == x && scheduledTick.Y == y && scheduledTick.Z == z)
                {
                    return;
                }
            }

            ulong time = when;
            if (timing == TickTiming.Absolute)
            {
                time += server.CurrentTick;
            }

            ScheduledTick tick = new ScheduledTick();
            tick.Tick = time;
            tick.X = x;
            tick.Y = y;
            tick.Z = z;
            tick.Event = tickEvent;
            tick.Timing = timing;
            tick.TimeAdded = server.CurrentTick;
            scheduledTicks.Add(tick);
        }

        private void ScheduleBlockTick(int x, int y, int z)
        {
            Block block = GetBlock(x, y, z);
            Block north = GetBlock(x, y, z + 1);
            Block east = GetBlock(x + 1, y, z);
            Block south = GetBlock(x, y, z - 1);
            Block west = GetBlock(x - 1, y, z);
            Block above = GetBlock(x, y + 1, z);
            Block below = GetBlock(x, y - 1, z);

            if (block == Block.Sapling && settings.GrowTrees)
            {
                AddScheduledTick(x, y, z, (ulong) Rng.Next(60 * 20, 180 * 20), TickEvent.GrowTree);
            }

            if ((north == Block.Water || east == Block.Water || south == Block.Water || west == Block.Water ||
                above == Block.Water) && block != Block.Water && settings.WaterFlow)
            {
                AddScheduledTick(x, y, z, WATER_TICK_DELAY, TickEvent.PlaceWater, TickTiming.Modulus);
            }

            else if ((north == Block.Lava || east == Block.Lava || south == Block.Lava || west == Block.Lava ||
                above == Block.Lava) && block != Block.Lava && settings.LavaFlow)
            {
                AddScheduledTick(x, y, z, LAVA_TICK_DELAY, TickEvent.PlaceLava, TickTiming.Modulus);
            }

            else if (block == Block.Water)
            {
                AddScheduledTick(x, y, z, 1, TickEvent.PlaceWater, TickTiming.Absolute);
            }

            else if (block == Block.Lava)
            {
                AddScheduledTick(x, y, z, 1, TickEvent.PlaceLava, TickTiming.Absolute);
            }

            else if (CanBlockFall(block))
            {
                AddScheduledTick(x, y, z, 1, TickEvent.BlockFall);
            }

            if (CanBlockFall(above))
            {
                AddScheduledTick(x, y + 1, z, 1, TickEvent.BlockFall);
            }
        }

        private void PerformScheduledTick(ScheduledTick tick)
        {
            int x = tick.X;
            int y = tick.Y;
            int z = tick.Z;

            Block block = GetBlock(x, y, z);
            Block north = GetBlock(x, y, z + 1);
            Block east = GetBlock(x + 1, y, z);
            Block south = GetBlock(x, y, z - 1);
            Block west = GetBlock(x - 1, y, z);
            Block above = GetBlock(x, y + 1, z);
            Block below = GetBlock(x, y - 1, z);

            bool northValid = IsValidBlock(x, y, z + 1);
            bool eastValid = IsValidBlock(x + 1, y, z);
            bool southValid = IsValidBlock(x, y, z - 1);
            bool westValid = IsValidBlock(x - 1, y, z);
            bool aboveValid = IsValidBlock(x, y + 1, z);
            bool belowValid = IsValidBlock(x, y - 1, z);

            if (tick.Event == TickEvent.GrowTree)
            {
                int treeHeight = Rng.Next(1, 3) + 4;
                if (IsSpaceForTree(x, y, z, treeHeight))
                {
                    GrowTree(x, y, z, treeHeight);
                }
                else
                {
                    AddScheduledTick(x, y, z, (ulong) Rng.Next(60 * 20, 180 * 20), TickEvent.GrowTree);
                }
            }

            else if (tick.Event == TickEvent.PlaceWater)
            {
                if (((north == Block.Water || east == Block.Water || south == Block.Water || west == Block.Water ||
                    above == Block.Water) && CanReplaceWithLiquid(block)) || block == Block.Water)
                {
                    if (block != Block.Water) SetBlock(x, y, z, Block.Water);

                    if (northValid && CanReplaceWithLiquid(north)) AddScheduledTick(x, y, z + 1, WATER_TICK_DELAY, TickEvent.PlaceWater, TickTiming.Modulus);
                    else if (northValid && (north == Block.Lava || north == Block.LavaStill)) SetBlock(x, y, z + 1, Block.Obsidian);

                    if (eastValid && CanReplaceWithLiquid(east)) AddScheduledTick(x + 1, y, z, WATER_TICK_DELAY, TickEvent.PlaceWater, TickTiming.Modulus);
                    else if (eastValid && (east == Block.Lava || east == Block.LavaStill)) SetBlock(x + 1, y, z, Block.Obsidian);

                    if (southValid && CanReplaceWithLiquid(south)) AddScheduledTick(x, y, z - 1, WATER_TICK_DELAY, TickEvent.PlaceWater, TickTiming.Modulus);
                    else if (southValid && (south == Block.Lava || south == Block.LavaStill)) SetBlock(x, y, z - 1, Block.Obsidian);

                    if (westValid && CanReplaceWithLiquid(west)) AddScheduledTick(x - 1, y, z, WATER_TICK_DELAY, TickEvent.PlaceWater, TickTiming.Modulus);
                    else if (westValid && (west == Block.Lava || west == Block.LavaStill)) SetBlock(x - 1, y, z, Block.Obsidian);

                    if (belowValid && CanReplaceWithLiquid(below)) AddScheduledTick(x, y - 1, z, WATER_TICK_DELAY, TickEvent.PlaceWater, TickTiming.Modulus);
                    else if (belowValid && (below == Block.Lava || below == Block.LavaStill)) SetBlock(x, y - 1, z, Block.Obsidian);
                }
            }

            else if (tick.Event == TickEvent.PlaceLava)
            {
                if (((north == Block.Lava || east == Block.Lava || south == Block.Lava || west == Block.Lava ||
                    above == Block.Lava || block == Block.Lava) && CanReplaceWithLiquid(block)) || block == Block.Lava)
                {
                    if (block != Block.Lava) SetBlock(x, y, z, Block.Lava);

                    if (northValid && CanReplaceWithLiquid(north)) AddScheduledTick(x, y, z + 1, LAVA_TICK_DELAY, TickEvent.PlaceLava, TickTiming.Modulus);
                    else if (northValid && (north == Block.Water || north == Block.WaterStill)) SetBlock(x, y, z + 1, Block.Stone);

                    if (eastValid && CanReplaceWithLiquid(east)) AddScheduledTick(x + 1, y, z, LAVA_TICK_DELAY, TickEvent.PlaceLava, TickTiming.Modulus);
                    else if (eastValid && (east == Block.WaterStill || east == Block.WaterStill)) SetBlock(x + 1, y, z, Block.Stone);

                    if (southValid && CanReplaceWithLiquid(south)) AddScheduledTick(x, y, z - 1, LAVA_TICK_DELAY, TickEvent.PlaceLava, TickTiming.Modulus);
                    else if (southValid && (south == Block.Water || south == Block.WaterStill)) SetBlock(x, y, z - 1, Block.Stone);

                    if (westValid && CanReplaceWithLiquid(west)) AddScheduledTick(x - 1, y, z, LAVA_TICK_DELAY, TickEvent.PlaceLava, TickTiming.Modulus);
                    else if (westValid && (west == Block.Water || west == Block.WaterStill)) SetBlock(x - 1, y, z, Block.Stone);

                    if (belowValid && CanReplaceWithLiquid(below)) AddScheduledTick(x, y - 1, z, LAVA_TICK_DELAY, TickEvent.PlaceLava, TickTiming.Modulus);
                    else if (belowValid && (below == Block.Water || below == Block.WaterStill)) SetBlock(x, y - 1, z, Block.Stone);
                }
            }

            else if (tick.Event == TickEvent.BlockFall)
            {
                int yy = DoBlockFall(x, y, z);

                SetBlock(x, y, z, Block.Air);
                SetBlock(x, yy, z, block);
            }
        }
    }
}
