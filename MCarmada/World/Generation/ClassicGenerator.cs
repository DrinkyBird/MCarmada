using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MCarmada.Utils;
using MCarmada.World.Generation.Noise;
using NLog;

namespace MCarmada.World.Generation
{
    partial class ClassicGenerator : WorldGenerator
    {
        private Level level;
        private Logger logger = LogUtils.GetClassLogger();

        private int[] heightMap;

        public override void Generate(Level level)
        {
            this.level = level;

            GenerateHeightmap();
            GenerateStrata();
            GenerateCaves();

            GenerateOres(Block.GoldOre, 0.5); // Gold
            GenerateOres(Block.IronOre, 0.7); // Iron
            GenerateOres(Block.CoalOre, 0.9); // Coal

            GenerateWater();
            GenerateLava();
            GenerateSurface();
            GeneratePlants();
        }

        private void GenerateHeightmap()
        {
            logger.Info("Raising...");

            heightMap = new int[level.Width * level.Height];

            int waterLevel = level.Depth / 2;

            CombinedNoise noise1 = new CombinedNoise(new OctaveNoise(8, level.Rng), new OctaveNoise(8, level.Rng));
            CombinedNoise noise2 = new CombinedNoise(new OctaveNoise(8, level.Rng), new OctaveNoise(8, level.Rng));
            OctaveNoise noise3 = new OctaveNoise(6, level.Rng);

            int lastPercent = -1;
            int generated = 0;

            for (int x = 0; x < level.Width; x++)
            for (int z = 0; z < level.Height; z++)
            {
                double pc2 = (double)(level.Width * level.Height);
                int percent = (int)(((double) generated / pc2) * 100.0);

                if (percent != lastPercent)
                {
                    lastPercent = percent;

                    logger.Info("Raising... " + percent + "%");
                }

                double heightLow = noise1.Compute(x * 1.3, z * 1.3) / 6 - 4;
                double heightHigh = noise1.Compute(x * 1.3, z * 1.3) / 5 + 6;
                double heightResult;

                if (noise3.Compute(x, z) / 8 > 0)
                {
                    heightResult = heightLow;
                }
                else
                {
                    heightResult = Math.Max(heightLow, heightHigh);
                }

                heightResult /= 2;

                heightMap[x + z * level.Width] = (int) (heightResult + waterLevel);

                generated++;
            }
            logger.Info("Raised...");
        }

        private void GenerateStrata()
        {
            logger.Info("Soiling...");

            OctaveNoise noise = new OctaveNoise(8, level.Rng);

            int lastPercent = -1;
            int generated = 0;

            for (int x = 0; x < level.Width; x++)
            for (int z = 0; z < level.Height; z++)
            {
                double pc2 = (double)(level.Width * level.Height);
                int percent = (int)(((double)generated / pc2) * 100.0);

                if (percent != lastPercent)
                {
                    lastPercent = percent;

                    logger.Info("Soiling... " + percent + "%");
                }

                int dirtThickness = (int) noise.Compute(x, z) / 24 - 4;
                int dirtTransition = heightMap[x + z * level.Width];
                int stoneTransition = dirtTransition + dirtThickness;

                for (int y = 0; y < level.Depth; y++)
                {
                    Block block = Block.Air;

                    if (y == 0) block = Block.Lava;
                    else if (y <= stoneTransition) block = Block.Stone;
                    else if (y <= dirtTransition) block = Block.Dirt;

                    level.SetBlock(x, y, z, block);
                }

                generated++;
            }
        }

        private void GenerateCaves()
        {
            int numCaves = (level.Width * level.Depth * level.Height) / 8192;

            int lastPercent = -1;

            logger.Info("Carving... (" + numCaves + " caves)");

            for (int i = 0; i < numCaves; i++)
            {
                int percent = (int) (((double) i / numCaves) * 100.0);
                if (percent != lastPercent)
                {
                    logger.Info("Carving... " + percent + "%");
                    lastPercent = percent;
                }

                double caveX = level.Rng.Next(0, level.Width);
                double caveY = level.Rng.Next(0, level.Depth);
                double caveZ = level.Rng.Next(0, level.Height);

                int caveLength = (int) (level.Rng.NextDouble() * level.Rng.NextDouble() * 200);

                double theta = level.Rng.NextDouble() * Math.PI * 2;
                double deltaTheta = 0;

                double phi = level.Rng.NextDouble() * Math.PI * 2;
                double deltaPhi = 0;

                double caveRadius = level.Rng.NextDouble() * level.Rng.NextDouble();

                for (double len = 0; len < caveLength; len++)
                {
                    caveX += Math.Sin(theta) * Math.Cos(phi);
                    caveY += Math.Cos(theta) * Math.Cos(phi);
                    caveZ += Math.Sin(phi);

                    theta = theta + deltaTheta * 0.2;
                    deltaTheta = deltaTheta * 0.9 + level.Rng.NextDouble() - level.Rng.NextDouble();
                    phi = phi / 2 + deltaPhi / 4;
                    deltaPhi = deltaPhi * 0.75 + level.Rng.NextDouble() - level.Rng.NextDouble();

                    if (level.Rng.NextDouble() >= 0.25)
                    {
                        int centreX = (int) (caveX + (level.Rng.Next(4) - 2) * 0.2);
                        int centreY = (int) (caveY + (level.Rng.Next(4) - 2) * 0.2);
                        int centreZ = (int) (caveZ + (level.Rng.Next(4) - 2) * 0.2);

                        double radius = (level.Height - centreY) / (double) level.Height;
                        radius = 1.2 + (radius * 3.5 + 1) * caveRadius;
                        radius = radius * Math.Sin(len * Math.PI / caveLength);

                        FillOblateSpheroid(centreX, centreY, centreZ, radius, 0);
                    }
                }
            }
        }

