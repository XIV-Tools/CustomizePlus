// © Customize+.
// Licensed under the MIT license.

using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Helpers
{
	internal static class ChatHelper
	{
		public static void PrintInChat(string message)
		{
			var stringBuilder = new SeStringBuilder();
			stringBuilder.AddUiForeground(45);
			stringBuilder.AddText($"[Customize+] {message}");
			stringBuilder.AddUiForegroundOff();
			DalamudServices.ChatGui.Print(stringBuilder.BuiltString);
		}
	}
}
