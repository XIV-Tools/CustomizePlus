// © Customize+.
// Licensed under the MIT license.

using System;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using CustomizePlus.Data;
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
        public const string GetCharacterProfileLegacyLabel = "CustomizePlus.GetBodyScale";
        public const string GetCharacterProfileLabel = $"CustomizePlus.{nameof(SetCharacterProfile)}";
        public const string GetProfileFromCharacterLabel = $"CustomizePlus.{nameof(GetProfileFromCharacter)}";
        public const string SetCharacterProfileLegacyLabel = "CustomizePlus.SetBodyScale";
        public const string SetCharacterProfileLabel = $"CustomizePlus.{nameof(SetCharacterProfile)}";
        public const string SetProfileToCharacterLabel = $"CustomizePlus.{nameof(SetProfileToCharacter)}";
        public const string RevertLabel = $"CustomizePlus.{nameof(Revert)}";
        public const string RevertCharacterLabel = $"CustomizePlus.{nameof(RevertCharacter)}";
        public const string OnProfileUpdateLabel = $"CustomizePlus.{nameof(OnProfileUpdate)}";
        public static readonly string ApiVersion = "2.0";
        private readonly ObjectTable _objectTable;
        private readonly DalamudPluginInterface _pluginInterface;
        internal ICallGateProvider<string>? ProviderGetApiVersion;


        internal ICallGateProvider<string, string?>? ProviderGetCharacterProfileLegacy;
        internal ICallGateProvider<string, string?>? ProviderGetCharacterProfile;
        internal ICallGateProvider<Character?, string?>? ProviderGetProfileFromCharacter;

        internal ICallGateProvider<string?, object?>?
            ProviderOnProfileUpdate; //Sends either bodyscale string or null at startup and when scales are saved in the ui

        internal ICallGateProvider<string, object>? ProviderRevert;
        internal ICallGateProvider<Character?, object>? ProviderRevertCharacter;
        internal ICallGateProvider<string, string, object>? ProviderSetCharacterProfileLegacy;
        internal ICallGateProvider<string, string, object>? ProviderSetCharacterProfile;
        internal ICallGateProvider<string, Character?, object>? ProviderSetProfileToCharacter;

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
            ProviderGetCharacterProfileLegacy?.UnregisterFunc();
            ProviderGetCharacterProfile?.UnregisterFunc();
            ProviderGetProfileFromCharacter?.UnregisterFunc();
            ProviderSetCharacterProfileLegacy?.UnregisterAction();
            ProviderSetCharacterProfile?.UnregisterAction();
            ProviderSetProfileToCharacter?.UnregisterAction();
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
                ProviderGetCharacterProfileLegacy = _pluginInterface.GetIpcProvider<string, string?>(GetCharacterProfileLegacyLabel);
                ProviderGetCharacterProfileLegacy.RegisterFunc(GetCharacterProfile);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {GetCharacterProfileLegacyLabel}.");
            }

            try
            {
                ProviderGetCharacterProfile = _pluginInterface.GetIpcProvider<string, string?>(GetCharacterProfileLabel);
                ProviderGetCharacterProfile.RegisterFunc(GetCharacterProfile);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {GetCharacterProfileLabel}.");
            }

            try
            {
                ProviderGetProfileFromCharacter =
                    _pluginInterface.GetIpcProvider<Character?, string?>(GetProfileFromCharacterLabel);
                ProviderGetProfileFromCharacter.RegisterFunc(GetProfileFromCharacter);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {GetProfileFromCharacterLabel}.");
            }

            try
            {
                ProviderSetCharacterProfileLegacy =
                    _pluginInterface.GetIpcProvider<string, string, object>(SetCharacterProfileLegacyLabel);
                ProviderSetCharacterProfileLegacy.RegisterAction(SetCharacterProfile);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {SetCharacterProfileLegacyLabel}.");
            }

            try
            {
                ProviderSetCharacterProfile =
                    _pluginInterface.GetIpcProvider<string, string, object>(SetCharacterProfileLabel);
                ProviderSetCharacterProfile.RegisterAction(SetCharacterProfile);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {SetCharacterProfileLabel}.");
            }

            try
            {
                ProviderSetProfileToCharacter =
                    _pluginInterface.GetIpcProvider<string, Character?, object>(SetProfileToCharacterLabel);
                ProviderSetProfileToCharacter.RegisterAction(SetProfileToCharacter);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {SetProfileToCharacterLabel}.");
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
                ProviderOnProfileUpdate = _pluginInterface.GetIpcProvider<string?, object?>(OnProfileUpdateLabel);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {OnProfileUpdateLabel}.");
            }
        }

        public void OnProfileUpdate(string profileJson)
        {
            PluginLog.Debug("Sending c+ ipc profile message.");
            ProviderOnProfileUpdate?.SendMessage(profileJson);
        }

        private static string GetApiVersion()
        {
            return ApiVersion;
        }

        private string? GetCharacterProfile(string characterName)
        {
            var prof = Plugin.ProfileManager.GetProfileByCharacterName(characterName);
            return prof != null ? JsonConvert.SerializeObject(prof) : null;
        }

        private string? GetProfileFromCharacter(Character? character)
        {
            return character == null ? null : GetCharacterProfile(character.Name.ToString());
        }

        private void SetCharacterProfile(string profileJson, string characterName)
        {
            try
            {
                var prof = JsonConvert.DeserializeObject<CharacterProfile>(profileJson);
                if (prof != null)
                {
                    if (prof.ConfigVersion != Constants.ConfigurationVersion)
                        throw new Exception("Incompatible version");

                    Plugin.ProfileManager.AddTemporaryProfile(characterName, prof);
                }
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"Unable to set body profile. Character: {characterName}, exception: {ex}, debug data: {GetBase64String(profileJson)}");
            }
        }

        private void SetProfileToCharacter(string profileJson, Character? character)
        {
            if (character == null)
            {
                return;
            }

            SetCharacterProfile(profileJson, character.Name.ToString());
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

        private string GetBase64String(string data)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(json);
            using var compressedStream = new MemoryStream();
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                zipStream.Write(bytes, 0, bytes.Length);

            return Convert.ToBase64String(compressedStream.ToArray());
        }
    }
}