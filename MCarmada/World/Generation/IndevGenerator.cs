using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.Utils.Maths;
using MCarmada.World.Generation.Noise;

namespace MCarmada.World.Generation
{
    class IndevGenerator : WorldGenerator
    {
        private NoiseOctavesIndev noiseGen1;
        private NoiseOctavesIndev noiseGen2;
        private NoiseOctavesIndev noiseGen3;
        private NoiseOctavesIndev noiseGen4;
        private NoiseOctavesIndev noiseGen5;
        private NoiseOctavesIndev noiseGen6;
        private NoiseOctavesIndev noiseGen10;
        private NoiseOctavesIndev noiseGen11;
        private NoisePerlinIndev perlinGen1;

        private Level level;

        private bool paradise = false;
        private bool hell = false;
        private bool woods = false;
        private bool island = false;
        private bool floating = true;

        public override void Generate(Level level)
        {
            this.level = level;

            Init();

            if (floating)
            {
                GenFloating(Math.Max(1, this.level.Depth / 64));
            } 
            else
            {
                DoGen();
            }

            SetEnv();

            GenerateCaves();

            GenerateOres(Block.GoldOre, 0.5); // Gold
            GenerateOres(Block.IronOre, 0.7); // Iron
            GenerateOres(Block.CoalOre, 0.9); // Coal

            GeneratePlants();
        }

        private void Init()
        {
            noiseGen1 = new NoiseOctavesIndev(level.Rng, 16);
            noiseGen2 = new NoiseOctavesIndev(level.Rng, 16);
            noiseGen3 = new NoiseOctavesIndev(level.Rng, 8);
            noiseGen4 = new NoiseOctavesIndev(level.Rng, 4);
            noiseGen5 = new NoiseOctavesIndev(level.Rng, 4);
            noiseGen6 = new NoiseOctavesIndev(level.Rng, 5);
            noiseGen10 = new NoiseOctavesIndev(level.Rng, 6);
            noiseGen11 = new NoiseOctavesIndev(level.Rng, 8);
            perlinGen1 = new NoisePerlinIndev(level.Rng);
        }

        private void DoGen()
        {
            int height = level.Height;
            int seaLevel = level.Depth / 2;
            
            for (int x = 0; x < level.Width; x++)
            for (int z = 0; z < level.Height; z++)
            {
                int n = x / 1024;
                int i1 = z / 1024;

                int i2 = 64;

                if (island)
                {
                    float xc = level.Width / 2f;
                    float zc = level.Height / 2f;
                    float size = 3.0f * ((level.Width + level.Height) / 256.0f);

                    float f2 = (float)this.noiseGen5.Noise(x / 4.0F, z / 4.0F);
                    i2 = 74 - ((int)Math.Floor(Math.Sqrt((xc - x) * (xc - x) + (zc - z) * (zc - z)) / size));
                    if (i2 < 50) { i2 = 50; }
                    i2 += ((int)f2);
                }
                else
                {
                    float f1 = (float) (noiseGen1.Noise(x / 0.03125f, 0, z / 0.03125f) -
                                        noiseGen2.Noise(x / 0.015625f, 0, z / 0.015625f)) / 512f / 4.0f;
                    float f2 = (float) noiseGen5.Noise(x / 4.0f, z / 4.0f);
                    float f3 = (float) noiseGen6.Noise(x / 8.0f, z / 8.0f) / 8.0f;
                    f2 = f2 > 0.0F ? (float)(noiseGen3.Noise(x * 0.2571428f * 2.0f, z * 0.2571428f * 2.0f) * f3 / 4.0) : (float)(noiseGen4.Noise(x * 0.2571428f, z * 0.2571428f) * f3);
                    i2 = (int)(f1 + 64.0f + f2);
                }

                if ((float) noiseGen5.Noise(x, z) < 0f)
                {
                    i2 = i2 / 2 << 1;

                    if ((float) noiseGen5.Noise(x / 5.0f, z / 5f) < 0.0f)
                    {
                        i2++;
                    }
                }

                bool flagSand = noiseGen3.Noise(x, z) > 8.0;
                bool flagGravel = noiseGen11.Noise(x, z) > 18D;

                if (paradise)
                {
                    flagSand = noiseGen3.Noise(x, z) > -32.0;
                } 
                else if (hell || woods)
                {
                    flagSand = noiseGen3.Noise(x, z) > -8.0;
                }

                for (int y = 0; y < level.Depth; y++)
                {
                    Block i4 = Block.Air;

                    int beachHeight = seaLevel + 1;

                    if (paradise)
                    {
                        beachHeight = seaLevel + 3;
                    }

                    if (y == 0)
                    {
                        i4 = Block.Bedrock;
                    }

                    else if (y == i2 && i2 >= beachHeight)
                    {
                        i4 = hell ? Block.Dirt : Block.Grass;
                    }

                    else if (y == i2)
                    {
                        if (flagGravel)
                        {
                            i4 = hell ? Block.Grass : Block.Gravel;
                        } 
                        else if (flagSand)
                        {
                            i4 = hell ? Block.Grass : Block.Sand;
                        }
                        else if (i2 > seaLevel - 1)
                        {
                            i4 = Block.Grass;
                        }
                        else
                        {
                            i4 = Block.Dirt;
                        }
                    }

                    else if (y <= i2 - 2)
                    {
                        i4 = Block.Stone;
                    }

                    else if (y < i2)
                    {
                        i4 = Block.Dirt;
                    }

                    else if (y < 64)
                    {
                        if (hell)
                        {
                            i4 = Block.Lava;
                        }
                        else
                        {
                            i4 = Block.Water;
                        }
                    }

                    level.SetBlock(x, y, z, i4);
                }
            }
        }

