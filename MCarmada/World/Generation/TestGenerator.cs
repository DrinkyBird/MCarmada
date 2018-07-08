using System;
using System.Collections.Generic;
using MCarmada.World.Generation.Noise;

namespace MCarmada.World.Generation
{
    class TestGenerator : WorldGenerator
    {
        private int[] heightMap;
        private Level level;

        public override void Generate(Level level)
        {
            this.level = level;
            int width = level.Width;
            int depth = level.Depth;
            int height = level.Height;

            heightMap = new int[level.Width * level.Height];
            int waterLevel = level.Depth / 2;

            CombinedNoise noise1 = new CombinedNoise(new OctaveNoise(8, level.Rng), new OctaveNoise(8, level.Rng));
            CombinedNoise noise2 = new CombinedNoise(new OctaveNoise(8, level.Rng), new OctaveNoise(8, level.Rng));
            OctaveNoise noise3 = new OctaveNoise(8, level.Rng);

            for (int x = 0; x < level.Width; x++)
            for (int z = 0; z < level.Height; z++)
            {
                double heightLow = noise1.Compute(x * 1.3, z * 1.3) / 6 - 4;
                double heightHigh = noise2.Compute(x * 1.3, z * 1.3) / 5 + 6;
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

                int hmVal = (int) (heightResult + waterLevel);
                heightMap[x + z * width] = hmVal;
                for (int y = 0; y < hmVal; y++)
                {
                    Block b = Block.Air;

                    if (y == hmVal - 1) b = Block.Grass;
                    else b = Block.Dirt;
                    level.SetBlock(x, y, z, b);
                }
            }

            GenerateStrata();
            GenerateWater();
            GenerateLava();
            GenerateSurface();

            OpenSimplex noise4 = new OpenSimplex((int) (level.Depth * 0.8), 1.0, 2.0, 16, level.Seed);
            OpenSimplex noise5 = new OpenSimplex((int)(level.Depth * 0.8), 1.0, 3.0, 16, level.Seed);

            for (int x = 0; x < level.Width; x++)
            for (int z = 0; z < level.Height; z++)
            {
                for (int y = depth; y > heightMap[x + z * width] - 1; y--)
                {
                    double val1 = noise4.noise(x, y, z);
                    double val2 = noise5.noise(x, y, z);

                    double val = Math.Max(val1, val2);

                    if (val > 0.85 && heightMap[x + z * width] > waterLevel)
                    {
                        level.SetBlock(x, y, z, Block.PinkWool);
                    }
                }
            }

            Grassify();
            GenerateCaves();

            GenerateOres(Block.GoldOre, 0.5);
            GenerateOres(Block.IronOre, 0.7);
            GenerateOres(Block.CoalOre, 0.9);
            GenerateOres(Block.Dirt, 2.0);
            GenerateOres(Block.Gravel, 1.5);

            GeneratePlants();
        }

        private void Grassify()
        {
            int width = level.Width;
            int depth = level.Depth;
            int height = level.Height;

            for (int x = 0; x < level.Width; x++)
            for (int z = 0; z < level.Height; z++)
            {
                var list = new List<Tuple<int, int>>();

                int begin = -1;
                int end = -1;

                for (int y = depth; y > heightMap[x + z * width]; y--)
                {
                    Block block = level.GetBlock(x, y, z);
                    if (block == Block.PinkWool && begin == -1)
                    {
                        begin = y;
                    } 
                    else if (block == Block.Air && end == -1)
                    {
                        end = y;
                        list.Add(new Tuple<int, int>(begin, end));
                        begin = -1;
                        end = -1;

                    }
                    else if (y == heightMap[x + z * width] + 1 && end == -1)
                    {
                        end = heightMap[x + z * width];
                        list.Add(new Tuple<int, int>(begin, 0));
                        begin = -1;
                        end = -1;
                    }
                }

                for (int i = 0; i < list.Count; i++)
                {
                    Tuple<int, int> pair = list[i];
                    begin = pair.Item1;
                    end = pair.Item2;

                    for (int y = begin; y > end; y--)
                    {
                        Block b = Block.Air;
                        if (y == begin)
                        {
                            b = Block.Grass;
                        } 
                        else if (y >= begin - 3)
                        {
                            b = Block.Dirt;
                        } 
                        else
                        {
                            b = Block.Stone;
                        }

                        level.SetBlock(x, y, z, b);
                    }
                }
            }
        }



        private void GenerateStrata()
        {
            OctaveNoise noise = new OctaveNoise(8, level.Rng);

            for (int x = 0; x < level.Width; x++)
            for (int z = 0; z < level.Height; z++)
            {
                int dirtThickness = (int)noise.Compute(x, z) / 24 - 4;
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
            }
        }

