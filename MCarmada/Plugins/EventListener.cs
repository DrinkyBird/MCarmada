using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.Server;
using MCarmada.Utils.Maths;
using MCarmada.World;

namespace MCarmada.Plugins
{
    public abstract class EventListener
    {
        public virtual void OnPlayerConnect(Player player) { }
        public virtual void OnPlayerSpawn(Player player) { }
        public virtual void OnPlayerQuit(Player player) { }

        public virtual void OnPlayerMove(Player player, Vector3 oldPos, Vector3 newPos) { }
        public virtual void OnPlayerRotate(Player player, Vector2 oldRot, Vector2 newRot) { }

        public virtual void OnPlayerDestroyBlock(Player player, int x, int y, int z, Block oldBlock) { }
        public virtual void OnPlayerPlaceBlock(Player player, int x, int y, int z, Block newBlock) { }
        public virtual void OnPlayerChangeBlock(Player player, int x, int y, int z, Block oldBlock, Block newBlock) { }

        public virtual void OnServerTick(Server.Server server, long tickNumber) { }
        public virtual void OnLevelTick(Level level, ulong tickNumber) { }
        public virtual void OnLevelLoaded(Level level) { }

        public virtual void OnLevelBlockChange(Level level, int x, int y, int z, Block block) { }
    }
}
