using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.Cpe
{
    public class CpeExtension
    {
        public static readonly string ClickDistance = "ClickDistance";
        public static readonly string CustomBlocks = "CustomBlocks";
        public static readonly string HeldBlock = "HeldBlock";
        public static readonly string EmoteFix = "EmoteFix";
        public static readonly string TextHotKey = "TextHotKey";
        public static readonly string ExtPlayerList = "ExtPlayerList";
        public static readonly string EnvColors = "EnvColors";
        public static readonly string SelectionCuboid = "SelectionCuboid";
        public static readonly string BlockPermissions = "BlockPermissions";
        public static readonly string ChangeModel = "ChangeModel";
        public static readonly string EnvMapAppearance = "EnvMapAppearance";
        public static readonly string EnvWeatherType = "EnvWeatherType";
        public static readonly string HackControl = "HackControl";
        public static readonly string MessageTypes = "MessageTypes";
        public static readonly string PlayerClick = "PlayerClick";
        public static readonly string LongerMessages = "LongerMessages";
        public static readonly string FullCp437 = "FullCP437";
        public static readonly string BlockDefinitions = "BlockDefinitions";
        public static readonly string BlockDefinitionsExt = "BlockDefinitionsExt";
        public static readonly string BulkBlockUpdate = "BulkBlockUpdate";
        public static readonly string TextColors = "TextColors";
        public static readonly string EnvMapAspect = "EnvMapAspect";
        public static readonly string EntityProperty = "EntityProperty";
        public static readonly string ExtEntityPositions = "ExtEntityPositions";
        public static readonly string FastMap = "FastMap";

        public string Name;
        public int Version;

        public CpeExtension(string name, int version)
        {
            Name = name;
            Version = version;
        }
    }
}
