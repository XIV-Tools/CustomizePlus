// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Collections.Generic;
	using Dalamud.Game;
	using Dalamud.Game.ClientState;
	using Dalamud.Game.ClientState.Objects;
	using Dalamud.Game.ClientState.Objects.Types;
	using Dalamud.Game.Command;
	using Dalamud.Game.Gui;
	using Dalamud.Hooking;
	using Dalamud.IoC;
	using Dalamud.Logging;
	using Dalamud.Plugin;

	public sealed class Plugin : IDalamudPlugin
	{
		private static readonly Dictionary<string, BodyScale> NameToScale = new();
		private readonly Hook<RenderDelegate> renderManagerHook;

		public Plugin()
		{
			UserInterface = new Interface();
			Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

			LoadConfig();

			CommandManager.AddCommand((s, t) => UserInterface.Show(), "/customize", "Opens the customize plus window");

			PluginInterface.UiBuilder.Draw += UserInterface.Draw;
			PluginInterface.UiBuilder.OpenConfigUi += UserInterface.Show;

			try
			{
				// "Render::Manager::Render"
				this.renderManagerHook = new Hook<RenderDelegate>(SigScanner.ScanText("40 53 55 57 41 56 41 57 48 83 EC 60"), this.OnRender);
				this.renderManagerHook.Enable();
			}
			catch (Exception e)
			{
				PluginLog.Error($"Failed to hook Render::Manager::Render {e}");
				throw;
			}

			ChatGui.Print("Cusotmize+ started");
		}

		private delegate IntPtr RenderDelegate(IntPtr renderManager);

		[PluginService] [RequiredVersion("1.0")] public static ObjectTable ObjectTable { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static CommandManager CommandManager { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static ChatGui ChatGui { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static ClientState ClientState { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static SigScanner SigScanner { get; private set; } = null!;

		public static Configuration Configuration { get; private set; } = null!;
		public static Interface UserInterface { get; private set; } = null!;

		public string Name => "Customize Plus";

		public static void LoadConfig()
		{
			NameToScale.Clear();

			foreach (BodyScale bodyScale in Configuration.BodyScales)
			{
				if (NameToScale.ContainsKey(bodyScale.CharacterName))
					continue;

				NameToScale.Add(bodyScale.CharacterName, bodyScale);
			}
		}

		public void Dispose()
		{
			this.renderManagerHook?.Disable();
			this.renderManagerHook?.Dispose();

			Files.Dispose();
			CommandManagerExtensions.Dispose();

			PluginInterface.UiBuilder.Draw -= UserInterface.Draw;
			PluginInterface.UiBuilder.OpenConfigUi -= UserInterface.Show;
		}

		public unsafe void Update()
		{
			foreach (GameObject obj in ObjectTable)
			{
				if (obj is Character character)
				{
					this.Apply(character);
				}
			}
		}

		private void Apply(Character character)
		{
			string characterName = character.Name.ToString();
			if (NameToScale.TryGetValue(characterName, out var scale))
			{
				scale.Apply(character);
			}
		}

		private IntPtr OnRender(IntPtr manager)
		{
			if (this.renderManagerHook == null)
				throw new Exception();

			// if this gets disposed while running we crash calling Original's getter, so get it at start
			RenderDelegate original = this.renderManagerHook.Original;

			try
			{
				this.Update();
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error in CustomizePlus render hook {e}");
				this.renderManagerHook?.Disable();
			}

			return original(manager);
		}
	}
}
