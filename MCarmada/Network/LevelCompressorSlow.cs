using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using MCarmada.Server;
using MCarmada.World;

namespace MCarmada.Network
{
    class LevelCompressorSlow : ILevelCompressor
    {
        private Player player;
        private Level level;

        private bool gzipped = false;
        private bool finished = false;

        private MemoryStream stream;
        private GZipStream gzip;
        private BinaryWriter writer;

        private int dataOffset = 0;

        private byte[] data;

        public int Position { get; private set; }
        public int Length { get; private set; }
        private bool useCpeFallbacks = false;

        internal LevelCompressorSlow(Player player, Level level, bool useFallbacks)
        {
            this.player = player;
            this.level = level;
            useCpeFallbacks = useFallbacks;

            stream = new MemoryStream();
            gzip = new GZipStream(stream, CompressionMode.Compress);
            writer = new BinaryWriter(gzip);

            writer.Write((int)IPAddress.HostToNetworkOrder(level.Blocks.Length));
        }

        public void Process()
        {
            if (!gzipped)
            {
                int total = level.Blocks.Length;
                int length = Math.Min(32 * 1024, total - dataOffset);

                byte[] array = new byte[length];
                Array.Copy(level.Blocks, dataOffset, array, 0, array.Length);
                dataOffset += length;

                AddChunk(array);

                if (dataOffset == total)
                {
                    Flush();
                    gzipped = true;
                }
            }
            else
            {
                int length;
                for (int i = 0; i < 2; i++)
                {
                    byte[] chunk = GetChunk(out length);

                    Packet packet = new Packet(PacketType.Header.LevelChunk);
                    packet.Write((short)length);
                    packet.Write(chunk);
                    packet.Write((byte)0);
                    player.Send(packet);

                    Console.WriteLine(Position + "/" + data.Length);

                    if (Position >= Length)
                    {
                        finished = true;

                        Packet finish = new Packet(PacketType.Header.LevelFinish);
                        finish.Write((short)level.Width);
                        finish.Write((short)level.Depth);
                        finish.Write((short)level.Height);
                        player.Send(finish);

                        return;
                    }
                }
            }
        }

        public bool IsComplete()
        {
            return finished;
        }

        public void Dispose()
        {
        }

        private void AddChunk(byte[] chunk)
        {
            for (int i = 0; i < chunk.Length; i++)
            {
                Block block = (Block)chunk[i];
                byte b = chunk[i];

                if (b >= (byte)Block.CobblestoneSlab && b <= (byte)Block.StoneBricks && useCpeFallbacks)
                {
                    chunk[i] = (byte)BlockConfig.CpeFallbacks[block];
                }
            }

            writer.Write(chunk);
        }

        private byte[] GetChunk(out int length)
        {
            int len = Math.Min(1024, Length - Position);

            byte[] chunk = new byte[len];
            Array.Copy(data, Position, chunk, 0, chunk.Length);

            Position += len;

            length = len;
            return chunk;
        }

        private void Flush()
        {
            writer.Dispose();
            gzip.Dispose();

            byte[] buf = stream.GetBuffer();
            Length = buf.Length;
            data = new byte[Length];
            Array.Copy(buf, 0, data, 0, buf.Length);
            stream.Dispose();

            Position = 0;

            Console.WriteLine("Data length = " + data.Length);
        }
    }
}
