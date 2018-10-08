using System;
using System.IO;
using Ionic.Zlib;
using MCarmada.Server;
using MCarmada.World;

namespace MCarmada.Network
{
    class LevelCompressorFast : ILevelCompressor
    {
        private Player player;
        private Level level;

        private MemoryStream stream;
        private DeflateStream deflate;

        private int offset = 0;
        private byte[] data;

        private byte[] stub = new byte[32 * 1024];

        private long defOffset = 0;
        private long pakOffset = 0;

        internal LevelCompressorFast(Player player, Level level, bool useFallbacks)
        {
            this.player = player;
            this.level = level;

            data = new byte[level.Blocks.Length];

            stream = new MemoryStream();
            deflate = new DeflateStream(stream, CompressionMode.Compress, CompressionLevel.Default)
            {
                FlushMode = FlushType.Sync
            };
        }

        public void Process()
        {
            for (int i = 0; i < 4; i++)
            {
                if (offset <= level.Blocks.Length)
                {
                    stream.Seek(defOffset, SeekOrigin.Begin);
                    long initial = stream.Position;
                    int length;
                    byte[] chunk = GetChunk(out length);

                    deflate.Write(chunk, 0, length);

                    defOffset = stream.Position;
                }

                deflate.Flush();
                stream.Flush();
            }

            if (offset != level.Blocks.Length && stream.Length - pakOffset <= 32 * 1024)
            {
                while (stream.Position < stream.Length)
                {
                    int l = Math.Min(stub.Length, (int) (stream.Length - stream.Position));
                    stream.Read(stub, 0, l);
                }
                return;
            }

            stream.Seek(pakOffset, SeekOrigin.Begin);

            while (stream.Position < stream.Length)
            {
                int len = Math.Min(1024, (int)(stream.Length - stream.Position));
                byte[] written = new byte[len];
                stream.Read(written, 0, len);

                Packet p = new Packet(PacketType.Header.LevelChunk);
                p.Write((short)len);
                p.Write(written);
                p.Write((byte)0);
                player.Send(p);
            }

            pakOffset = stream.Position;

            if (IsComplete())
            {
                Packet finish = new Packet(PacketType.Header.LevelFinish);
                finish.Write((short)level.Width);
                finish.Write((short)level.Depth);
                finish.Write((short)level.Height);
                player.Send(finish);
            }
        }

        public bool IsComplete()
        {
            return (offset == level.Blocks.Length && stream.Position == stream.Length);
        }

        public void Dispose()
        {
            if (deflate != null) deflate.Dispose();
            if (stream != null) stream.Dispose();
        }

        private byte[] GetChunk(out int length)
        {
            int total = level.Blocks.Length;
            int len = Math.Min(32 * 1024, total - offset);

            byte[] chunk = new byte[len];
            Array.Copy(level.Blocks, offset, chunk, 0, chunk.Length);

            offset += len;

            length = len;
            return chunk;
        }
    }
}
