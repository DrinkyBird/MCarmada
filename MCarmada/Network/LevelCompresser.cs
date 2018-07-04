using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using MCarmada.Server;
using MCarmada.Utils;
using MCarmada.World;

namespace MCarmada.Network
{
    class LevelCompresser
    {
        private Level level;
        private Player player;

        public LevelCompresser(Level level, Player player)
        {
            this.level = level;
            this.player = player;
        }

        public void Run()
        {
            MemoryStream stream = new MemoryStream();
            GZipStream gzip = new GZipStream(stream, CompressionMode.Compress);
            BinaryWriter writer = new BinaryWriter(gzip);

            byte[] blocks = level.Blocks;

            writer.Write(IPAddress.HostToNetworkOrder((int) blocks.Length));
            writer.Write(blocks);

            writer.Dispose();
            gzip.Dispose();

            player.queuedLevelData = stream.GetBuffer();
            player.levelDataTime = TimeUtil.GetTimeInMs();

            stream.Dispose();
        }
    }
}
