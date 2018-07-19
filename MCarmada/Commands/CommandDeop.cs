using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.Server;
using NLog.LayoutRenderers;

namespace MCarmada.Commands
{
    class CommandDeop : Command
    {
        public override void Execute(Player player, string[] args)
        {
            if (!player.IsOp)
            {
                player.SendMessage("&cYou do not have permission to use this command.");
                return;
            }

            if (args.Length != 1)
            {
                player.SendMessage("&fSyntax: /deop <username>");
                return;
            }

            Server.Server server = Program.Instance.Server;
            NameList ops = server.OpList;
            string name = args[0];

            if (!ops.Contains(name))
            {
                player.SendMessage("&cThat user is not an operator.");
                return;
            }

            ops.RemoveName(name);
            server.BroadcastOpEvent(player, "De-opped " + name);
            server.UpdatePlayerOpStatus();
        }

        public override void Help(Player player)
        {
            throw new NotImplementedException();
        }
    }
}
