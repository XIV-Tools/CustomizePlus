// © Customize+.
// Licensed under the MIT license.

// Signautres stolen from:
// https://github.com/0ceal0t/DXTest/blob/8e9aef4f6f871e7743aafe56deb9e8ad4dc87a0d/SamplePlugin/Plugin.DX.cs
// I don't know how they work, but they do!

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using CustomizePlus.Api;
using CustomizePlus.Core;
using CustomizePlus.Extensions;
using CustomizePlus.Helpers;
using CustomizePlus.Interface;
using CustomizePlus.Services;
using CustomizePlus.Util;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Newtonsoft.Json;
using Penumbra.String;

using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
using ObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace CustomizePlus
{
	public sealed class Plugin : IDalamudPlugin
	{
		private static readonly Dictionary<string, BodyScale> NameToScale = new();
		private static Dictionary<GameObject, BodyScale> scaleByObject = new();
		private static ConcurrentDictionary<string, BodyScale> scaleOverride = new();
		private static Hook<RenderDelegate>? renderManagerHook;
		private static Hook<GameObjectMovementDelegate>? gameObjectMovementHook;
		private static BodyScale? defaultScale;
		private static BodyScale? defaultRetainerScale;
		private static BodyScale? defaultCutsceneScale;
		private static CustomizePlusIpc ipcManager = null!;
		private static CustomizePlusLegacyIpc legacyIpcManager = null!;
		private static ServiceManager serviceManager { get; set; } = null!;

		private delegate IntPtr RenderDelegate(IntPtr a1, long a2);
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		private unsafe delegate void GameObjectMovementDelegate(IntPtr gameObject);

		public static InterfaceManager InterfaceManager { get; private set; } = new InterfaceManager();

		public static ConfigurationManager ConfigurationManager { get; private set; } = new ConfigurationManager();

		public string Name => "Customize Plus";

		public Plugin(DalamudPluginInterface pluginInterface)
		{
			DalamudServices.Initialize(pluginInterface);
			DalamudServices.PluginInterface.UiBuilder.DisableGposeUiHide = true;

			serviceManager = new ServiceManager();
			serviceManager.Add<GPoseService>();
			serviceManager.Add<GPoseAmnesisKtisisWarningService>();
			serviceManager.Add<PosingModeDetectService>();

			DalamudServices.Framework.RunOnFrameworkThread(() =>
			{
				serviceManager.Start();
				DalamudServices.Framework.Update += Framework_Update;
			});

			try
			{
				try
				{
					ConfigurationManager.LoadConfigurationFromFile(DalamudServices.PluginInterface.ConfigFile.FullName);
				}
				catch (FileNotFoundException ex)
				{
					ConfigurationManager.CreateNewConfiguration();
				}
				catch (Exception ex)
				{
					PluginLog.Error(ex, "Unable to load plugin config");
					ChatHelper.PrintInChat("There was an error while loading plugin configuration, details have been printed into dalamud console.");
				}

				ipcManager = new(DalamudServices.ObjectTable, DalamudServices.PluginInterface);
				legacyIpcManager = new(DalamudServices.ObjectTable, DalamudServices.PluginInterface);

				LoadConfig();

				DalamudServices.CommandManager.AddCommand((s, t) => ConfigurationInterface.Toggle(), "/customize", "Toggles the Customize+ configuration window.");
                DalamudServices.CommandManager.AddCommand((s, t) => ApplyByCommand(t), "/customize-apply", "Apply a specific Scale (usage: /customize-apply {Character Name},{Scale Name})");
                DalamudServices.CommandManager.AddCommand((s, t) => ApplyByCommand(t), "/capply", "Alias to /customize-apply");

				DalamudServices.PluginInterface.UiBuilder.Draw += InterfaceManager.Draw;
				DalamudServices.PluginInterface.UiBuilder.OpenConfigUi += ConfigurationInterface.Toggle;

				if (DalamudServices.PluginInterface.IsDevMenuOpen)
					ConfigurationInterface.Show();

				ChatHelper.PrintInChat("Started");
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, "Error instantiating plugin");
			}
		}

		public static void LoadConfig(bool autoModeUpdate = false)
		{
			try
			{
				NameToScale.Clear();

				defaultScale = null;
				defaultRetainerScale = null;
				defaultCutsceneScale = null;

				foreach (BodyScale bodyScale in ConfigurationManager.Configuration.BodyScales)
				{
					bodyScale.ClearCache();
					if (bodyScale.CharacterName == "Default" && bodyScale.BodyScaleEnabled)
					{
						defaultScale = bodyScale;
						PluginLog.Debug($"Default scale with name {defaultScale.ScaleName} being used.");
						continue;
					} else if (bodyScale.CharacterName == "DefaultRetainer" && bodyScale.BodyScaleEnabled)
					{
						defaultRetainerScale = bodyScale;
						PluginLog.Debug($"Default retainer scale with name {defaultRetainerScale.ScaleName} being used.");
					}
					else if (bodyScale.CharacterName == "DefaultCutscene" && bodyScale.BodyScaleEnabled)
					{
						defaultCutsceneScale = bodyScale;
						PluginLog.Debug($"Default cutscene scale with name {defaultCutsceneScale.ScaleName} being used.");
					}

					if (NameToScale.ContainsKey(bodyScale.CharacterName))
						continue;

					if (bodyScale.BodyScaleEnabled)
						NameToScale.Add(bodyScale.CharacterName, bodyScale);
				}

				try
				{
					if (ConfigurationManager.Configuration.Enable)
					{
						if (renderManagerHook == null)
						{
							// "Render::Manager::Render"
							IntPtr renderAddress = DalamudServices.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 81 C3 ?? ?? ?? ?? BE ?? ?? ?? ?? 45 33 F6");
							renderManagerHook = Hook<RenderDelegate>.FromAddress(renderAddress, OnRender);
						}

						if(gameObjectMovementHook == null)
						{
							IntPtr movementAddress = DalamudServices.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 48 8B 03 48 8B CB FF 50 ?? 83 F8 ?? 75 ??");
							gameObjectMovementHook = Hook<GameObjectMovementDelegate>.FromAddress(movementAddress, new GameObjectMovementDelegate(OnGameObjectMove));
						}

						gameObjectMovementHook.Enable();

						renderManagerHook.Enable();
						PluginLog.Debug("Hooking render function");

						//Get player's body scale string and send IPC message (only when saving manually to spare server)
						string? playerName = GetPlayerName();
						if (playerName != null && !autoModeUpdate) {
							BodyScale? playerScale = GetBodyScale(playerName);
							ipcManager.OnScaleUpdate(JsonConvert.SerializeObject(playerScale));
							legacyIpcManager.OnScaleUpdate(playerScale);
						}
						
					}
					else
					{
						renderManagerHook?.Disable();
						gameObjectMovementHook?.Disable();
						PluginLog.Debug("Unhooking render function");
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
			for (var i = 0; i < DalamudServices.ObjectTable.Length; i++)
			{
				// Always filler Event obj
				if (i == 245) 
					continue;

				// Removed setting until new UI is done
				// Don't affect EventNPCs when they are given an index above the player range, like in big cities, by config
				// if (i > 245 && !Configuration.ApplyToNpcsInBusyAreas) 
				//	continue;

				// Don't affect the cutscene object range, by configuration.
				// 202 gives leeway as player is not always put in 200 like they should be.
				if (i >= 202 && i < 240 && !ConfigurationManager.Configuration.ApplyToNpcsInCutscenes)
					continue;

				var obj = DalamudServices.ObjectTable[i];
	
				if (obj == null)
					continue;

				try
				{
					(BodyScale? scale, bool applyRootScale) = FindScale(i);
					if (scale == null)
						continue;
					scale.ApplyNonRootBonesAndRootScale(obj, applyRootScale);
				}
				catch (Exception ex)
				{
					PluginLog.LogError($"Error during update:{ex}");
					continue;
				}
			}
		}

		public static GameObject? FindModelByName(string name)
		{
			foreach (GameObject obj in DalamudServices.ObjectTable)
			{
				if (obj.Name.ToString() == name)
					return obj;
			}

			return null;
		}

		public void Dispose()
		{
			DalamudServices.Framework.Update -= Framework_Update;
			serviceManager.Dispose();

			ipcManager?.Dispose();
			legacyIpcManager?.Dispose();

			gameObjectMovementHook?.Disable();
			gameObjectMovementHook?.Dispose();

			renderManagerHook?.Disable();
			renderManagerHook?.Dispose();

			Files.Dispose();
			CommandManagerExtensions.Dispose();

			DalamudServices.PluginInterface.UiBuilder.Draw -= InterfaceManager.Draw;
			DalamudServices.PluginInterface.UiBuilder.OpenConfigUi -= ConfigurationInterface.Show;
		}

		private void Framework_Update(Framework framework)
		{
			serviceManager.Tick();
		}

		private static unsafe (BodyScale, bool) FindScale(int objectIndex)
		{
			//Determine player object for root scale behavior later. Have to catch errors for zone transitions.
			uint playerObjId = 0;
			try
			{
				playerObjId = DalamudServices.ObjectTable[0].ObjectId;
			}
			catch (Exception ex) { }

			var obj = DalamudServices.ObjectTable[objectIndex];

			if (obj == null)
				return (null, false);

			BodyScale? scale = null;
			// Mare Support: Bool check to see if override table from IPC can be used
			scale = IdentifyBodyScale((ObjectStruct*)DalamudServices.ObjectTable.GetObjectAddress(objectIndex), playerObjId != obj.ObjectId);

			bool isCutsceneNpc = false;
			switch (obj.ObjectKind)
			{
				case ObjectKind.Player:
				case ObjectKind.Companion:
					scale = scale ?? defaultScale ?? null;
					break;
				case ObjectKind.Retainer:
					scale = scale ?? defaultRetainerScale ?? defaultScale ?? null;
					break;
				case ObjectKind.EventNpc:
				case ObjectKind.BattleNpc:
					isCutsceneNpc = objectIndex >= 200 && objectIndex < 246;
					// Stop if NPCs disabled by config. Have to double check cutscene range due to the 200/201 issue.
					if (!ConfigurationManager.Configuration.ApplyToNpcs && !isCutsceneNpc)
						return (null, false);
					else if (isCutsceneNpc && !ConfigurationManager.Configuration.ApplyToNpcsInCutscenes)
						return (null, false);
					// Choose most appropriate default, or fallback to null.
					if (isCutsceneNpc)
						scale = scale ?? defaultCutsceneScale ?? defaultScale ?? null;
					else
						scale = scale ?? defaultScale ?? null;
					break;
				default:
					return (null, false);
			}

			// No scale to apply, move on.
			if (scale == null)
				return (null, false);

			// Don't apply root scales to NPCs in cutscenes or battle NPCs. Both cause animation or camera issues. Exception made for player pets
			bool applyRootScale = !(isCutsceneNpc || (obj.ObjectKind == ObjectKind.BattleNpc && obj.OwnerId != playerObjId));

			return (scale, applyRootScale);
		}

		private void ApplyByCommand(string args)
		{
			string charaName = "", scaleName = "";

			try
			{
				if (string.IsNullOrWhiteSpace(args) || args.Count(c => c == ',') != 1)
				{
					PluginLog.Warning($"Can't apply Scale by command because arguments passed were not in the correct format ([Character Name],[Scale Name]). args: \"{args}\"");
					return;
				}

				(charaName, scaleName) = args.Split(',') switch { var a => (a[0].Trim(), a[1].Trim()) };

				if (!ConfigurationManager.Configuration.BodyScales.Any())
				{
					PluginLog.Warning($"Can't apply Scale \"{scaleName}\" to Character \"{charaName}\" by command because no Scale were loaded or none exist");
					return;
				}

				if (ConfigurationManager.Configuration.BodyScales.Count(x => x.ScaleName == scaleName && x.CharacterName == charaName) > 1)
				{
					PluginLog.Information($"More than one entry were found for Scale \"{scaleName}\" and Character \"{charaName}\". Will try to apply the first matching Scale.");
				}

				var scale = ConfigurationManager.Configuration.BodyScales.FirstOrDefault(x => x.ScaleName == scaleName && x.CharacterName == charaName);

				if (scale == null)
				{
					PluginLog.Warning($"Can't apply Scale \"{(string.IsNullOrWhiteSpace(scaleName) ? "empty (none provided)" : scaleName)}\" " +
						$"to Character \"{(string.IsNullOrWhiteSpace(charaName) ? "empty (none provided)" : charaName)}\" by command\n" +
						"Check if the Scale and Character names were provided correctly and said Scale exists to the appointed Character");
					return;
				}

				ConfigurationManager.ToggleOffAllOtherMatching(scale);
				scale.BodyScaleEnabled = true;
				ConfigurationManager.SaveConfiguration();
				LoadConfig(true);

				PluginLog.Debug($"Scale \"{scale.ScaleName}\" were successfully applied to Character \"{scale.CharacterName}\" by command");
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error applying Scale by command: \n" +
					$"Scale name \"{(string.IsNullOrWhiteSpace(scaleName) ? "empty (none provided)" : scaleName)}\"\n" +
					$"Character name \"{(string.IsNullOrWhiteSpace(charaName) ? "empty (none provided)" : charaName)}\"\n" +
					$"Error: {e}");
			}
		}

        private static IntPtr OnRender(IntPtr a1, long a2)
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

			return original(a1, a2);
		}

		//todo: doesn't work in cutscenes, something getting called after this and resets changes
		private static unsafe void OnGameObjectMove(IntPtr gameObjectPtr)
		{
			// Call the original function.
			gameObjectMovementHook.Original(gameObjectPtr);

			if (GPoseService.Instance.GPoseState == GPoseState.Inside && PosingModeDetectService.Instance.IsInPosingMode)
				return;

			var gameObject = DalamudServices.ObjectTable.CreateObjectReference(gameObjectPtr);

			if (gameObject == null)
				return;

			// Always filler Event obj
			if (gameObject.ObjectIndex == 245)
				return;

			// Removed setting until new UI is done
			// Don't affect EventNPCs when they are given an index above the player range, like in big cities, by config
			// if (gameObject.ObjectIndex > 245 && !Configuration.ApplyToNpcsInBusyAreas) 
			//	continue;

			// Don't affect the cutscene object range, by configuration.
			// 202 gives leeway as player is not always put in 200 like they should be.
			if (gameObject.ObjectIndex >= 202 && gameObject.ObjectIndex < 240 && !ConfigurationManager.Configuration.ApplyToNpcsInCutscenes)
				return;

			(BodyScale? scale, bool applyRootScale) = FindScale(gameObject.ObjectIndex);
			if (scale == null)
				return;
			scale.ApplyRootPosition(gameObject);
		}

		// All functions related to this process for non-named objects adapted from Penumbra logic. Credit to Ottermandias et al.
		private static unsafe BodyScale? IdentifyBodyScale(ObjectStruct* gameObject, bool allowIPC)
		{
			if (gameObject == null)
			{
				return null;
			}

			string? actorName = null;
			BodyScale? scale = null;

			try
			{
				actorName = new ByteString(gameObject->Name).ToString();

				if (string.IsNullOrEmpty(actorName))
				{
					string? actualName = null;

					// Check if in pvp intro sequence, which uses 240-244 for the 5 players, and only affect the first if so
					// TODO: Ensure player side only. First group, where one of the node textures is blue. Alternately, look for hidden party list UI and get names from there.
					if (DalamudServices.GameGui.GetAddonByName("PvPMKSIntroduction", 1) == IntPtr.Zero)
					{
						actualName = gameObject->ObjectIndex switch
						{
							240 => GetPlayerName(), // character window
							241 => GetInspectName() ?? GetGlamourName(), // GetCardName() ?? // inspect, character card, glamour plate editor. - Card removed due to logic issues
							242 => GetPlayerName(), // try-on
							243 => GetPlayerName(), // dye preview
							244 => GetPlayerName(), // portrait preview
							>= 200 => GetCutsceneName(gameObject),
							_ => null,
						} ?? new ByteString(gameObject->Name).ToString();
					}
					else
					{
						actualName = gameObject->ObjectIndex switch
						{
							240 => GetPlayerName(), // character window
							_ => null,
						} ?? new ByteString(gameObject->Name).ToString();
					}

					if (actualName == null)
					{
						return null;
					}

					actorName = actualName;
				}
        
				scale = IdentifyBodyScaleByName(actorName, allowIPC);
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error identifying bodyscale:\n{e}");
				return null;
			}

			return scale;
		}

		private static BodyScale? IdentifyBodyScaleByName(string actorName, bool allowIPC = false, bool playerOnly = false)
		{
			BodyScale? scale = null;
			if (allowIPC)
			{
				if (!scaleOverride.TryGetValue(actorName, out scale))
					NameToScale.TryGetValue(actorName, out scale);
			}
			else if (playerOnly && DalamudServices.ObjectTable[0] != null)
			{
				if (DalamudServices.ObjectTable[0].Name.TextValue == actorName)
					NameToScale.TryGetValue(actorName, out scale);
			}
			else
			{
				NameToScale.TryGetValue(actorName, out scale);
			}
			
			return scale;
		}

		// Checks Customization (not ours) of the cutscene model vs the player model to see if
		// the player name should be used.
		private static unsafe string? GetCutsceneName(ObjectStruct* gameObject)
		{
			if (gameObject->Name[0] != 0 || gameObject->ObjectKind != (byte)ObjectKind.Player) {
				return null;
			}

			var player = DalamudServices.ObjectTable[0];
			if (player == null) {
				return null;
			}

			bool customizeEqual = true;
			var customize1 = ((CharacterStruct*)gameObject)->CustomizeData;
			var customize2 = ((CharacterStruct*)player.Address)->CustomizeData;
			for (int i = 0;i < 26;i++) {
				var data1 = Marshal.ReadByte((IntPtr)customize1, i);
				var data2 = Marshal.ReadByte((IntPtr)customize2, i);
				if (data1 != data2) {
					customizeEqual = false;
					break;
				}
			}
			return customizeEqual ? player.Name.ToString() : null;
		}

		private static unsafe string? GetInspectName()
		{
			var addon = DalamudServices.GameGui.GetAddonByName("CharacterInspect", 1);
			if (addon == IntPtr.Zero)
			{
				return null;
			}

			var ui = (AtkUnitBase*)addon;
			if (ui->UldManager.NodeListCount < 60)
			{
				return null;
			}

			var text = (AtkTextNode*)ui->UldManager.NodeList[59];
			if (text == null || !text->AtkResNode.IsVisible)
			{
				text = (AtkTextNode*)ui->UldManager.NodeList[60];
			}

			return text != null ? text->NodeText.ToString() : null;
		}

		// Obtain the name displayed in the Character Card from the agent.
		private static unsafe string? GetCardName()
		{
			var uiModule = (UIModule*)DalamudServices.GameGui.GetUIModule();
			var agentModule = uiModule->GetAgentModule();
			var agent = (byte*)agentModule->GetAgentByInternalID(393);
			if (agent == null)
			{
				return null;
			}

			var data = *(byte**)(agent + 0x28);
			if (data == null)
			{
				return null;
			}

			var block = data + 0x7A;
			return new ByteString(block).ToString();
		}

		// Obtain the name of the player character if the glamour plate edit window is open.
		private static unsafe string? GetGlamourName()
		{
			var addon = DalamudServices.GameGui.GetAddonByName("MiragePrismMiragePlate", 1);
			return addon == IntPtr.Zero ? null : GetPlayerName();
		}

		private static string? GetPlayerName()
			=> DalamudServices.ObjectTable[0]?.Name.ToString();

		public static void SetTemporaryCharacterScale(string characterName, BodyScale scale)
		{
			if (string.IsNullOrEmpty(characterName))
				return;
			scaleOverride[characterName] = scale;
		}

		public static bool RemoveTemporaryCharacterScale(string characterName)
		{
			return scaleOverride.TryRemove(characterName, out _);
		}

		public static BodyScale? GetBodyScale(string characterName)
		{
			if (string.IsNullOrEmpty(characterName))
				return null;
			return IdentifyBodyScaleByName(characterName, true);
		}

		public static BodyScale? GetPlayerBodyScale(string characterName)
		{
			if (string.IsNullOrEmpty(characterName))
				return null;
			return IdentifyBodyScaleByName(characterName, false, true);
		}
	}
}