        private void GenFloating(int layers = 3)
        {
            int seaLevel = 64;

            for (int layer = 0; layer < layers; layer++)
            {
                for (int x = 0; x < level.Width; x++)
                for (int z = 0; z < level.Height; z++)
                {
                    float f2 = (float) noiseGen5.Noise((x + (layer * 2000f)) / 4.0f, (z + (layer * 2000f)) / 4.0f);
                    int i2 = 35 + (layer * 45) + ((int) f2);

                    if (i2 < 1)
                    {
                        i2 = 1;
                    }

                    if ((float) noiseGen5.Noise(x, z) < 0f)
                    {
                        i2 = i2 / 2 << 1;

                        if (noiseGen5.Noise(x / 5.0, z / 5.0) < 0)
                        {
                            i2++;
                        }
                    }

                    float xc = level.Width / 2f;
                    float zc = level.Height / 2f;

                    double w = 2.25 * ((level.Width + level.Height) / 512.0);
                    int thickness = -25;
                    int less = (int) Math.Floor(Math.Sqrt((x - xc) * (x - xc) + (z - zc) * (z - zc)) / w);
                    if (less > 150)
                    {
                        less = 150;
                    }

                    thickness += less;

                    double ovar32 = Clamp(GetNoise(8, x + (layer * 2000), z + (layer * 2000), 50, 50, 0));
                    int var77 = (int) (ovar32 * (seaLevel / 2f)) + 20 + (layer * 45) + thickness;

                    bool flagSand = noiseGen3.Noise(x + (layer * 2000f), z + (layer * 2000f)) > 52.0 + (less / 3.0);
                    bool flagGravel = noiseGen11.Noise(x + (layer * 2000f), z + (layer * 2000f)) > 62.0 + (less / 3.0);

                    for (int y = 0; y < level.Depth; y++)
                    {
                        Block b = Block.Air;

                        if (y == i2)
                        {
                            if (flagGravel)
                            {
                                b = Block.Gravel;
                            }
                            else if (flagSand)
                            {
                                b = Block.Sand;
                            } 
                            else if (y > var77)
                            {
                                b = Block.Stone;
                            }
                        }
                        else if (y > var77 && y < i2)
                        {
                            b = Block.Stone;
                        }

                        if (b != Block.Air)
                        {
                            level.SetBlock(x, y, z, b);
                        }
                    }
                }
            }

            for (int x = 0; x < level.Width; x++)
            for (int z = 0; z < level.Height; z++)
            {
                int t = -1;
                bool air = true;

                for (int y = 255; y > -1; y--)
                {
                    Block b = Block.Air;
                    Block cb = level.GetBlock(x, y, z);

                    if (cb == Block.Air)
                    {
                        b = Block.Air;
                        t = -1;
                    }
                    else if (cb == Block.Stone)
                    {
                        t++;
                        if (t == 0 && air)
                        {
                            b = Block.Grass;
                        } 
                        else if (t < 3)
                        {
                            b = Block.Dirt;
                        }
                        else
                        {
                            b = Block.Stone;
                        }

                        air = false;
                    }
                    else
                    {
                        t++;
                        b = cb;
                    }

                    level.SetBlock(x, y, z, b);
                }
            }

            for (int x = 0; x < level.Width; x++)
            for (int z = 0; z < level.Height; z++)
            {
                Block cb = level.GetBlock(x, 0, z);
                if (cb == Block.Air)
                {
                    level.SetBlock(x, 0, z, Block.Water);
                }
            }
        }

