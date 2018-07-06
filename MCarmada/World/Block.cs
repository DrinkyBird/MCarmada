using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.World
{
    public enum Block : byte
    {
        Air,
        Stone,
        Grass,
        Dirt,
        Cobblestone,
        Wood,
        Sapling,
        Bedrock,
        Water,
        WaterStill,
        Lava,
        LavaStill,
        Sand,
        Gravel,
        GoldOre,
        IronOre,
        CoalOre,
        Log,
        Leaves,
        Sponge,
        Glass,
        RedWool,
        OrangeWool,
        YellowWool,
        LimeWool,
        GreenWool,
        AquaGreenWool,
        CyanWool,
        BlueWool,
        PurpleWool,
        IndigoWool,
        VioletWool,
        MagentaWool,
        PinkWool,
        BlackWool,
        GreyWool,
        WhiteWool,
        Dandelion,
        Rose,
        BrownMushroom,
        RedMushroom,
        GoldBlock,
        IronBlock,
        DoubleSlab,
        Slab,
        Bricks,
        Tnt,
        Bookshelf,
        MossyCobblestoe,
        Obsidian,

        // CPE CustomBlocks support level 1
        CobblestoneSlab,
        Rope,
        Sandstone,
        Snow,
        Fire,
        LightPinkWool,
        ForestGreenWool,
        BrownWool,
        DeepBlue,
        Turquoise,
        Ice,
        CeramicTile,
        Magma,
        Pillar,
        Crate,
        StoneBricks
    }

    public class BlockConfig
    {
        private BlockConfig()
        {

        }

        public static readonly Dictionary<Block, Block> CpeFallbacks = new Dictionary<Block, Block>();

        static BlockConfig()
        {
            CpeFallbacks[Block.CobblestoneSlab] = Block.Slab;
            CpeFallbacks[Block.Rope] = Block.BrownMushroom;
            CpeFallbacks[Block.Sandstone] = Block.Sand;
            CpeFallbacks[Block.Snow] = Block.Air;
            CpeFallbacks[Block.Fire] = Block.Lava;
            CpeFallbacks[Block.LightPinkWool] = Block.PinkWool;
            CpeFallbacks[Block.ForestGreenWool] = Block.GreenWool;
            CpeFallbacks[Block.BrownWool] = Block.Dirt;
            CpeFallbacks[Block.DeepBlue] = Block.BlueWool;
            CpeFallbacks[Block.Turquoise] = Block.CyanWool;
            CpeFallbacks[Block.Ice] = Block.Glass;
            CpeFallbacks[Block.CeramicTile] = Block.IronBlock;
            CpeFallbacks[Block.Magma] = Block.Obsidian;
            CpeFallbacks[Block.Pillar] = Block.WhiteWool;
            CpeFallbacks[Block.Crate] = Block.Wood;
            CpeFallbacks[Block.StoneBricks] = Block.Stone;
        }
    }
}
