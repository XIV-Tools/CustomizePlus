// © Customize+.
// Licensed under the MIT license.

using System;
using System.IO;
using CustomizePlus.Helpers;
using Dalamud.Logging;
using Newtonsoft.Json;

namespace CustomizePlus.Data.Profile
{
    /// <summary>
    ///     Contains utilities for saving and loading <see cref="CharacterProfile" />s to disk,
    ///     as well as parsing profiles from previous plugin versions.
    /// </summary>
    public static class ProfileReaderWriter
    {
        #region Save/Load

        private static string CreateFileName(CharacterProfile prof)
        {
            var invalidCharacters = Path.GetInvalidFileNameChars();
            var fileName = $"{prof.CharacterName}-{prof.ProfileName}-{prof.UniqueId}.profile";
            fileName = string.Join(string.Empty,
                fileName.Split(invalidCharacters, StringSplitOptions.RemoveEmptyEntries));
            return fileName;
        }

        private static string CreatePath(string fileName)
        {
            var dir = DalamudServices.PluginInterface.GetPluginConfigDirectory();

            Directory.CreateDirectory(dir);

            return Path.GetFullPath($"{dir}\\{fileName}");
        }

        public static void SaveProfile(CharacterProfile prof, bool archival = false)
        {
            try
            {
                var oldFilePath = prof.OriginalFilePath ?? string.Empty;
                var newFilePath = CreatePath(CreateFileName(prof));

                if (!archival)
                {
                    var json = JsonConvert.SerializeObject(prof, Formatting.Indented);

                    File.WriteAllText(newFilePath, json);
                }
                else
                {
                    newFilePath += "_arch";
                    var text = Base64Helper.ExportToBase64(prof, Constants.ConfigurationVersion);

                    File.WriteAllText(newFilePath, text);
                }

                if (newFilePath != oldFilePath && oldFilePath != string.Empty)
                {
                    File.Delete(oldFilePath);
                }

                prof.OriginalFilePath = newFilePath;
            }
            catch (Exception ex)
            {
                PluginLog.LogError($"Error saving {prof}: {ex}");
            }
        }

        public static void DeleteProfile(CharacterProfile prof)
        {
            //SaveProfile(prof, true);
            if (CreatePath(CreateFileName(prof)) is string path && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static string[] GetProfilePaths()
        {
            var dir = DalamudServices.PluginInterface.GetPluginConfigDirectory();
            if (Directory.Exists(dir))
            {
                return Directory.GetFiles(dir, "*.profile");
            }

            Directory.CreateDirectory(dir);
            return Array.Empty<string>();
        }

        public static bool TryLoadProfile(string path, out CharacterProfile? prof)
        {
            try
            {
                if (Path.Exists(path))
                {
                    var file = JsonConvert.DeserializeObject<CharacterProfile>(File.ReadAllText(path));

                    if (file != null)
                    {
                        prof = file;
                        prof.OriginalFilePath = path;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.LogError($"Error loading character profile (from '{path}'): {ex}");
            }

            prof = null;
            return false;
        }

        #endregion
    }
}