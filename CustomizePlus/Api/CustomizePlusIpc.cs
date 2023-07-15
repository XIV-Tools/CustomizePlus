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
using CustomizePlus.Helpers;

namespace CustomizePlus.Api
{
    public class CustomizePlusIpc : IDisposable
    {
        public const string ProviderApiVersionLabel = $"CustomizePlus.{nameof(GetApiVersion)}";
        [Obsolete("To be removed in later versions after mare switches to new endpoints")]
        public const string GetCharacterProfileLegacyLabel = "CustomizePlus.GetBodyScale";
        public const string GetCharacterProfileLabel = $"CustomizePlus.{nameof(SetCharacterProfile)}";
        public const string GetProfileFromCharacterLabel = $"CustomizePlus.{nameof(GetProfileFromCharacter)}";
        [Obsolete("To be removed in later versions after mare switches to new endpoints")]
        public const string SetCharacterProfileLegacyLabel = "CustomizePlus.SetBodyScale";
        public const string SetCharacterProfileLabel = $"CustomizePlus.{nameof(SetCharacterProfile)}";
        public const string SetProfileToCharacterLabel = $"CustomizePlus.{nameof(SetProfileToCharacter)}";
        [Obsolete("To be removed in later versions after mare switches to new endpoints")]
        public const string SetProfileToCharacterLegacyLabel = "CustomizePlus.SetBodyScaleToCharacter";
        public const string RevertLabel = $"CustomizePlus.{nameof(Revert)}";
        public const string RevertCharacterLabel = $"CustomizePlus.{nameof(RevertCharacter)}";
        [Obsolete("To be removed in later versions after mare switches to new endpoints")]
        public const string OnOnLocalPlayerProfileLegacyLabel = "CustomizePlus.OnScaleUpdate";
        public const string OnLocalPlayerProfileUpdateLabel = $"CustomizePlus.{nameof(OnLocalPlayerProfileUpdate)}";
        public static readonly (int, int) ApiVersion = (3, 0);
        private readonly ObjectTable _objectTable;
        private readonly DalamudPluginInterface _pluginInterface;
        internal ICallGateProvider<(int, int)>? ProviderGetApiVersion;

        [Obsolete("To be removed in later versions after mare switches to new endpoints")]
        internal ICallGateProvider<string, string?>? ProviderGetCharacterProfileLegacy;
        internal ICallGateProvider<string, string?>? ProviderGetCharacterProfile;
        internal ICallGateProvider<Character?, string?>? ProviderGetProfileFromCharacter;

        //Sends local player's profile on hooks reload (plugin startup) as well as any updates to their profile.
        //If no profile is applied sends null
        [Obsolete("To be removed in later versions after mare switches to new endpoints")]
        internal ICallGateProvider<string?, object?>? ProviderOnLocalPlayerProfileLegacyUpdate;
        internal ICallGateProvider<string?, object?>? ProviderOnLocalPlayerProfileUpdate;

        internal ICallGateProvider<string, object>? ProviderRevert;
        internal ICallGateProvider<Character?, object>? ProviderRevertCharacter;
        [Obsolete("To be removed in later versions after mare switches to new endpoints")]
        internal ICallGateProvider<string, string, object>? ProviderSetCharacterProfileLegacy;
        internal ICallGateProvider<string, string, object>? ProviderSetCharacterProfile;
        [Obsolete("To be removed in later versions after mare switches to new endpoints")]
        internal ICallGateProvider<string, Character?, object>? ProviderSetProfileToCharacterLegacy;
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
            ProviderSetProfileToCharacterLegacy?.UnregisterAction();
            ProviderSetProfileToCharacter?.UnregisterAction();
            ProviderRevert?.UnregisterAction();
            ProviderRevertCharacter?.UnregisterAction();
            ProviderGetApiVersion?.UnregisterFunc();
            ProviderOnLocalPlayerProfileUpdate?.UnregisterFunc();
        }

        private void InitializeProviders()
        {
            PluginLog.Debug("Initializing c+ ipc providers.");
            try
            {
                ProviderGetApiVersion = _pluginInterface.GetIpcProvider<(int, int)>(ProviderApiVersionLabel);
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
                    _pluginInterface.GetIpcProvider<string, Character?, object>(SetProfileToCharacterLegacyLabel);
                ProviderSetProfileToCharacter.RegisterAction(SetProfileToCharacter);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {SetProfileToCharacterLegacyLabel}.");
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
                ProviderOnLocalPlayerProfileLegacyUpdate = _pluginInterface.GetIpcProvider<string?, object?>(OnOnLocalPlayerProfileLegacyLabel);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {OnOnLocalPlayerProfileLegacyLabel}.");
            }

            try
            {
                ProviderOnLocalPlayerProfileUpdate = _pluginInterface.GetIpcProvider<string?, object?>(OnLocalPlayerProfileUpdateLabel);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {OnLocalPlayerProfileUpdateLabel}.");
            }
        }

        public void OnLocalPlayerProfileUpdate()
        {
            //Get player's body profile string and send IPC message
            if (GameDataHelper.GetPlayerName() is string name && name != null)
            {
                CharacterProfile? profile = Plugin.ProfileManager.GetProfileByCharacterName(name, true);

                PluginLog.Debug($"Sending local player update message: {profile?.ProfileName ?? "no profile"} - {profile?.CharacterName ?? "no profile"}");
                ProviderOnLocalPlayerProfileUpdate?.SendMessage(profile != null ? JsonConvert.SerializeObject(profile) : null);
            }
        }

        private static (int, int) GetApiVersion()
        {
            return ApiVersion;
        }

        private string? GetCharacterProfile(string characterName)
        {
            var prof = Plugin.ProfileManager.GetProfileByCharacterName(characterName, true);
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