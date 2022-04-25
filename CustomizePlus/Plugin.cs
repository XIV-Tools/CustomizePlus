// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Collections.Generic;
	using CustomizePlus.Interface;
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
		private static Hook<RenderDelegate>? renderManagerHook;

		public Plugin()
		{
			try
			{
				Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

				LoadConfig();

				CommandManager.AddCommand((s, t) => ConfigurationInterface.Show(), "/customize", "Opens the customize+ configuration window");

				PluginInterface.UiBuilder.Draw += InterfaceManager.Draw;
				PluginInterface.UiBuilder.OpenConfigUi += ConfigurationInterface.Show;

				ChatGui.Print("Customize+ started");
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, "Error instantiating plugin");
			}
		}

		private delegate IntPtr RenderDelegate(IntPtr renderManager);

		[PluginService] [RequiredVersion("1.0")] public static ObjectTable ObjectTable { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static CommandManager CommandManager { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static ChatGui ChatGui { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static ClientState ClientState { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static SigScanner SigScanner { get; private set; } = null!;

		public static InterfaceManager InterfaceManager { get; private set; } = new InterfaceManager();

		public static Configuration Configuration { get; private set; } = null!;

		public string Name => "Customize Plus";

		public static void LoadConfig()
		{
			try
			{
				NameToScale.Clear();

				foreach (BodyScale bodyScale in Configuration.BodyScales)
				{
					if (NameToScale.ContainsKey(bodyScale.CharacterName))
						continue;

					NameToScale.Add(bodyScale.CharacterName, bodyScale);
				}

				try
				{
					if (Configuration.Enable)
					{
						if (renderManagerHook == null)
						{
							// "Render::Manager::Render"
							IntPtr renderAddress = SigScanner.ScanText("40 53 55 57 41 56 41 57 48 83 EC 60");
							renderManagerHook = new Hook<RenderDelegate>(renderAddress, OnRender);
						}

						renderManagerHook.Enable();
						PluginLog.Information("Hooking render function");
					}
					else
					{
						renderManagerHook?.Disable();
						PluginLog.Information("Unhooking render function");
					}
				}
				catch (Exception e)
				{
					PluginLog.Error($"Failed to hook Render::Manager::Render {e}");
					throw;
				}
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, "Error loading config");
			}
		}

		public static unsafe void Update()
		{
			foreach (GameObject obj in ObjectTable)
			{
				if (obj is Character character)
				{
					Apply(character);
				}
			}
		}

		public void Dispose()
		{
			renderManagerHook?.Disable();
			renderManagerHook?.Dispose();

			Files.Dispose();
			CommandManagerExtensions.Dispose();

			PluginInterface.UiBuilder.Draw -= InterfaceManager.Draw;
			PluginInterface.UiBuilder.OpenConfigUi -= ConfigurationInterface.Show;
		}

		private static void Apply(Character character)
		{
			string characterName = character.Name.ToString();
			if (NameToScale.TryGetValue(characterName, out var scale))
			{
				scale.Apply(character);
			}
		}

		private static IntPtr OnRender(IntPtr manager)
		{
			if (renderManagerHook == null)
				throw new Exception();

			// if this gets disposed while running we crash calling Original's getter, so get it at start
			RenderDelegate original = renderManagerHook.Original;

			try
			{
				Update();
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error in CustomizePlus render hook {e}");
				renderManagerHook?.Disable();
			}

			return original(manager);
		}
	}
}
