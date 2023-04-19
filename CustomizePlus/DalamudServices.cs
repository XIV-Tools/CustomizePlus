// © Customize+.
// Licensed under the MIT license.

using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus
{
	public class DalamudServices
	{
		[PluginService] public static Framework Framework { get; private set; } = null!;
		[PluginService][RequiredVersion("1.0")] public static ObjectTable ObjectTable { get; private set; } = null!;
		[PluginService][RequiredVersion("1.0")] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
		[PluginService][RequiredVersion("1.0")] public static CommandManager CommandManager { get; private set; } = null!;
		[PluginService][RequiredVersion("1.0")] public static ChatGui ChatGui { get; private set; } = null!;
		[PluginService][RequiredVersion("1.0")] public static ClientState ClientState { get; private set; } = null!;
		[PluginService][RequiredVersion("1.0")] public static SigScanner SigScanner { get; private set; } = null!;
		[PluginService][RequiredVersion("1.0")] public static GameGui GameGui { get; private set; } = null!;

		public static void Initialize(DalamudPluginInterface pluginInterface) => pluginInterface.Create<DalamudServices>();

		public static void Destroy() { }
	}
}
