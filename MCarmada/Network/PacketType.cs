using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.Network
{
    public class PacketType
    {
        public enum Header
        {
            // Standard client -> server
            PlayerIdent = 0x00,
            PlayerSetBlock = 0x05,
            PlayerPosition = 0x08,
            Message = 0x0d,

            // Standard server-> client
            ServerIdent = 0x00,
            Ping = 0x01,
            LevelInit = 0x02,
            LevelChunk = 0x03,
            LevelFinish = 0x04,
            ServerSetBlock = 0x06,
            SpawnPlayer = 0x07,
            Teleport = 0x08,
            PlayerUpdatePos = 0x09,
            PlayerPositionUpdate = 0x0a,
            PlayerRotationUpdate = 0x0b,
            DespawnPlayer = 0x0c,
            DisconnectPlayer = 0x0e,
            SetUserType = 0x0f,

            // CPE negotiation
            CpeExtInfo = 0x10,
            CpeExtEntry = 0x11,

            // CPE ClickDistance
            CpeClickDistance = 0x12,

            // CPE CustomBlocks
            CpeCustomBlockSupportLevel = 0x13,

            // CPE EnvColors
            CpeEnvSetColor = 0x19,

            // CPE SelectionCuboid
            MakeSelection = 0x1A,
            RemoveSelection = 0x1B,

            // CPE EnvWeatherType
            CpeEnvWeatherSetType = 0x1F,
        }

        private static Dictionary<Header, int> PacketSizes = new Dictionary<Header, int>();

        static PacketType()
        {
            // Standard client -> server
            PacketSizes[Header.PlayerIdent] = 1 + 64 + 64 + 1;
            PacketSizes[Header.PlayerSetBlock] = 2 + 2 + 2 + 1 + 1;
            PacketSizes[Header.PlayerPosition] = 1 + 2 + 2 + 2 + 1 + 1;
            PacketSizes[Header.Message] = 1 + 64;

            // Standard server-> client
            PacketSizes[Header.ServerIdent] = 1 + 64 + 64 + 1;
            PacketSizes[Header.Ping] = 0;
            PacketSizes[Header.LevelInit] = 0;
            PacketSizes[Header.LevelChunk] = 2 + 1024 + 1;
            PacketSizes[Header.LevelFinish] = 2 + 2 + 2;
            PacketSizes[Header.ServerSetBlock] = 2 + 2 + 2 + 1;
            PacketSizes[Header.SpawnPlayer] = 1 + 64 + 2 + 2 + 2 + 1 + 1;
            PacketSizes[Header.Teleport] = 1 + 2 + 2 + 2 + 1 + 1;
            PacketSizes[Header.PlayerUpdatePos] = 1 + 1 + 1 + 1 + 1 + 1;
            PacketSizes[Header.PlayerPositionUpdate] = 1 + 1 + 1 + 1;
            PacketSizes[Header.PlayerRotationUpdate] = 1 + 1 + 1;
            PacketSizes[Header.DespawnPlayer] = 1;
            PacketSizes[Header.DisconnectPlayer] =  64;
            PacketSizes[Header.SetUserType] = 1;

            // CPE negotiation
            PacketSizes[Header.CpeExtInfo] = 66;
            PacketSizes[Header.CpeExtEntry] = 68;

            // CPE ClickDistance
            PacketSizes[Header.CpeClickDistance] = 2;

            // CPE CustomBlocks
            PacketSizes[Header.CpeCustomBlockSupportLevel] = 1;

            // CPE EnvColors
            PacketSizes[Header.CpeEnvSetColor] = 7;

            // CPE SelectionCuboid
            PacketSizes[Header.MakeSelection] = 85;
            PacketSizes[Header.RemoveSelection] = 1;

            // CPE EnvWeatherType
            PacketSizes[Header.CpeEnvWeatherSetType] = 1;
        }

        public static int GetPacketSize(Header packet)
        {
            return PacketSizes[packet];
        }
    }
}
