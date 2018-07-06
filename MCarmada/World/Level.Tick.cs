using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace MCarmada.World
{
    partial class Level
    {
        private static readonly int WATER_TICK_DELAY = 4;
        private static readonly int LAVA_TICK_DELAY = 10;

        public enum TickEvent
        {
            GrowTree,
            PlaceWater,
            PlaceLava,
            BlockFall
        }

        public struct ScheduledTick
        {
            public long Tick;
            public int X, Y, Z;
            public TickEvent Event;
        }

        private List<ScheduledTick> scheduledTicks = new List<ScheduledTick>();

        public void Tick()
        {
            for (int i = 0; i < scheduledTicks.Count; i++)
            {
                ScheduledTick tick = scheduledTicks[i];

                if (server.CurrentTick >= tick.Tick)
                {
                    PerformScheduledTick(tick);
                    scheduledTicks.RemoveAt(i);
                }
            }
        }

        private void AddScheduledTick(int x, int y, int z, int when, TickEvent tickEvent)
        {
            foreach (var scheduledTick in scheduledTicks)
            {
                if (scheduledTick.X == x && scheduledTick.Y == y && scheduledTick.Z == z)
                {
                    return;
                }
            }

            ScheduledTick tick = new ScheduledTick();
            tick.Tick = server.CurrentTick + when;
            tick.X = x;
            tick.Y = y;
            tick.Z = z;
            tick.Event = tickEvent;
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
                AddScheduledTick(x, y, z, Rng.Next(60 * 20, 180 * 20), TickEvent.GrowTree);
            }

            if ((north == Block.Water || east == Block.Water || south == Block.Water || west == Block.Water ||
                above == Block.Water) && block != Block.Water && settings.WaterFlow)
            {
                AddScheduledTick(x, y, z, WATER_TICK_DELAY, TickEvent.PlaceWater);
            }

            else if ((north == Block.Lava || east == Block.Lava || south == Block.Lava || west == Block.Lava ||
                above == Block.Lava) && block != Block.Lava && settings.LavaFlow)
            {
                AddScheduledTick(x, y, z, LAVA_TICK_DELAY, TickEvent.PlaceLava);
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
                    AddScheduledTick(x, y, z, Rng.Next(60 * 20, 180 * 20), TickEvent.GrowTree);
                }
            }

            else if (tick.Event == TickEvent.PlaceWater)
            {
                if ((north == Block.Water || east == Block.Water || south == Block.Water || west == Block.Water ||
                    above == Block.Water) && block == Block.Air)
                {
                    SetBlock(x, y, z, Block.Water);

                    if (northValid && north == Block.Air) AddScheduledTick(x, y, z + 1, WATER_TICK_DELAY, TickEvent.PlaceWater);
                    else if (northValid && (north == Block.Lava || north == Block.LavaStill)) SetBlock(x, y, z + 1, Block.Obsidian);

                    if (eastValid && east == Block.Air) AddScheduledTick(x + 1, y, z, WATER_TICK_DELAY, TickEvent.PlaceWater);
                    else if (eastValid && (east == Block.Lava || east == Block.LavaStill)) SetBlock(x + 1, y, z, Block.Obsidian);

                    if (southValid && south == Block.Air) AddScheduledTick(x, y, z - 1, WATER_TICK_DELAY, TickEvent.PlaceWater);
                    else if (southValid && (south == Block.Lava || south == Block.LavaStill)) SetBlock(x, y, z - 1, Block.Obsidian);

                    if (westValid && west == Block.Air) AddScheduledTick(x - 1, y, z, WATER_TICK_DELAY, TickEvent.PlaceWater);
                    else if (westValid && (west == Block.Lava || west == Block.LavaStill)) SetBlock(x - 1, y, z, Block.Obsidian);

                    if (belowValid && below == Block.Air) AddScheduledTick(x, y - 1, z, WATER_TICK_DELAY, TickEvent.PlaceWater);
                    else if (belowValid && (below == Block.Lava || below == Block.LavaStill)) SetBlock(x, y - 1, z, Block.Obsidian);
                }
            }

            else if (tick.Event == TickEvent.PlaceLava)
            {
                if ((north == Block.Lava || east == Block.Lava || south == Block.Lava || west == Block.Lava ||
                    above == Block.Lava) && block == Block.Air)
                {
                    SetBlock(x, y, z, Block.Lava);

                    if (northValid && north == Block.Air) AddScheduledTick(x, y, z + 1, LAVA_TICK_DELAY, TickEvent.PlaceLava);
                    else if (northValid && (north == Block.Water || north == Block.WaterStill)) SetBlock(x, y, z + 1, Block.Stone);

                    if (eastValid && east == Block.Air) AddScheduledTick(x + 1, y, z, LAVA_TICK_DELAY, TickEvent.PlaceLava);
                    else if (eastValid && (east == Block.WaterStill || east == Block.WaterStill)) SetBlock(x + 1, y, z, Block.Stone);

                    if (southValid && south == Block.Air) AddScheduledTick(x, y, z - 1, LAVA_TICK_DELAY, TickEvent.PlaceLava);
                    else if (southValid && (south == Block.Water || south == Block.WaterStill)) SetBlock(x, y, z - 1, Block.Stone);

                    if (westValid && west == Block.Air) AddScheduledTick(x - 1, y, z, LAVA_TICK_DELAY, TickEvent.PlaceLava);
                    else if (westValid && (west == Block.Water || west == Block.WaterStill)) SetBlock(x - 1, y, z, Block.Stone);

                    if (belowValid && below == Block.Air) AddScheduledTick(x, y - 1, z, LAVA_TICK_DELAY, TickEvent.PlaceLava);
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
