// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System.Collections.Generic;
	using Dalamud.Game.Command;

	public static class Commands
	{
		private static readonly List<string> BoundCommands = new List<string>();

		public static void Add(CommandInfo.HandlerDelegate handler, string command, string help)
		{
			CommandInfo info = new CommandInfo(handler);
			info.HelpMessage = help;

			if (!command.StartsWith('/'))
				command = '/' + command;

			BoundCommands.Add(command);

			Plugin.CommandManager.AddHandler(command, info);
		}

		public static void Dispose()
		{
			foreach (string command in BoundCommands)
			{
				Plugin.CommandManager.RemoveHandler(command);
			}
		}
	}
}
