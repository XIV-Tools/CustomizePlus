// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
    using Dalamud.Game.Command;
    using Dalamud.IoC;
    using Dalamud.Plugin;

    public sealed class Plugin : IDalamudPlugin
    {
		public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
			PluginInterface = pluginInterface;
			CommandManager = commandManager;

			Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

			Interface = new Interface();

			Commands.Add((s, t) => Interface.Show(), "/customize", "Opens the customize plus window");

			PluginInterface.UiBuilder.Draw += Interface.Draw;
			PluginInterface.UiBuilder.OpenConfigUi += Interface.Show;
        }

#pragma warning disable CS8618
		public static DalamudPluginInterface PluginInterface { get; private set; }
		public static CommandManager CommandManager { get; private set; }
		public static Configuration Configuration { get; private set; }
		public static Interface Interface { get; private set; }
#pragma warning restore

		public string Name => "Customize Plus";

		public void Dispose()
        {
			Files.Dispose();
			Commands.Dispose();
        }
    }
}
