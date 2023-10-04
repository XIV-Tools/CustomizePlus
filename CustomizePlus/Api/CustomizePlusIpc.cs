// © Customize+.
// Licensed under the MIT license.

using System;
using System.IO.Compression;
using System.IO;
using System.Text;
using CustomizePlus.Data;
using CustomizePlus.Data.Profile;
using Dalamud.Plugin.Services;
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
        public const string GetProfileFromCharacterLabel = $"CustomizePlus.{nameof(GetProfileFromCharacter)}";
        public const string SetProfileToCharacterLabel = $"CustomizePlus.{nameof(SetProfileToCharacter)}";
        public const string RevertCharacterLabel = $"CustomizePlus.{nameof(RevertCharacter)}";
        public const string OnProfileUpdateLabel = $"CustomizePlus.{nameof(OnProfileUpdate)}";
        public static readonly (int, int) ApiVersion = (3, 0);
        private readonly IObjectTable _objectTable;
        private readonly DalamudPluginInterface _pluginInterface;


        //Sends local player's profile on hooks reload (plugin startup) as well as any updates to their profile.
        //If no profile is applied sends null
        internal ICallGateProvider<string?, string?, object?>? ProviderOnProfileUpdate;
        internal ICallGateProvider<Character?, object>? ProviderRevertCharacter;
        internal ICallGateProvider<string, Character?, object>? ProviderSetProfileToCharacter;
        internal ICallGateProvider<Character?, string?>? ProviderGetProfileFromCharacter;
        internal ICallGateProvider<(int, int)>? ProviderGetApiVersion;

        public CustomizePlusIpc(IObjectTable objectTable, DalamudPluginInterface pluginInterface)
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
            ProviderGetProfileFromCharacter?.UnregisterFunc();
            ProviderSetProfileToCharacter?.UnregisterAction();
            ProviderRevertCharacter?.UnregisterAction();
            ProviderGetApiVersion?.UnregisterFunc();
            ProviderOnProfileUpdate?.UnregisterFunc();
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
                ProviderOnProfileUpdate = _pluginInterface.GetIpcProvider<string?, string?, object?>(OnProfileUpdateLabel);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error registering IPC provider for {OnProfileUpdateLabel}.");
            }
        }

        public void OnProfileUpdate(CharacterProfile? profile)
        {
            //Get player's body profile string and send IPC message
            PluginLog.Debug($"Sending local player update message: {profile?.ProfileName ?? "no profile"} - {profile?.CharacterName ?? "no profile"}");
            ProviderOnProfileUpdate?.SendMessage(profile?.CharacterName ?? null, profile == null ? null : JsonConvert.SerializeObject(profile));
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

        private void SetCharacterProfile(string profileJson, nint address)
        {
            try
            {
                var prof = JsonConvert.DeserializeObject<CharacterProfile>(profileJson);
                if (prof != null)
                {
                    if (prof.ConfigVersion != Constants.ConfigurationVersion)
                        throw new Exception("Incompatible version");

                    prof.OwnedOnly = false;

                    Plugin.ProfileManager.AddTemporaryProfile(address, prof);
                }
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"Unable to set body profile. Address: {address}, exception: {ex}, debug data: {GetBase64String(profileJson)}");
            }
        }

        private void SetProfileToCharacter(string profileJson, Character? character)
        {
            if (character == null)
            {
                return;
            }

            SetCharacterProfile(profileJson, character.Address);
        }

        private void RevertCharacter(Character? character)
        {
            if (character == null)
            {
                return;
            }

            Plugin.ProfileManager.RemoveTemporaryProfile(character.Address);
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