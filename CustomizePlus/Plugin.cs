// © Customize+.
// Licensed under the MIT license.

// Signautres stolen from:
// https://github.com/0ceal0t/DXTest/blob/8e9aef4f6f871e7743aafe56deb9e8ad4dc87a0d/SamplePlugin/Plugin.DX.cs
// I don't know how they work, but they do!
namespace CustomizePlus
{
	using System;
	using System.Collections.Concurrent;
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

				CommandManager.AddCommand((s, t) => ConfigurationInterface.Show(), "/customize", "Opens the Customize+ configuration window.");

				PluginInterface.UiBuilder.Draw += InterfaceManager.Draw;
				PluginInterface.UiBuilder.OpenConfigUi += ConfigurationInterface.Show;

				ClientState.Login += this.ClientState_Login;

				ChatGui.Print("Customize+ started");
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, "Error instantiating plugin");
			}
		}

		private delegate IntPtr RenderDelegate(IntPtr a1, int a2, IntPtr a3, byte a4, IntPtr a5, IntPtr a6);

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
					bodyScale.ClearCache();

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
							IntPtr renderAddress = SigScanner.ScanText("E8 ?? ?? ?? ?? 48 81 C3 ?? ?? ?? ?? BE ?? ?? ?? ?? 45 33 F6");
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
			ClientState.Login -= this.ClientState_Login;

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
			BodyScale? scale = null;
			NameToScale.TryGetValue(characterName, out scale);

			if (scale == null)
				return;

			scale.Apply(character);
		}

		private static IntPtr OnRender(IntPtr a1, int a2, IntPtr a3, byte a4, IntPtr a5, IntPtr a6)
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

			return original(a1, a2, a3, a4, a5, a6);
		}

		private void ClientState_Login(object? sender, EventArgs e)
		{
			CharacterToScale.Clear();
		}
	}
}
