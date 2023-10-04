// © Customize+.
// Licensed under the MIT license.

using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace CustomizePlus.Services
{
    public class DalamudServices
    {
        

        [PluginService]
        [RequiredVersion("1.0")]
        public static DalamudPluginInterface PluginInterface { get; private set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        public static ISigScanner SigScanner { get; private set; } = null!;

        [PluginService] 
        public static IFramework Framework { get; private set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        public static IObjectTable ObjectTable { get; private set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        public static ICommandManager CommandManager { get; private set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        public static IChatGui ChatGui { get; private set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        public static IClientState ClientState { get; private set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        public static IGameGui GameGui { get; private set; } = null!;

        [PluginService]
        [RequiredVersion("1.0")]
        internal static IGameInteropProvider Hooker { get; private set; } = null!;

        public static void Initialize(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<DalamudServices>();
        }

        public static void Destroy()
        {
        }
    }
}