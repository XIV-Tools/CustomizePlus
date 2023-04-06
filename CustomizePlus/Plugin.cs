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
	using System.Runtime.InteropServices;
	using System.Text;
	using CustomizePlus.Api;
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
	using FFXIVClientStructs.FFXIV.Client.System.String;
	using FFXIVClientStructs.FFXIV.Client.UI;
	using FFXIVClientStructs.FFXIV.Common.Lua;
	using FFXIVClientStructs.FFXIV.Component.GUI;
	using Lumina.Excel.GeneratedSheets;
	using Newtonsoft.Json;
	using Penumbra.String;

	using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
	using CustomizeData = Penumbra.GameData.Structs.CustomizeData;
	using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
	using ObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

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

		public Plugin()
		{
			try
			{
				Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
				ipcManager = new(ObjectTable, PluginInterface);

				LoadConfig();

				CommandManager.AddCommand((s, t) => ConfigurationInterface.Toggle(), "/customize", "Toggles the Customize+ configuration window.");

				PluginInterface.UiBuilder.Draw += InterfaceManager.Draw;
				PluginInterface.UiBuilder.OpenConfigUi += ConfigurationInterface.Toggle;

				if (PluginInterface.IsDevMenuOpen)
					ConfigurationInterface.Show();

				ChatGui.Print("Customize+ started");
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, "Error instantiating plugin");
			}
		}

		private delegate IntPtr RenderDelegate(IntPtr a1, int a2, IntPtr a3, byte a4, IntPtr a5, IntPtr a6);
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		private unsafe delegate void GameObjectMovementDelegate(IntPtr gameObject);

		[PluginService] [RequiredVersion("1.0")] public static ObjectTable ObjectTable { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static CommandManager CommandManager { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static ChatGui ChatGui { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static ClientState ClientState { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static SigScanner SigScanner { get; private set; } = null!;
		[PluginService] [RequiredVersion("1.0")] public static GameGui GameGui { get; private set; } = null!;

		public static InterfaceManager InterfaceManager { get; private set; } = new InterfaceManager();

		public static Configuration Configuration { get; private set; } = null!;

		public string Name => "Customize Plus";

		public static void LoadConfig(bool autoModeUpdate = false)
		{

			try
			{
				NameToScale.Clear();

				defaultScale = null;
				defaultRetainerScale = null;
				defaultCutsceneScale = null;

				foreach (BodyScale bodyScale in Configuration.BodyScales)
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
					if (Configuration.Enable)
					{
						if (renderManagerHook == null)
						{
							// "Render::Manager::Render"
							IntPtr renderAddress = SigScanner.ScanText("E8 ?? ?? ?? ?? 48 81 C3 ?? ?? ?? ?? BE ?? ?? ?? ?? 45 33 F6");
							//renderManagerHook = new Hook<RenderDelegate>(renderAddress, OnRender);
							renderManagerHook = Hook<RenderDelegate>.FromAddress(renderAddress, OnRender);
						}

						if(gameObjectMovementHook == null)
						{
							IntPtr movementAddress = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 48 8B 03 48 8B CB FF 50 ?? 83 F8 ?? 75 ??");
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
			for (var i = 0; i < ObjectTable.Length; i++)
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
				if (i >= 202 && i < 240 && !Configuration.ApplyToNpcsInCutscenes)
					continue;

				var obj = ObjectTable[i];
	
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
			foreach (GameObject obj in ObjectTable)
			{
				if (obj.Name.ToString() == name)
					return obj;
			}

			return null;
		}

		public void Dispose()
		{
			ipcManager?.Dispose();

			gameObjectMovementHook?.Disable();
			gameObjectMovementHook?.Dispose();

			renderManagerHook?.Disable();
			renderManagerHook?.Dispose();

			Files.Dispose();
			CommandManagerExtensions.Dispose();

			PluginInterface.UiBuilder.Draw -= InterfaceManager.Draw;
			PluginInterface.UiBuilder.OpenConfigUi -= ConfigurationInterface.Show;
		}

		private static unsafe (BodyScale, bool) FindScale(int objectIndex)
		{
			//Determine player object for root scale behavior later. Have to catch errors for zone transitions.
			uint playerObjId = 0;
			try
			{
				playerObjId = ObjectTable[0].ObjectId;
			}
			catch (Exception ex) { }

			var obj = ObjectTable[objectIndex];

			if (obj == null)
				return (null, false);

			BodyScale? scale = null;
			// Mare Support: Bool check to see if override table from IPC can be used
			scale = IdentifyBodyScale((ObjectStruct*)ObjectTable.GetObjectAddress(objectIndex), playerObjId != obj.ObjectId);

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
					if (!Configuration.ApplyToNpcs && !isCutsceneNpc)
						return (null, false);
					else if (isCutsceneNpc && !Configuration.ApplyToNpcsInCutscenes)
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

		private static unsafe void OnGameObjectMove(IntPtr gameObjectPtr)
		{
			// Call the original function.
			gameObjectMovementHook.Original(gameObjectPtr);

			var gameObject = ObjectTable.CreateObjectReference(gameObjectPtr);

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
			if (gameObject.ObjectIndex >= 202 && gameObject.ObjectIndex < 240 && !Configuration.ApplyToNpcsInCutscenes)
				return;

			(BodyScale? scale, bool applyRootScale) = FindScale(gameObject.ObjectIndex);
			if (scale == null)
				return;
			scale.ApplyRootPosition(gameObject);

			//PluginLog.Information($"{gameObject.ObjectIndex} = {gameObject.Name.ToString()}");
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
					if (GameGui.GetAddonByName("PvPMKSIntroduction", 1) == IntPtr.Zero)
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
			else if (playerOnly && ObjectTable[0] != null)
			{
				if (ObjectTable[0].Name.TextValue == actorName)
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
			if (gameObject->Name[0] != 0 || gameObject->ObjectKind != (byte)ObjectKind.Player)
			{
				return null;
			}

			var player = ObjectTable[0];
			if (player == null)
			{
				return null;
			}

			var customize1 = (CustomizeData*)((CharacterStruct*)gameObject)->CustomizeData;
			var customize2 = (CustomizeData*)((CharacterStruct*)player.Address)->CustomizeData;
			return customize1->Equals(*customize2) ? player.Name.ToString() : null;
		}

		private static unsafe string? GetInspectName()
		{
			var addon = GameGui.GetAddonByName("CharacterInspect", 1);
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
			var uiModule = (UIModule*)GameGui.GetUIModule();
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
			var addon = GameGui.GetAddonByName("MiragePrismMiragePlate", 1);
			return addon == IntPtr.Zero ? null : GetPlayerName();
		}

		private static string? GetPlayerName()
			=> ObjectTable[0]?.Name.ToString();

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