// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Data.Configuration.Interfaces;
using CustomizePlus.Data.Configuration.Version0;
using CustomizePlus.Data.Configuration.Version2;
using CustomizePlus.Helpers;
using Dalamud.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Data.Configuration
{
	/// <summary>
	/// Configuration manager. Implemented because dalamud can't handle several configuration classes in a single plugin properly.
	/// </summary>
	public class ConfigurationManager
	{
		public PluginConfiguration Configuration { get; private set; }
		private static string ConfigFilePath => DalamudServices.PluginInterface.ConfigFile.FullName;

		public ConfigurationManager()
		{
			this.Configuration = new PluginConfiguration();
			this.ReloadConfiguration();
		}

		public void SaveConfiguration()
		{
			string json = JsonConvert.SerializeObject(this.Configuration, Formatting.Indented);
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
				ChatHelper.PrintInChat("There was an error while loading plugin configuration, details have been printed into the Dalamud console.");
			}
		}

		public void LoadConfigurationFromFile(string path)
		{
			if (!Path.Exists(path))
			{
				throw new FileNotFoundException("Specified config path is invalid");
			}

			this.Configuration = ConvertConfigIfNeeded(path);
			this.SaveConfiguration();
		}

		private static PluginConfiguration ConvertConfigIfNeeded(string path)
		{
			int? configVersion = GetCurrentConfigurationVersion();

			PluginConfiguration output;

			switch (configVersion)
			{
				case 0:
					Version0Configuration legacyConfig0 = new Version0Configuration();
					output = legacyConfig0.LoadFromFile(path).ConvertToLatestVersion();
					break;

				case 2:
					Version2Configuration legacyConfig2 = new Version2Configuration();
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

			return output;
		}

		/// <summary>
		/// Returns current configuration file version, returns null if configuration file does not exist.
		/// </summary>
		private static int? GetCurrentConfigurationVersion()
		{
			if (!DalamudServices.PluginInterface.ConfigFile.Exists)
				return null;

			return JsonConvert.DeserializeObject<ConfigurationVersion>(File.ReadAllText(DalamudServices.PluginInterface.ConfigFile.FullName)).Version;
		}
	}
}