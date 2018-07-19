using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.Server;

namespace MCarmada.Commands
{
    class CommandWhitelist : Command
    {
        public override void Execute(Player player, string[] args)
        {
            if (!player.IsOp)
            {
                player.SendMessage("&cYou do not have permission to use this command.");
                return;
            }

            Server.Server server = Program.Instance.Server;
            NameList whitelist = server.Whitelist;

            if (args.Length < 1)
            {
                Help(player);
                return;
            }

            string subcmd = args[0];

            switch (subcmd)
            {
                case "add":
                {
                    if (args.Length < 2)
                    {
                        Help(player);
                        return;
                    }

                    string name = args[1];

                    if (whitelist.Contains(name))
                    {
                        player.SendMessage("&cThat user is already whitelisted.");
                        return;
                    }

                    whitelist.AddName(name);
                    server.BroadcastOpEvent(player, "Added " + name + " to the whitelist");

                    break;
                }

                case "del":
                {
                    if (args.Length < 2)
                    {
                        Help(player);
                        return;
                    }

                    string name = args[1];

                    if (!whitelist.Contains(name))
                    {
                        player.SendMessage("&cThat user is not whitelisted.");
                        return;
                    }

                    whitelist.RemoveName(name);
                    server.BroadcastOpEvent(player, "Removed " + name + " from the whitelist");

                    break;
                }
            }
        }

        public override void Help(Player player)
        {
            player.SendMessage("&fSyntax: /whitelist add <name>");
            player.SendMessage("&f        /whitelist del <name>");
            player.SendMessage("&f        /whitelist on");
            player.SendMessage("&f        /whitelist off");
        }
    }
}