        private void GenerateOres(Block block, double abundance)
        {
            int numVeins = (int) ((level.Width * level.Height * level.Depth * abundance) / 16384);

            for (int i = 0; i < numVeins; i++)
            {
                double veinX = level.Rng.Next(0, level.Width);
                double veinY = level.Rng.Next(0, level.Depth);
                double veinZ = level.Rng.Next(0, level.Height);

                double veinLength = level.Rng.NextDouble() * level.Rng.NextDouble() * 75 * abundance;

                double theta = level.Rng.NextDouble() * Math.PI * 2;
                double deltaTheta = 0;
                double phi = level.Rng.NextDouble() * Math.PI * 2;
                double deltaPhi = 0;

                for (double len = 0; len < veinLength; len++)
                {
                    veinX = veinX + Math.Sin(theta) * Math.Cos(phi);
                    veinY = veinY + Math.Cos(theta) * Math.Cos(phi);
                    veinZ = veinZ + Math.Cos(phi);

                    theta = deltaTheta * 0.2;
                    deltaTheta = (deltaTheta * 0.9) + level.Rng.NextDouble() - level.Rng.NextDouble();
                    phi = phi / 2 + deltaPhi / 4;
                    deltaPhi = (deltaPhi * 0.9) + level.Rng.NextDouble() - level.Rng.NextDouble();

                    double radius = abundance * Math.Sin(len * Math.PI / veinLength) + 1;

                    FillOblateSpheroid((int)veinX, (int)veinY, (int)veinZ, radius, block);
                }
            }
        }

        private void GenerateWater()
        {
            int waterLevel = (level.Depth / 2) - 1;
            int numSources = (level.Width * level.Height) / 800;
            int total = (level.Width * 2) + (level.Height * 2) + numSources;
            int done = 0;
            int percent = 0;
            int lastPercent = -1;

            for (int x = 0; x < level.Width; x++)
            {
                FloodFill(level.GetBlockIndex(x, waterLevel, 0), Block.Water);
                FloodFill(level.GetBlockIndex(x, waterLevel, level.Height - 1), Block.Water);

                done += 2;
                percent = (int)(((double)done / total) * 100.0);
                if (percent != lastPercent)
                {
                    logger.Info("Watering... " + percent + "%");
                    lastPercent = percent;
                }
            }

            for (int y = 0; y < level.Height; y ++)
            {
                FloodFill(level.GetBlockIndex(0, waterLevel, y), Block.Water);
                FloodFill(level.GetBlockIndex(level.Width - 1, waterLevel, y), Block.Water);

                done += 2;
                percent = (int)(((double)done / total) * 100.0);
                if (percent != lastPercent)
                {
                    logger.Info("Watering... " + percent + "%");
                    lastPercent = percent;
                }
            }

            for (int i = 0; i < numSources; i++)
            {
                int x = level.Rng.Next(0, level.Width);
                int z = level.Rng.Next(0, level.Height);
                int y = waterLevel - level.Rng.Next(0, 2);

                FloodFill(level.GetBlockIndex(x, y, z), Block.Water);

                done++;
                percent = (int)(((double)done / total) * 100.0);
                if (percent != lastPercent)
                {
                    logger.Info("Watering... " + percent + "%");
                    lastPercent = percent;
                }
            }
        }

        private void GenerateLava()
        {
            int waterLevel = (level.Depth / 2);
            int numSources = (level.Width * level.Height) / 20000;
            int done = 0;
            int lastPercent = -1;

            for (int i = 0; i < numSources; i++)
            {
                int x = level.Rng.Next(0, level.Width);
                int z = level.Rng.Next(0, level.Height);
                int y = (int) ((waterLevel - 3) * level.Rng.NextDouble() * level.Rng.NextDouble());

                FloodFill(level.GetBlockIndex(x, y, z), Block.Lava);

                done++;
                int percent = (int)(((double)done / numSources) * 100.0);
                if (percent != lastPercent)
                {
                    logger.Info("Melting... " + percent + "%");
                    lastPercent = percent;
                }
            }
        }

