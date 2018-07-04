using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using MCarmada.Utils;
using MCarmada.World;

namespace MCarmada.Network
{
    class Packet : IDisposable
    {
        private MemoryStream buffer;
        private BinaryWriter writer;
        private BinaryReader reader;

        public PacketType.Header Type { get; private set; }

        public Packet(PacketType.Header header)
        {
            buffer = new MemoryStream(PacketType.GetPacketSize(header) + 1);
            writer = new BinaryWriter(buffer);
            reader = new BinaryReader(buffer);

            Type = header;

            writer.Write((byte) header);
        }

        public Packet(byte[] bytes)
        {
            buffer = new MemoryStream(bytes);

            writer = new BinaryWriter(buffer);
            reader = new BinaryReader(buffer);

            Type = (PacketType.Header) reader.ReadByte();
        }

        public void Dispose()
        {
            if (buffer != null) buffer.Dispose();
            if (writer != null) writer.Dispose();
            if (reader != null) reader.Dispose();
        }

        public Packet Write(byte value)
        {
            writer.Write(value);
            return this;
        }

        public Packet Write(sbyte value)
        {
            writer.Write(value);
            return this;
        }

        public Packet Write(short value)
        {
            writer.Write(IPAddress.HostToNetworkOrder(value));
            return this;
        }

        public Packet Write(int value)
        {
            writer.Write(IPAddress.HostToNetworkOrder(value));
            return this;
        }

        public Packet Write(string value)
        {
            int len = Math.Min(value.Length, 64);
            int spaces = 64 - len;

            byte[] chars = Encoding.ASCII.GetBytes(value);
            writer.Write(chars, 0, len);

            for (int i = 0; i < spaces; i++)
            {
                writer.Write((byte) 0x20);
            }

            return this;
        }

        public Packet Write(byte[] value)
        {
            int len = Math.Min(value.Length, 1024);
            int nulls = 1024 - len;

            writer.Write(value, 0, len);

            for (int i = 0; i < nulls; i++)
            {
                writer.Write((byte) 0x00);
            }

            return this;
        }

        public Packet WriteFixedPointPos(float x, float y, float z)
        {
            Write(FixedPoint.ToFixedPoint(x));
            Write(FixedPoint.ToFixedPoint(y));
            Write(FixedPoint.ToFixedPoint(z));

            return this;
        }

        public Packet WriteFixedPointPos(BlockPos pos)
        {
            return WriteFixedPointPos(pos.X, pos.Y, pos.Z);
        }

        public byte ReadByte()
        {
            return reader.ReadByte();
        }

        public sbyte ReadSByte()
        {
            return reader.ReadSByte();
        }

        public short ReadShort()
        {
            return IPAddress.NetworkToHostOrder(reader.ReadInt16());
        }

        public int ReadInt()
        {
            return IPAddress.NetworkToHostOrder(reader.ReadInt32());
        }

        public string ReadString()
        {
            byte[] bytes = reader.ReadBytes(64);
            string s = Encoding.ASCII.GetString(bytes);
            return s.Trim();
        }

        public byte[] ReadByteArray()
        {
            return reader.ReadBytes(1024);
        }

        public byte[] GetBytes()
        {
            long oldPos = buffer.Position;
            buffer.Position = 0;
            
            byte[] data = new byte[buffer.Length];
            buffer.Read(data, 0, data.Length);

            buffer.Position = oldPos;

            return data;
        }

        public int GetLength()
        {
            return (int) buffer.Length;
        }
    }
}
