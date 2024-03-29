﻿// © Customize+.
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
using Dalamud.Plugin.Services;
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

        public static CustomizePlusIpc IPCManager = null!;

        private static Hook<RenderDelegate>? _renderManagerHook;
        private static Hook<GameObjectMovementDelegate>? _gameObjectMovementHook;

        private delegate nint RenderDelegate(nint a1, nint a2, int a3, int a4);

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

                DalamudServices.Framework.RunOnFrameworkThread(() =>
                {
                    ServiceManager.Start();
                    DalamudServices.Framework.Update += Framework_Update;
                });

                IPCManager = new CustomizePlusIpc(DalamudServices.ObjectTable, DalamudServices.PluginInterface);

                ReloadHooks();

                DalamudServices.CommandManager.AddCommand((s, t) => MainWindow.Toggle(), "/customize",
                    "Toggles the Customize+ configuration window.");
                DalamudServices.CommandManager.AddCommand((s, t) => MainWindow.Toggle(), "/c+",
                    "Toggles the Customize+ configuration window.");
                DalamudServices.CommandManager.AddCommand((s, t) => ApplyByCommand(t), "/customize-apply",
                    "Apply a specific Scale (usage: /customize-apply {Character Name},{Scale Name})\nAllows for the usage of <me>/self and <t>/target.");
                DalamudServices.CommandManager.AddCommand((s, t) => ApplyByCommand(t), "/capply",
                    "Alias to /customize-apply");

                DalamudServices.PluginInterface.UiBuilder.Draw += InterfaceManager.Draw;
                DalamudServices.PluginInterface.UiBuilder.OpenConfigUi += MainWindow.Toggle;

                if (DalamudServices.PluginInterface.IsDevMenuOpen && ConfigurationManager.Configuration.DebuggingModeEnabled)
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

            IPCManager?.Dispose();

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
                        _renderManagerHook = DalamudServices.Hooker.HookFromAddress<RenderDelegate>(renderAddress, OnRender);
                        PluginLog.Debug("Render hook established");
                    }

                    if (_gameObjectMovementHook == null)
                    {
                        var movementAddress = DalamudServices.SigScanner.ScanText(Constants.MovementHookAddress);
                        _gameObjectMovementHook = DalamudServices.Hooker.HookFromAddress<GameObjectMovementDelegate>(movementAddress, OnGameObjectMove);
                        PluginLog.Debug("Movement hook established");
                    }

                    PluginLog.Debug("Hooking render & movement functions");
                    _renderManagerHook.Enable();
                    _gameObjectMovementHook.Enable();

                    PluginLog.Debug("Hooking render manager");
                    _renderManagerHook.Enable();

                    //Send current player's profile update message to IPC
                    IPCManager.OnProfileUpdate(null);
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

        private static void Framework_Update(IFramework framework)
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

                charaName = charaName switch {
                    "<me>"  => GameDataHelper.GetPlayerName() ?? string.Empty,
                    "self" => GameDataHelper.GetPlayerName() ?? string.Empty,
                    "<t>"   => GameDataHelper.GetPlayerTargetName() ?? string.Empty,
                    "target" => GameDataHelper.GetPlayerTargetName() ?? string.Empty,
                    _ => charaName,
                };

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

                var outProf = ProfileManager.Profiles.FirstOrDefault(x => x.ProfileName == profName && x.CharacterName == charaName);

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

        private static nint OnRender(nint a1, nint a2, int a3, int a4)
        {
            if (_renderManagerHook == null)
            {
                throw new Exception();
            }

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

            return _renderManagerHook.Original(a1, a2, a3, a4);
        }

        //todo: doesn't work in cutscenes, something getting called after this and resets changes
        private unsafe static void OnGameObjectMove(IntPtr gameObjectPtr)
        {
            // Call the original function.
            _gameObjectMovementHook.Original(gameObjectPtr);

            // If GPose and a 3rd-party posing service are active simultneously, abort
            if (GameStateHelper.GameInPosingMode())
            {
                return;
            }

            if (DalamudServices.ObjectTable.CreateObjectReference(gameObjectPtr) is var obj
                && obj != null
                && ProfileManager.GetProfilesByGameObject(obj).FirstOrDefault(x => x.Enabled) is CharacterProfile prof
                && prof != null
                && prof.Armature != null)
            {
                prof.Armature.ApplyRootTranslation(obj.ToCharacterBase());
            }
        }
    }
}