        private void SetEnv()
        {
            if (hell)
            {
                byte skyLvl = (byte) ((3.0f / 15.0f) * 256.0f);
                byte shdLvl = (byte) ((2.0f / 15.0f) * 256.0f);

                level.SkyColour.R = 48;
                level.SkyColour.G = 0;
                level.SkyColour.B = 0;

                level.CloudColour.R = 16;
                level.CloudColour.G = 0;
                level.CloudColour.B = 0;

                level.FogColour.R = 48;
                level.FogColour.G = 0;
                level.FogColour.B = 0;

                level.AmbientColour.R = shdLvl;
                level.AmbientColour.G = shdLvl;
                level.AmbientColour.B = shdLvl;

                level.DiffuseColour.R = skyLvl;
                level.DiffuseColour.G = skyLvl;
                level.DiffuseColour.B = skyLvl;

                level.EdgeWaterBlock = Block.LavaStill;
            }
            else if (floating)
            {
                level.EdgeWaterBlock = Block.Water;
                level.EdgeSideBlock = Block.Bedrock;
                level.EdgeHeight = 1;
                level.EdgeDistance = 2;
                level.CloudHeight = (int)(level.Depth * 0.75);
            }
        }

        private void GenerateCaves()
        {
            int numCaves = (level.Width * level.Depth * level.Height) / 8192;

            int lastPercent = -1;

            for (int i = 0; i < numCaves; i++)
            {
                int percent = (int)(((double)i / numCaves) * 100.0);
                if (percent != lastPercent)
                {
                    lastPercent = percent;
                }

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

        private void GeneratePlants()
        {
            int numFlowers = (level.Width * level.Height) / 3000;
            int numShrooms = (level.Width * level.Depth * level.Height) / 2000;
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
                            int flowerY = level.FindTopBlock(flowerX, flowerZ);
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

                        if (level.IsValidBlock(shroomX, 0, shroomZ))
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
                            int treeY = level.FindTopBlock(treeX, treeZ) + 1;
                            int treeHeight = level.Rng.Next(1, 3) + 4;

                            if (level.IsSpaceForTree(treeX, treeY, treeZ, treeHeight))
                            {
                                level.GrowTree(treeX, treeY, treeZ, treeHeight);
                            }
                        }
                    }
                }

                done++;
                int percent = (int)(((double)done / total) * 100.0);
                if (percent != lastPercent)
                {
                    lastPercent = percent;
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

        private double Clamp(double input)
        {
            if (input > 1.0D)
            {
                return 1.0D;
            }

            if (input < -1.0D)
            {
                return -1.0D;
            }

            return input;
        }

        private double GetNoise(int level, int x, int y, double xfact, double yfact, double zstart)
        {
            double output = 0;
            for (double l = 1; l <= level * level; l *= 2)
            {
                output += perlinGen1.Noise((x / xfact) * l, (y / yfact) * l) / l;
            }
            return output;
        }
    }
}
