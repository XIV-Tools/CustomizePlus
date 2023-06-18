// © Customize+.
// Licensed under the MIT license.

using System;
using System.IO;
using CustomizePlus.Data.Configuration.Version0;
using CustomizePlus.Data.Configuration.Version2;
using CustomizePlus.Helpers;
using Dalamud.Logging;
using Newtonsoft.Json;

namespace CustomizePlus.Data.Configuration
{
    /// <summary>
    ///     Configuration manager. Implemented because dalamud can't handle several configuration classes in a single plugin
    ///     properly.
    /// </summary>
    public class ConfigurationManager
    {
        public ConfigurationManager()
        {
            Configuration = new PluginConfiguration();
            ReloadConfiguration();
        }

        public PluginConfiguration Configuration { get; private set; }
        private static string ConfigFilePath => DalamudServices.PluginInterface.ConfigFile.FullName;

        /// <summary>
        /// Gets Dalamud's pre-determined location for storing files related to Customize+.
        /// </summary>
        public static string ConfigDirectory => DalamudServices.PluginInterface.GetPluginConfigDirectory();

        public void SaveConfiguration()
        {
            var json = JsonConvert.SerializeObject(Configuration, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }

        public void ReloadConfiguration()
        {
            try
            {
                LoadConfigurationFromFile(ConfigFilePath);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Unable to load plugin config");
                ChatHelper.PrintInChat(
                    "There was an error while loading plugin configuration, details have been printed into the Dalamud console.");
            }
        }

        public void LoadConfigurationFromFile(string path)
        {
            if (!Path.Exists(path))
            {
                throw new FileNotFoundException("Specified config path is invalid");
            }

            Configuration = ConvertConfigIfNeeded(path);
            SaveConfiguration();
        }

        private static PluginConfiguration ConvertConfigIfNeeded(string path)
        {
            var configVersion = GetCurrentConfigurationVersion();

            PluginConfiguration output;

            switch (configVersion)
            {
                case 0:
                    BackupLegacyConfiguration(path);
                    var legacyConfig0 = new Version0Configuration();
                    output = legacyConfig0.LoadFromFile(path).ConvertToLatestVersion();
                    break;

                case 2:
                    BackupLegacyConfiguration(path);
                    var legacyConfig2 = new Version2Configuration();
                    output = legacyConfig2.LoadFromFile(path).ConvertToLatestVersion();
                    break;

                case 3:
                    output = JsonConvert.DeserializeObject<PluginConfiguration>(File.ReadAllText(path))
                             ?? new PluginConfiguration();
                    break;

                default:
                    output = new PluginConfiguration();
                    break;
            }

            if (configVersion != null && configVersion < Constants.ConfigurationVersion)
            {
                ChatHelper.PrintInChat("Configuration backed up and updated to newest version.");
            }

            return output;
        }

        /// <summary>
        ///     Returns current configuration file version, returns null if configuration file does not exist.
        /// </summary>
        private static int? GetCurrentConfigurationVersion()
        {
            return !DalamudServices.PluginInterface.ConfigFile.Exists
                ? null
                : JsonConvert
                    .DeserializeObject<ConfigurationVersion>(
                        File.ReadAllText(DalamudServices.PluginInterface.ConfigFile.FullName)).Version;
        }

        private static void BackupLegacyConfiguration(string path)
        {
            string backupPath = Path.Combine(ConfigDirectory, Path.GetFileName(path));

            int increment = 1;

            backupPath = IncrementBackupPath(backupPath, increment);

            while(File.Exists(backupPath))
            {
                ++increment;
                backupPath = IncrementBackupPath(path, increment);
            }

            File.Copy(path, backupPath);
        }

        private static string IncrementBackupPath(string path, int inc)
        {
            return Path.Combine(Path.GetDirectoryName(path),
                $"{Path.GetFileNameWithoutExtension(path)}_LegacyBackup{inc}{Path.GetExtension(path)}");
        }
    }
}