using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.World.Generation
{
    class DebugGenerator : WorldGenerator
    {
        public override void Generate(Level level)
        {
            int width = level.Width;
            int depth = level.Depth;
            int height = level.Height;

            int doff = 0;

            for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
            {
                level.SetBlock(x, doff + 1, z, Block.Stone);
                level.SetBlock(x, 0, z, Block.Bedrock);
            }
            
            for (int z = 0; z < height; z++)
            {
                level.SetBlock((width / 2), doff + 1, z, Block.BlueWool);
                level.SetBlock((width / 2) - 1, doff + 1, z, Block.BlueWool);
            }

            for (int x = 0; x < width; x++)
            {
                level.SetBlock(x, doff + 1, height / 2, Block.RedWool);
                level.SetBlock(x, doff + 1, (height / 2) - 1, Block.RedWool);
            }

            level.SetBlock(width / 2, doff + 1, height / 2, Block.MagentaWool);
            level.SetBlock(width / 2 - 1, doff + 1, height / 2, Block.MagentaWool);
            level.SetBlock(width / 2 - 1, doff + 1, height / 2 - 1, Block.MagentaWool);
            level.SetBlock(width / 2, doff + 1, height / 2 - 1, Block.MagentaWool);

            for (int x = 0; x < Enum.GetNames(typeof(Block)).Length; x++)
            for (int z = 0; z < height; z++) 
            {
                level.SetBlock(x, doff + 2, z, (Block) x);
            }

            for (int x = width / 2 + 1; x < width; x++)
            for (int z = height / 2 + 1; z < height; z++)
            {
                level.SetBlock(x, doff + 1, z, Block.Water);
            }

            for (int x = width / 2 + 1; x < width; x++)
            for (int z = height / 2 - 2; z > -1; z--)
            {
                level.SetBlock(x, doff + 1, z, Block.Lava);
            }

            level.SetBlock(width / 2 + 1, doff + 1, height / 2 + 1, Block.Water);
            level.SetBlock(width / 2 + 1, doff + 1, height / 2 - 2, Block.Lava);
        }
    }
}
