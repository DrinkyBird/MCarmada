using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.Server;
using MCarmada.Utils.Maths;
using MCarmada.World;

namespace MCarmada.Plugins
{
    partial class PluginManager
    {
        internal void OnPlayerConnect(Player player)
        {
            foreach (var listener in eventListeners) listener.OnPlayerConnect(player);
        }

        internal void OnPlayerSpawn(Player player)
        {
            foreach (var listener in eventListeners) listener.OnPlayerSpawn(player);
        }

        internal void OnPlayerQuit(Player player)
        {
            foreach (var listener in eventListeners) listener.OnPlayerQuit(player);
        }

        internal void OnPlayerMove(Player player, Vector3 oldPos, Vector3 newPos)
        {
            foreach (var listener in eventListeners) listener.OnPlayerMove(player, oldPos, newPos);
        }

        internal void OnPlayerRotate(Player player, Vector2 oldRot, Vector2 newRot)
        {
            foreach (var listener in eventListeners) listener.OnPlayerRotate(player, oldRot, newRot);
        }

        internal void OnServerTick(Server.Server server, long tickNumber)
        {
            foreach (var listener in eventListeners) listener.OnServerTick(server, tickNumber);
        }

        internal void OnLevelTick(Level level, ulong tickNumber)
        {
            foreach (var listener in eventListeners) listener.OnLevelTick(level, tickNumber);
        }

        internal void OnLevelLoaded(Level level)
        {
            foreach (var listener in eventListeners) listener.OnLevelLoaded(level);
        }

        internal void OnPlayerDestroyBlock(Player player, int x, int y, int z, Block oldBlock)
        {
            foreach (var listener in eventListeners) listener.OnPlayerDestroyBlock(player, x, y, z, oldBlock);
        }

        internal void OnPlayerPlaceBlock(Player player, int x, int y, int z, Block newBlock)
        {
            foreach (var listener in eventListeners) listener.OnPlayerPlaceBlock(player, x, y, z, newBlock);
        }

        internal void OnPlayerChangeBlock(Player player, int x, int y, int z, Block oldBlock, Block newBlock)
        {
            foreach (var listener in eventListeners) listener.OnPlayerChangeBlock(player, x, y, z, oldBlock, newBlock);
        }

        internal void OnLevelBlockChange(Level level, int x, int y, int z, Block block)
        {
            foreach (var listener in eventListeners) listener.OnLevelBlockChange(level, x, y, z, block);
        }
    }
}
