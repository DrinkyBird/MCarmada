using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using MCarmada.Server;

namespace MCarmada.Commands
{
    internal class CommandManager
    {
        private Dictionary<string, Command> commandMap = new Dictionary<string, Command>();

        internal CommandManager()
        {

        }

        internal void AddCommand(string name, Command command)
        {
            if (commandMap.ContainsKey(name))
            {
                throw new InvalidOperationException("Tried to register command that already exists:  + name");
            }

            commandMap[name] = command;
        }

        internal void Execute(Player source, string line)
        {
            string[] words = line.Split(' ');

            if (words.Length == 0)
            {
                return;
            }

            string name = words[0];
            if (name.StartsWith("/"))
            {
                name = name.Substring(1);
            }

            if (!commandMap.ContainsKey(name))
            {
                source.SendMessage("&cInvalid command.");
                return;
            }

            Command command = commandMap[name];
            if (command == null)
            {
                source.SendMessage("&cInvalid command.");
                return;
            }

            string[] parameters = new string[words.Length - 1];
            Array.Copy(words, 1, parameters, 0, parameters.Length);
            command.Execute(source, parameters);
        }

        internal void RegisterMainCommands()
        {
            AddCommand("exts", new CommandExtensions());
        }
    }
}
