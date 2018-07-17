using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fNbt;
using MCarmada.Cpe;
using MCarmada.Server;

namespace MCarmada.Commands
{
    class CommandClickDistance : Command
    {
        public override void Execute(Player player, string[] args)
        {
            if (!player.IsOp)
            {
                player.SendMessage("&cYou do not have permission to use this command.");
                return;
            }

            if (!player.SupportsExtension(CpeExtension.ClickDistance))
            {
                player.SendMessage("&cYour client does not support the ClickDistance extension.");
                return;
            }

            if (args.Length != 1)
            {
                Help(player);
                return;
            }

            if (args[0].Equals("reset", StringComparison.CurrentCultureIgnoreCase))
            {
                player.ClickDistance = 5.0f;
                player.SendMessage("&aClick distance reset to " + player.ClickDistance + ".");
            }

            float val;

            try
            {
                val = float.Parse(args[0]);
            }
            catch (Exception ex)
            {
                player.SendMessage("&cValue is invalid format.");
                return;
            }

            player.ClickDistance = val;
            player.SendMessage("&aClick distance set to " + player.ClickDistance + ".");
        }

        public override void Help(Player player)
        {
            player.SendMessage("/cd <reset | click distance in blocks>");
        }
    }
}
