// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Data.Configuration;
using CustomizePlus.Data.Configuration.Interfaces;
using CustomizePlus.Data.Configuration.Version0;
using CustomizePlus.Helpers;
using Dalamud.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Util
{
	/// <summary>
	/// Configuration manager. Implemented because dalamud can't handle several configuration classes in a single plugin properly.
	/// </summary>
	public class ConfigurationManager
	{
		public PluginConfiguration Configuration { get; private set; }

		public ConfigurationManager()
		{
			this.Configuration = new PluginConfiguration();
		}

		public void CreateNewConfiguration()
		{
			Configuration = new PluginConfiguration(); 
		}

		public void SaveConfiguration(string? path = null)
		{
			string json = JsonConvert.SerializeObject(Configuration, Formatting.Indented);
			File.WriteAllText(path ?? DalamudServices.PluginInterface.ConfigFile.FullName, json);
		}

		public void LoadConfigurationFromFile(string path)
		{
			if (!Path.Exists(path))
			{
				throw new FileNotFoundException("Specified config path is invalid");
			}

			this.Configuration = ConvertConfigIfNeeded(path);

			if (this.Configuration != null)
			{
				SaveConfiguration();
			}
			else
			{
				Configuration = JsonConvert.DeserializeObject<PluginConfiguration>(File.ReadAllText(path));
			}
		}

		// Used any time a scale is added or enabled to ensure multiple scales for a single character
		// aren't on at the same time.
		public void ToggleOffAllOtherMatching(BodyScale bs)
		{
			foreach(BodyScale offScale in Configuration.BodyScales
				.Where(x => x.CharacterName == bs.CharacterName && x.ScaleName != bs.ScaleName))
			{
				offScale.BodyScaleEnabled = false;
			}

			SaveConfiguration();
		}

		private PluginConfiguration? ConvertConfigIfNeeded(string path)
		{
			int? currentConfigVersion = GetCurrentConfigurationVersion();
			if (currentConfigVersion == null || currentConfigVersion == PluginConfiguration.CurrentVersion)
			{
				return null;
			}	

			//TODO: In the future this will need to be rewritten to properly handle multiversion upgrades
			ILegacyConfiguration? legacyConfiguration = null;
			if (currentConfigVersion == 0)
				legacyConfiguration = Version0Configuration.LoadFromFile(path);

			if (legacyConfiguration == null)
				return null;

			PluginConfiguration configuration = legacyConfiguration.ConvertToLatestVersion();

			if (configuration == null)
				return null;

			string legacyConfigName = $"{Path.GetFileNameWithoutExtension(path)}_v{currentConfigVersion}{Path.GetExtension(path)}";
			File.Copy(path, Path.Combine(Path.GetDirectoryName(path), legacyConfigName));
			PluginLog.Information($"Customize+ legacy config copy saved to {legacyConfigName}");

			ChatHelper.PrintInChat($"Configuration has been updated to latest version, the copy of your old configuration file was saved to {legacyConfigName}");

			return configuration;
		}

		/// <summary>
		/// Returns current configuration file version, returns null if configuration file does not exist.
		/// </summary>
		private int? GetCurrentConfigurationVersion()
		{
			if (!DalamudServices.PluginInterface.ConfigFile.Exists)
				return null;

			return JsonConvert.DeserializeObject<ConfigurationVersion>(File.ReadAllText(DalamudServices.PluginInterface.ConfigFile.FullName)).Version;
		}
	}
}