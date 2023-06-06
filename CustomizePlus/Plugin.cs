// © Customize+.
// Licensed under the MIT license.

// Signautres stolen from:
// https://github.com/0ceal0t/DXTest/blob/8e9aef4f6f871e7743aafe56deb9e8ad4dc87a0d/SamplePlugin/Plugin.DX.cs
// I don't know how they work, but they do!

using System;
using System.Linq;
using System.Runtime.InteropServices;
using CustomizePlus.Api;
using CustomizePlus.Core;
using CustomizePlus.Data;
using CustomizePlus.Data.Armature;
using CustomizePlus.Data.Configuration;
using CustomizePlus.Data.Profile;
using CustomizePlus.Extensions;
using CustomizePlus.Helpers;
using CustomizePlus.Services;
using CustomizePlus.UI;
using CustomizePlus.UI.Windows;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Plugin;
using Newtonsoft.Json;

//using Dalamud.Game.ClientState.Objects.Types;
//using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
//using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
//using ObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
//using Dalamud.Game.ClientState.Objects.Enums;

namespace CustomizePlus
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Customize Plus";

        public static UserInterfaceManager InterfaceManager { get; } = new();
        public static ServiceManager ServiceManager { get; } = new();
        public static ProfileManager ProfileManager { get; } = new();
        public static ArmatureManager ArmatureManager { get; } = new();
        public static ConfigurationManager ConfigurationManager { get; set; } = new();
        //private static readonly Dictionary<string, BodyScale> NameToScale = new();
        //private static Dictionary<GameObject, BodyScale> scaleByObject = new();
        //private static ConcurrentDictionary<string, BodyScale> scaleOverride = new();

        private static CustomizePlusIpc _ipcManager = null!;

        private static Hook<RenderDelegate>? _renderManagerHook;
        private static Hook<GameObjectMovementDelegate>? _gameObjectMovementHook;

        private delegate IntPtr RenderDelegate(IntPtr a1, long a2, int a3, int a4);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void GameObjectMovementDelegate(IntPtr gameObject);

        //private static BodyScale? defaultScale;
        //private static BodyScale? defaultRetainerScale;
        //private static BodyScale? defaultCutsceneScale;

        public Plugin(DalamudPluginInterface pluginInterface)
        {
            try
            {
                DalamudServices.Initialize(pluginInterface);
                DalamudServices.PluginInterface.UiBuilder.DisableGposeUiHide = true;

                ReloadHooks();

                DalamudServices.Framework.RunOnFrameworkThread(() =>
                {
                    ServiceManager.Start();
                    DalamudServices.Framework.Update += Framework_Update;
                });

                ProfileManager.ProcessConvertedProfiles();
                ProfileManager.LoadProfiles();

                _ipcManager = new CustomizePlusIpc(DalamudServices.ObjectTable, DalamudServices.PluginInterface);

                DalamudServices.CommandManager.AddCommand((s, t) => MainWindow.Toggle(), "/customize",
                    "Toggles the Customize+ configuration window.");
                DalamudServices.CommandManager.AddCommand((s, t) => ApplyByCommand(t), "/customize-apply",
                    "Apply a specific Scale (usage: /customize-apply {Character Name},{Scale Name})");
                DalamudServices.CommandManager.AddCommand((s, t) => ApplyByCommand(t), "/capply",
                    "Alias to /customize-apply");

                DalamudServices.PluginInterface.UiBuilder.Draw += InterfaceManager.Draw;
                DalamudServices.PluginInterface.UiBuilder.OpenConfigUi += MainWindow.Toggle;

                if (DalamudServices.PluginInterface.IsDevMenuOpen)
                {
                    MainWindow.Show();
                }

                ChatHelper.PrintInChat("Customize+ Started!");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error instantiating plugin");
                ChatHelper.PrintInChat(
                    "An error occurred while starting Customize+. See the Dalamud log for more details");
            }
        }

        public void Dispose()
        {
            InterfaceManager.Dispose();
            ServiceManager.Dispose();

            DalamudServices.Framework.Update -= Framework_Update;

            _ipcManager?.Dispose();

            _gameObjectMovementHook?.Disable();
            _gameObjectMovementHook?.Dispose();

            _renderManagerHook?.Disable();
            _renderManagerHook?.Dispose();

            Files.Dispose();
            CommandManagerExtensions.Dispose();

            DalamudServices.PluginInterface.UiBuilder.Draw -= InterfaceManager.Draw;
            DalamudServices.PluginInterface.UiBuilder.OpenConfigUi -= MainWindow.Show;
        }


        /// <summary>
        ///     The function that basically tells the plugin to get all its ducks in a row.
        ///     Making sure the profiles are loaded up and squared away and the armature's
        ///     are up to date.
        /// </summary>
        public static void RefreshPlugin(bool autoModeUpdate = false)
        {
            try
            {
                //ConfigurationManager.
                //ProfileManager.SaveAllProfiles(); //?
                //ProfileManager.LoadProfiles();

                //NameToScale.Clear();

                //defaultScale = null;
                //defaultRetainerScale = null;
                //defaultCutsceneScale = null;

                //foreach (BodyScale bodyScale in ConfigurationManager.Configuration.BodyScales)
                //{
                //	bodyScale.ClearCache();
                //	if (bodyScale.CharacterName == "Default" && bodyScale.BodyScaleEnabled)
                //	{
                //		defaultScale = bodyScale;
                //		PluginLog.Debug($"Default scale with name {defaultScale.ScaleName} being used.");
                //		continue;
                //	}
                //	else if (bodyScale.CharacterName == "DefaultRetainer" && bodyScale.BodyScaleEnabled)
                //	{
                //		defaultRetainerScale = bodyScale;
                //		PluginLog.Debug($"Default retainer scale with name {defaultRetainerScale.ScaleName} being used.");
                //	}
                //	else if (bodyScale.CharacterName == "DefaultCutscene" && bodyScale.BodyScaleEnabled)
                //	{
                //		defaultCutsceneScale = bodyScale;
                //		PluginLog.Debug($"Default cutscene scale with name {defaultCutsceneScale.ScaleName} being used.");
                //	}

                //	if (NameToScale.ContainsKey(bodyScale.CharacterName))
                //		continue;

                //	if (bodyScale.BodyScaleEnabled)
                //		NameToScale.Add(bodyScale.CharacterName, bodyScale);
                //}
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error loading config");
            }
        }

        public static void ReloadHooks()
        {
            try
            {
                if (ConfigurationManager.Configuration.PluginEnabled)
                {
                    if (_renderManagerHook == null)
                    {
                        // "Render::Manager::Render"
                        var renderAddress = DalamudServices.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 81 C3 ?? ?? ?? ?? BF ?? ?? ?? ?? 33 ED");
                        _renderManagerHook = Hook<RenderDelegate>.FromAddress(renderAddress, OnRender);
                        PluginLog.Debug("Render hook established");
                    }

                    if (_gameObjectMovementHook == null)
                    {
                        var movementAddress = DalamudServices.SigScanner.ScanText("E8 ?? ?? ?? ?? EB 29 48 8B 5F 08");
                        _gameObjectMovementHook = Hook<GameObjectMovementDelegate>.FromAddress(movementAddress, new GameObjectMovementDelegate(OnGameObjectMove));
                        PluginLog.Debug("Movement hook established");
                    }

                    PluginLog.Debug("Hooking render & movement functions");
                    _renderManagerHook.Enable();
                    _gameObjectMovementHook.Enable();

                    PluginLog.Debug("Hooking render manager");
                    _renderManagerHook.Enable();

                    //Get player's body scale string and send IPC message (only when saving manually to spare server)
                    //string? playerName = GetPlayerName();
                    //if (playerName != null && !autoModeUpdate) {
                    //	BodyScale? playerScale = GetBodyScale(playerName);
                    //	ipcManager.OnScaleUpdate(JsonConvert.SerializeObject(playerScale));
                    //	legacyIpcManager.OnScaleUpdate(playerScale);
                    //}

                }
                else
                {
                    PluginLog.Debug("Unhooking...");
                    _renderManagerHook?.Disable();
                    _gameObjectMovementHook?.Disable();
                    _renderManagerHook?.Disable();
                }
            }
            catch (Exception e)
            {
                PluginLog.Error($"Failed to hook Render::Manager::Render {e}");
                throw;
            }
        }

        private void UpdatePlayerIpc()
        {
            //Get player's body scale string and send IPC message
            if (GameDataHelper.GetPlayerName() is string name && name != null)
            {
                if (ProfileManager.GetProfileByCharacterName(name) is CharacterProfile prof && prof != null)
                {
                    _ipcManager.OnScaleUpdate(JsonConvert.SerializeObject(prof));
                }
            }
        }

        private static void Framework_Update(Framework framework)
        {
            ServiceManager.Tick();
            ProfileManager.CheckForNewProfiles();
        }

        private void ApplyByCommand(string args)
        {
            string charaName = "", profName = "";

            try
            {
                if (string.IsNullOrWhiteSpace(args) || args.Count(c => c == ',') != 1)
                {
                    PluginLog.Warning(
                        $"Can't apply Scale by command because arguments passed were not in the correct format ([Character Name],[Scale Name]). args: \"{args}\"");
                    return;
                }

                (charaName, profName) = args.Split(',') switch { var a => (a[0].Trim(), a[1].Trim()) };

                if (!ProfileManager.Profiles.Any())
                {
                    PluginLog.Warning(
                        $"Can't apply Scale \"{profName}\" to Character \"{charaName}\" by command because no Scale were loaded or none exist");
                    return;
                }

                if (ProfileManager.Profiles.Count(x => x.ProfileName == profName && x.CharacterName == charaName) > 1)
                {
                    PluginLog.Information(
                        $"Found more than one profile matching Profile \"{profName}\" and Character \"{charaName}\". Applying first match.");
                }

                var outProf =
                    ProfileManager.Profiles.FirstOrDefault(x => x.ProfileName == profName && x.CharacterName == charaName);

                if (outProf == null)
                {
                    PluginLog.Warning(
                        $"Can't apply Scale \"{(string.IsNullOrWhiteSpace(profName) ? "empty (none provided)" : profName)}\" " +
                        $"to Character \"{(string.IsNullOrWhiteSpace(charaName) ? "empty (none provided)" : charaName)}\" by command\n" +
                        "Check if the Scale and Character names were provided correctly and said Scale exists to the appointed Character");
                    return;
                }

                ProfileManager.AssertEnabledProfile(outProf);
                outProf.Enabled = true;

                //ConfigurationManager.SaveConfiguration(); //???
                RefreshPlugin(true);

                PluginLog.Debug(
                    $"Scale \"{outProf.ProfileName}\" were successfully applied to Character \"{outProf.CharacterName}\" by command");
            }
            catch (Exception e)
            {
                PluginLog.Error($"Error applying Scale by command: \n" +
                                $"Scale name \"{(string.IsNullOrWhiteSpace(profName) ? "empty (none provided)" : profName)}\"\n" +
                                $"Character name \"{(string.IsNullOrWhiteSpace(charaName) ? "empty (none provided)" : charaName)}\"\n" +
                                $"Error: {e}");
            }
        }

        private static IntPtr OnRender(IntPtr a1, long a2, int a3, int a4)
        {
            if (_renderManagerHook == null)
            {
                throw new Exception();
            }

            // if this gets disposed while running we crash calling Original's getter, so get it at start
            var original = _renderManagerHook.Original;

            try
            {
                var activeProfiles = ProfileManager.GetEnabledProfiles();
                ArmatureManager.RenderCharacterProfiles(activeProfiles);

                //then I guess if we've got any global settings re: npcs or whatever
                //THEN we can scrape the object table for applications?

                //for (var i = 0; i < DalamudServices.ObjectTable.Length; i++)
                //{
                //	// Always filler Event obj
                //	if (i == 245)
                //		continue;

                //	// Removed setting until new UI is done
                //	// Don't affect EventNPCs when they are given an index above the player range, like in big cities, by config
                //	// if (i > 245 && !Configuration.ApplyToNpcsInBusyAreas) 
                //	//	continue;

                //	// Don't affect the cutscene object range, by configuration.
                //	// 202 gives leeway as player is not always put in 200 like they should be.
                //	if (i >= 202 && i < 240 && !Config.DefaultRenderingRules.For(EntityType.NPC).ApplyInCutscenes)
                //		continue;

                //	var obj = DalamudServices.ObjectTable[i];

                //	if (obj == null)
                //		continue;

                //	try
                //	{
                //		(BodyScale? scale, bool applyRootScale) = GameDataHelper.FindScale(i);
                //		if (scale == null)
                //			continue;
                //		scale.ApplyNonRootBonesAndRootScale(obj, applyRootScale);
                //	}
                //	catch (Exception ex)
                //	{
                //		PluginLog.LogError($"Error during update:{ex}");
                //		continue;
                //	}
                //}
            }
            catch (Exception e)
            {
                PluginLog.Error($"Error in CustomizePlus render hook {e}");
                _renderManagerHook?.Disable();
            }

            return original(a1, a2, a3, a4);
        }

        //todo: doesn't work in cutscenes, something getting called after this and resets changes
        private static void OnGameObjectMove(IntPtr gameObjectPtr)
        {
            // Call the original function.
            _gameObjectMovementHook.Original(gameObjectPtr);

            //If GPose and a 3rd-party posing service are active simultneously, abort
            if (GPoseService.Instance.GPoseState == GPoseState.Inside
                && PosingModeDetectService.Instance.IsInPosingMode)
            {
                return;
            }

            if (DalamudServices.ObjectTable.CreateObjectReference(gameObjectPtr) is var obj && obj != null)
            {
                var objIndex = obj.ObjectIndex;

                bool isForbiddenFiller = objIndex == Constants.ObjectTableFillerIndex;
                bool isForbiddenCutsceneNPC = Constants.IsInObjectTableCutsceneNPCRange(objIndex)
                                           && !ConfigurationManager.Configuration.ApplyToNPCsInCutscenes;

                //TODO none of this should really be necessary? the armature should already be
                //keeping track of its own visibility wrt rules and such

                if (!isForbiddenFiller && !isForbiddenCutsceneNPC)
                {
                    ArmatureManager.RenderArmatureByObject(obj);
                }
            }
        }
    }
}