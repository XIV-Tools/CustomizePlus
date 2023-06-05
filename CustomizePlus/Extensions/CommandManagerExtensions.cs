// © Customize+.
// Licensed under the MIT license.

using System.Collections.Generic;

using Dalamud.Game.Command;

namespace CustomizePlus.Extensions
{
    public static class CommandManagerExtensions
    {
        private static readonly List<string> BoundCommands = new();

        public static void AddCommand(this CommandManager self, CommandInfo.HandlerDelegate handler, string command,
            string help)
        {
            var info = new CommandInfo(handler)
            {
                HelpMessage = help
            };

            if (!command.StartsWith('/'))
            {
                command = '/' + command;
            }

            BoundCommands.Add(command);

            self.AddHandler(command, info);
        }

        public static void Dispose()
        {
            foreach (var command in BoundCommands)
            {
                DalamudServices.CommandManager.RemoveHandler(command);
            }
        }
    }
}