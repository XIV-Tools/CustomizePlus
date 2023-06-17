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

        private static CustomizePlusIpc _ipcManager = null!;

        private static Hook<RenderDelegate>? _renderManagerHook;
        private static Hook<GameObjectMovementDelegate>? _gameObjectMovementHook;

        private delegate IntPtr RenderDelegate(IntPtr a1, long a2, int a3, int a4);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void GameObjectMovementDelegate(IntPtr gameObject);


        public Plugin(DalamudPluginInterface pluginInterface)
        {
            try
            {
                DalamudServices.Initialize(pluginInterface);
                DalamudServices.PluginInterface.UiBuilder.DisableGposeUiHide = true;

                ProfileManager.ProcessConvertedProfiles();
                ProfileManager.LoadProfiles();
                ProfileManager.CompleteInitialization();

                ReloadHooks();

                DalamudServices.Framework.RunOnFrameworkThread(() =>
                {
                    ServiceManager.Start();
                    DalamudServices.Framework.Update += Framework_Update;
                });

                _ipcManager = new CustomizePlusIpc(DalamudServices.ObjectTable, DalamudServices.PluginInterface);

                DalamudServices.CommandManager.AddCommand((s, t) => MainWindow.Toggle(), "/customize",
                    "Toggles the Customize+ configuration window.");
                DalamudServices.CommandManager.AddCommand((s, t) => MainWindow.Toggle(), "/c+",
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
        public static void ReloadHooks()
        {
            try
            {
                if (ConfigurationManager.Configuration.PluginEnabled)
                {
                    if (_renderManagerHook == null)
                    {
                        var renderAddress = DalamudServices.SigScanner.ScanText(Constants.RenderHookAddress);
                        _renderManagerHook = Hook<RenderDelegate>.FromAddress(renderAddress, OnRender);
                        PluginLog.Debug("Render hook established");
                    }

                    if (_gameObjectMovementHook == null)
                    {
                        var movementAddress = DalamudServices.SigScanner.ScanText(Constants.MovementHookAddress);
                        _gameObjectMovementHook =
                            Hook<GameObjectMovementDelegate>.FromAddress(movementAddress, OnGameObjectMove);
                        PluginLog.Debug("Movement hook established");
                    }

                    PluginLog.Debug("Hooking render & movement functions");
                    _renderManagerHook.Enable();
                    _gameObjectMovementHook.Enable();

                    PluginLog.Debug("Hooking render manager");
                    _renderManagerHook.Enable();
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
            //Get player's body profile string and send IPC message
            if (GameDataHelper.GetPlayerName() is string name && name != null)
            {
                if (ProfileManager.GetProfileByCharacterName(name) is CharacterProfile prof && prof != null)
                {
                    _ipcManager.OnProfileUpdate(JsonConvert.SerializeObject(prof));
                }
            }
        }

        private static void Framework_Update(Framework framework)
        {
            ServiceManager.Tick();
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
                    ProfileManager.Profiles.FirstOrDefault(x =>
                        x.ProfileName == profName && x.CharacterName == charaName);

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

                PluginLog.Debug(
                    $"{outProf.ProfileName} were successfully applied to {outProf.CharacterName} by command");
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
            }
            catch (Exception e)
            {
                PluginLog.Error($"Error in CustomizePlus render hook {e}");
                _renderManagerHook?.Disable();
            }

            return original(a1, a2, a3, a4);
        }

        //todo: doesn't work in cutscenes, something getting called after this and resets changes
        private unsafe static void OnGameObjectMove(IntPtr gameObjectPtr)
        {
            // Call the original function.
            _gameObjectMovementHook.Original(gameObjectPtr);

            ////If GPose and a 3rd-party posing service are active simultneously, abort
            //if (GameStateHelper.GameInPosingMode())
            //{
            //    return;
            //}

            //if (DalamudServices.ObjectTable.CreateObjectReference(gameObjectPtr) is var obj
            //    && obj != null
            //    && ProfileManager.GetProfilesByGameObject(obj) .FirstOrDefault(x => x.Enabled) is CharacterProfile prof
            //    && prof != null
            //    && prof.Armature != null)
            //{
            //    prof.Armature.ApplyRootTranslation(obj.ToCharacterBase());

            //    //var objIndex = obj.ObjectIndex;

            //    //var isForbiddenFiller = objIndex == Constants.ObjectTableFillerIndex;
            //    //var isForbiddenCutsceneNPC = Constants.IsInObjectTableCutsceneNPCRange(objIndex)
            //    //                             || !ConfigurationManager.Configuration.ApplyToNPCsInCutscenes;

            //    //if (!isForbiddenFiller && !isForbiddenCutsceneNPC)
            //    //{
            //    //    ArmatureManager.RenderArmatureByObject(obj);
            //    //}
            //}
        }
    }
}