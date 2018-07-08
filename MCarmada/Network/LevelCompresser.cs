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
        private MemoryStream stream;
        private GZipStream gzip;
        private BinaryWriter writer;

        private byte[] data;

        public int Position { get; private set; }
        public int Length { get; private set; }
        public bool UseCpeFallbacks = false;

        public LevelCompresser(Level level)
        {
            stream = new MemoryStream();
            gzip = new GZipStream(stream, CompressionMode.Compress, false);
            writer = new BinaryWriter(gzip);

            writer.Write((int) IPAddress.HostToNetworkOrder(level.Blocks.Length));
        }

        public void AddChunk(byte[] chunk)
        {
            for (int i = 0; i < chunk.Length; i++)
            {
                Block block = (Block) chunk[i];
                byte b = chunk[i];

                if (b >= (byte) Block.CobblestoneSlab && b <= (byte) Block.StoneBricks && UseCpeFallbacks)
                {
                    chunk[i] = (byte) BlockConfig.CpeFallbacks[block];
                }
            }

            writer.Write(chunk);
        }

        public void Flush()
        {
            writer.Dispose();
            gzip.Dispose();

            byte[] buf = stream.GetBuffer();
            Length = buf.Length;
            data = new byte[Length];
            Array.Copy(buf, 0, data, 0, buf.Length);
            stream.Dispose();

            Position = 0;
        }

        public byte[] GetChunk(out int length)
        {
            int len = Math.Min(1024, Length - Position);

            byte[] chunk = new byte[len];
            Array.Copy(data, Position, chunk, 0, chunk.Length);

            Position += len;

            length = len;
            return chunk;
        }
    }
}