        private void GenerateCaves()
        {
            int numCaves = (level.Width * level.Depth * level.Height) / 8192;

            for (int i = 0; i < numCaves; i++)
            {
                double caveX = level.Rng.Next(0, level.Width);
                double caveY = level.Rng.Next(0, level.Depth);
                double caveZ = level.Rng.Next(0, level.Height);

                int caveLength = (int)(level.Rng.NextDouble() * level.Rng.NextDouble() * 200);

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
                        int centreX = (int)(caveX + (level.Rng.Next(4) - 2) * 0.2);
                        int centreY = (int)(caveY + (level.Rng.Next(4) - 2) * 0.2);
                        int centreZ = (int)(caveZ + (level.Rng.Next(4) - 2) * 0.2);

                        double radius = (level.Height - centreY) / (double)level.Height;
                        radius = 1.2 + (radius * 3.5 + 1) * caveRadius;
                        radius = radius * Math.Sin(len * Math.PI / caveLength);

                        FillOblateSpheroid(centreX, centreY, centreZ, radius, 0);
                    }
                }
            }
        }

        private void GenerateOres(Block block, double abundance)
        {
            int numVeins = (int)((level.Width * level.Height * level.Depth * abundance) / 16384);

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

        private void GenerateSurface()
        {
            OctaveNoise noise1 = new OctaveNoise(8, level.Rng);
            OctaveNoise noise2 = new OctaveNoise(8, level.Rng);

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
                    else if (above != Block.Water)
                    {
                        level.SetBlock(x, y, z, Block.Grass);
                    }
                }
            }
        }
        private void GenerateWater()
        {
            int waterLevel = (level.Depth / 2) - 1;
            int numSources = (level.Width * level.Height) / 800;
            int total = (level.Width * 2) + (level.Height * 2) + numSources;

            for (int x = 0; x < level.Width; x++)
            {
                FloodFill(level.GetBlockIndex(x, waterLevel, 0), Block.Water);
                FloodFill(level.GetBlockIndex(x, waterLevel, level.Height - 1), Block.Water);
            }

            for (int y = 0; y < level.Height; y++)
            {
                FloodFill(level.GetBlockIndex(0, waterLevel, y), Block.Water);
                FloodFill(level.GetBlockIndex(level.Width - 1, waterLevel, y), Block.Water);
            }

            for (int i = 0; i < numSources; i++)
            {
                int x = level.Rng.Next(0, level.Width);
                int z = level.Rng.Next(0, level.Height);
                int y = waterLevel - level.Rng.Next(0, 2);

                FloodFill(level.GetBlockIndex(x, y, z), Block.Water);
            }
        }

        private void GenerateLava()
        {
            int waterLevel = (level.Depth / 2);
            int numSources = (level.Width * level.Height) / 20000;

            for (int i = 0; i < numSources; i++)
            {
                int x = level.Rng.Next(0, level.Width);
                int z = level.Rng.Next(0, level.Height);
                int y = (int)((waterLevel - 3) * level.Rng.NextDouble() * level.Rng.NextDouble());

                FloodFill(level.GetBlockIndex(x, y, z), Block.Lava);
            }
        }


        private void GeneratePlants()
        {
            int numFlowers = (level.Width * level.Height) / 1000;
            int numShrooms = (level.Width * level.Height) / 2000;
            int numTrees = (level.Width * level.Height) / 2000;

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
                            flowerY = level.FindLitY(flowerX, flowerY, flowerZ);
                            Block below = level.GetBlock(flowerX, flowerY - 1, flowerZ);

                            if (level.GetBlock(flowerX, flowerY, flowerZ) == 0 && below == Block.Grass)
                            {
                                level.SetBlock(flowerX, flowerY, flowerZ, flowerType);
                            }
                        }
                    }
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
                            treeY = level.FindLitY(treeX, treeY, treeZ);
                            int treeHeight = level.Rng.Next(1, 3) + 4;

                            if (level.IsSpaceForTree(treeX, treeY, treeZ, treeHeight))
                            {
                                level.GrowTree(treeX, treeY, treeZ, treeHeight);
                            }
                        }
                    }
                }
            }
        }

        private void FillOblateSpheroid(int centreX, int centreY, int centreZ, double radius, Block block)
	    {
	        int xStart = (int)Math.Floor(Math.Max(centreX - radius, 0));
	        int xEnd = (int)Math.Floor(Math.Min(centreX + radius, level.Width - 1));
	        int yStart = (int)Math.Floor(Math.Max(centreY - radius, 0));
	        int yEnd = (int)Math.Floor(Math.Min(centreY + radius, level.Depth - 1));
	        int zStart = (int)Math.Floor(Math.Max(centreZ - radius, 0));
	        int zEnd = (int)Math.Floor(Math.Min(centreZ + radius, level.Height - 1));

	        for (double x = xStart; x < xEnd; x++)
	        for (double y = yStart; y < yEnd; y++)
	        for (double z = zStart; z < zEnd; z++)
	        {
	            double dx = x - centreX;
	            double dy = y - centreY;
	            double dz = z - centreZ;

	            int ix = (int)x;
	            int iy = (int)y;
	            int iz = (int)z;

	            if ((dx * dx + 2 * dy * dy + dz * dz) < (radius * radius) &&
	                level.IsValidBlock(ix, iy, iz))
	            {
	                if (level.GetBlock(ix, iy, iz) == Block.Stone)
	                {
	                    level.SetBlock(ix, iy, iz, block);
	                }
	            }
	        }
	    }


        private void FloodFill(int startIndex, Block block)
        {
            int oneY = level.Width * level.Height;

            if (startIndex < 0) return; // y below map, immediately ignore
            ClassicGenerator.FastIntStack stack = new ClassicGenerator.FastIntStack(4);
            stack.Push(startIndex);

            while (stack.Size > 0)
            {
                int index = stack.Pop();
                if (index < 0 || index >= level.Blocks.Length) continue;

                if (level.Blocks[index] != 0) continue;
                level.Blocks[index] = block;

                int x = index % level.Width;
                int y = index / oneY;
                int z = (index / level.Width) % level.Height;

                if (x > 0) stack.Push(index - 1);
                if (x < level.Width - 1) stack.Push(index + 1);
                if (z > 0) stack.Push(index - level.Width);
                if (z < level.Height - 1) stack.Push(index + level.Width);
                if (y > 0) stack.Push(index - oneY);
            }
        }
    }
}
