using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.Server;
using NLog.LayoutRenderers;

namespace MCarmada.Commands
{
    class CommandEdit : Command
    {
        public override void Execute(Player player, string[] args)
        {
            if (!player.IsOp)
            {
                player.SendMessage("&cYou do not have permission to use this command.");
                return;
            }

            player.HandleEditCommand(args);
        }

        public override void Help(Player player)
        {
            throw new NotImplementedException();
        }
    }
}
