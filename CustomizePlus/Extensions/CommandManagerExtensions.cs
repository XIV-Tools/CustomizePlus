// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Extensions
{
	using System.Collections.Generic;
	using Dalamud.Game.Command;

	public static class CommandManagerExtensions
	{
		private static readonly List<string> BoundCommands = new List<string>();

		public static void AddCommand(this CommandManager self, CommandInfo.HandlerDelegate handler, string command, string help)
		{
			CommandInfo info = new CommandInfo(handler);
			info.HelpMessage = help;

			if (!command.StartsWith('/'))
				command = '/' + command;

			BoundCommands.Add(command);

			self.AddHandler(command, info);
		}

		public static void Dispose()
		{
			foreach (string command in BoundCommands)
			{
				DalamudServices.CommandManager.RemoveHandler(command);
			}
		}
	}
}
