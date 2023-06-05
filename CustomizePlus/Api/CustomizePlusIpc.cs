// © Customize+.
// Licensed under the MIT license.

using System;
using System.Linq;

using CustomizePlus.Data.Profile;

using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

using Newtonsoft.Json;

namespace CustomizePlus.Api
{
    public class CustomizePlusIpc : IDisposable
    {
        public const string ProviderApiVersionLabel = $"CustomizePlus.{nameof(GetApiVersion)}";
        public const string GetBodyScaleLabel = $"CustomizePlus.{nameof(GetBodyScale)}";
        public const string GetBodyScaleFromCharacterLabel = $"CustomizePlus.{nameof(GetBodyScaleFromCharacter)}";
        public const string SetBodyScaleLabel = $"CustomizePlus.{nameof(SetBodyScale)}";
        public const string SetBodyScaleToCharacterLabel = $"CustomizePlus.{nameof(SetBodyScaleToCharacter)}";
        public const string RevertLabel = $"CustomizePlus.{nameof(Revert)}";
        public const string RevertCharacterLabel = $"CustomizePlus.{nameof(RevertCharacter)}";
        public const string OnScaleUpdateLabel = $"CustomizePlus.{nameof(OnScaleUpdate)}";
        public static readonly string ApiVersion = "2.0";
        private readonly ObjectTable _objectTable;
        private readonly DalamudPluginInterface _pluginInterface;
        internal ICallGateProvider<string>? ProviderGetApiVersion;


        internal ICallGateProvider<string, string?>? ProviderGetBodyScale;
        internal ICallGateProvider<Character?, string?>? ProviderGetBodyScaleFromCharacter;

        internal ICallGateProvider<string?, object?>?
            ProviderOnScaleUpdate; //Sends either bodyscale string or null at startup and when scales are saved in the ui

        internal ICallGateProvider<string, object>? ProviderRevert;
        internal ICallGateProvider<Character?, object>? ProviderRevertCharacter;
        internal ICallGateProvider<string, string, object>? ProviderSetBodyScale;
        internal ICallGateProvider<string, Character?, object>? ProviderSetBodyScaleToCharacter;

        public CustomizePlusIpc(ObjectTable objectTable, DalamudPluginInterface pluginInterface)
        {
            _objectTable = objectTable;
            _pluginInterface = pluginInterface;

            InitializeProviders();
        }

        public void Dispose()
        {
            DisposeProviders();
        }

        private void DisposeProviders()
        {
            ProviderGetBodyScale?.UnregisterFunc();
            ProviderGetBodyScaleFromCharacter?.UnregisterFunc();
            ProviderSetBodyScale?.UnregisterAction();
            ProviderSetBodyScaleToCharacter?.UnregisterAction();
            ProviderRevert?.UnregisterAction();
            ProviderRevertCharacter?.UnregisterAction();
            ProviderGetApiVersion?.UnregisterFunc();
        }

        private void InitializeProviders()
        {
            PluginLog.Debug("Initializing c+ ipc providers.");
            try
            {
                ProviderGetApiVersion = _pluginInterface.GetIpcProvider<string>(ProviderApiVersionLabel);
                ProviderGetApiVersion.RegisterFunc(GetApiVersion);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {ProviderApiVersionLabel}.");
            }

            try
            {
                ProviderGetBodyScale = _pluginInterface.GetIpcProvider<string, string?>(GetBodyScaleLabel);
                ProviderGetBodyScale.RegisterFunc(GetBodyScale);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {GetBodyScaleLabel}.");
            }

            try
            {
                ProviderGetBodyScaleFromCharacter =
                    _pluginInterface.GetIpcProvider<Character?, string?>(GetBodyScaleFromCharacterLabel);
                ProviderGetBodyScaleFromCharacter.RegisterFunc(GetBodyScaleFromCharacter);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {GetBodyScaleFromCharacterLabel}.");
            }

            try
            {
                ProviderSetBodyScale =
                    _pluginInterface.GetIpcProvider<string, string, object>(SetBodyScaleLabel);
                ProviderSetBodyScale.RegisterAction(SetBodyScale);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {SetBodyScaleLabel}.");
            }

            try
            {
                ProviderSetBodyScaleToCharacter =
                    _pluginInterface.GetIpcProvider<string, Character?, object>(SetBodyScaleToCharacterLabel);
                ProviderSetBodyScaleToCharacter.RegisterAction(SetBodyScaleToCharacter);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {SetBodyScaleToCharacterLabel}.");
            }

            try
            {
                ProviderRevert =
                    _pluginInterface.GetIpcProvider<string, object>(RevertLabel);
                ProviderRevert.RegisterAction(Revert);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {RevertLabel}.");
            }

            try
            {
                ProviderRevertCharacter =
                    _pluginInterface.GetIpcProvider<Character?, object>(RevertCharacterLabel);
                ProviderRevertCharacter.RegisterAction(RevertCharacter);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {RevertCharacterLabel}.");
            }

            try
            {
                ProviderOnScaleUpdate = _pluginInterface.GetIpcProvider<string?, object?>(OnScaleUpdateLabel);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {OnScaleUpdateLabel}.");
            }
        }

        public void OnScaleUpdate(string bodyScaleString)
        {
            PluginLog.Debug("Sending c+ ipc scale message.");
            ProviderOnScaleUpdate?.SendMessage(bodyScaleString);
        }

        private static string GetApiVersion()
        {
            return ApiVersion;
        }

        private string? GetBodyScale(string characterName)
        {
            var prof = Plugin.ProfileManager.GetProfileByCharacterName(characterName);
            return prof != null ? JsonConvert.SerializeObject(prof) : null;
        }

        private string? GetBodyScaleFromCharacter(Character? character)
        {
            return character == null ? null : GetBodyScale(character.Name.ToString());
        }

        private void SetBodyScale(string bodyScaleString, string characterName)
        {
            var prof = JsonConvert.DeserializeObject<CharacterProfile>(bodyScaleString);
            if (prof != null)
            {
                Plugin.ProfileManager.AddTemporaryProfile(characterName, prof);
            }
        }

        private void SetBodyScaleToCharacter(string bodyScaleString, Character? character)
        {
            if (character == null)
            {
                return;
            }

            SetBodyScale(bodyScaleString, character.Name.ToString());
        }

        private void Revert(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                return;
            }

            Plugin.ProfileManager.RemoveTemporaryProfile(characterName);
        }

        private void RevertCharacter(Character? character)
        {
            if (character == null)
            {
                return;
            }

            Revert(character.Name.ToString());
        }

        private Character? FindCharacterByName(string? characterName)
        {
            return characterName == null
                ? null
                : _objectTable.FirstOrDefault(gameObject => gameObject.Name.ToString() == characterName) as Character;
        }
    }
}