        private void GenerateSurface()
        {
            OctaveNoise noise1 = new OctaveNoise(8, level.Rng);
            OctaveNoise noise2 = new OctaveNoise(8, level.Rng);

            int done = 0;
            int lastPercent = -1;
            
            for (int x = 0; x < level.Width; x++)
            for (int z = 0; z < level.Height; z++)
            {
                bool sandChance = noise1.Compute(x, z) > 8;
                bool gravelChance = noise2.Compute(x, z) > 12;

                int y = heightMap[x + z * level.Width];
                Block above = level.GetBlock(x, y + 1, z);

                if (above == Block.Water && gravelChance)
                {
                    level.SetBlock(x, y, z, Block.Gravel);
                }

                else if (above == 0)
                {
                    if (y <= (level.Depth / 2) && sandChance)
                    {
                        level.SetBlock(x, y, z, Block.Sand);
                    }
                    else
                    {
                        level.SetBlock(x, y, z, Block.Grass);
                    }
                }

                done++;
                double pc2 = (double)(level.Width * level.Height);
                int percent = (int)(((double)done / pc2) * 100.0);
                if (percent != lastPercent) 
                { 
                    logger.Info("Growing... " + percent + "%");
                    lastPercent = percent;
                }
            }
        }

        private void GeneratePlants()
        {
            int numFlowers = (level.Width * level.Height) / 3000;
            int numShrooms = (level.Width * level.Height) / 2000;
            int numTrees = (level.Width * level.Height) / 4000;

            int total = numFlowers + numShrooms + numTrees;
            int done = 0;
            int lastPercent = -1;

            for (int i = 0; i < numFlowers; i++)
            {
                Block flowerType = level.Rng.Next(2) == 0 ? Block.Dandelion : Block.Rose;

                int patchX = level.Rng.Next(0, level.Width);
                int patchZ = level.Rng.Next(0, level.Height);

                for (int j = 0; j < 10; j++)
                {
                    int flowerX = patchX;
                    int flowerZ = patchZ;

                    for (int k = 0; k < 5; k++)
                    {
                        flowerX += level.Rng.Next(6) - level.Rng.Next(6);
                        flowerZ += level.Rng.Next(6) - level.Rng.Next(6);

                        if (level.IsValidBlock(flowerX, 0, flowerZ))
                        {
                            int flowerY = heightMap[flowerX + flowerZ * level.Width] + 1;
                            Block below = level.GetBlock(flowerX, flowerY - 1, flowerZ);

                            if (level.GetBlock(flowerX, flowerY, flowerZ) == 0 && below == Block.Grass)
                            {
                                level.SetBlock(flowerX, flowerY, flowerZ, flowerType);
                            }
                        }
                    }
                }

                done++;
                int percent = (int)(((double)done / total) * 100.0);
                if (percent != lastPercent)
                {
                    logger.Info("Planting... " + percent + "%");
                    lastPercent = percent;
                }
            }

            for (int i = 0; i < numShrooms; i++)
            {
                Block shroomType = level.Rng.Next(2) == 0 ? Block.BrownMushroom : Block.RedMushroom;

                int patchX = level.Rng.Next(0, level.Width);
                int patchY = level.Rng.Next(0, level.Depth);
                int patchZ = level.Rng.Next(0, level.Height);


                for (int j = 0; j < 20; j++)
                {
                    int shroomX = patchX;
                    int shroomY = patchY;
                    int shroomZ = patchZ;

                    for (int k = 0; k < 5; k++)
                    {
                        shroomX += level.Rng.Next(6) - level.Rng.Next(6);
                        shroomZ += level.Rng.Next(6) - level.Rng.Next(6);

                        if (level.IsValidBlock(shroomX, 0, shroomZ) && shroomY < heightMap[shroomX + shroomZ * level.Width] - 1)
                        {
                            Block below = level.GetBlock(shroomX, shroomY - 1, shroomZ);

                            if (level.GetBlock(shroomX, shroomY, shroomZ) == 0 && below == Block.Stone)
                            {
                                level.SetBlock(shroomX, shroomY, shroomZ, shroomType);
                            }
                        }
                    }
                }

                done++;
                int percent = (int)(((double)done / total) * 100.0);
                if (percent != lastPercent)
                {
                    logger.Info("Planting... " + percent + "%");
                    lastPercent = percent;
                }
            }

            for (int i = 0; i < numTrees; i++)
            {
                int patchX = level.Rng.Next(0, level.Width);
                int patchZ = level.Rng.Next(0, level.Height);

                for (int j = 0; j < 20; j++)
                {
                    int treeX = patchX;
                    int treeZ = patchZ;

                    for (int k = 0; k < 20; k++)
                    {
                        treeX += level.Rng.Next(6) - level.Rng.Next(6);
                        treeZ += level.Rng.Next(6) - level.Rng.Next(6);

                        if (level.IsValidBlock(treeX, 0, treeZ) && level.Rng.NextDouble() <= 0.25)
                        {
                            int treeY = heightMap[treeX + treeZ * level.Width] + 1;
                            int treeHeight = level.Rng.Next(1, 3) + 4;

                            if (IsSpaceForTree(treeX, treeY, treeZ, treeHeight))
                            {
                                GrowTree(treeX, treeY, treeZ, treeHeight);
                            }
                        }
                    }
                }

                done++;
                int percent = (int)(((double)done / total) * 100.0);
                if (percent != lastPercent)
                {
                    logger.Info("Planting... " + percent + "%");
                    lastPercent = percent;
                }
            }
        }
    }
}
