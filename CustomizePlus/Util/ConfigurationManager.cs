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

		public void CreateNewConfiguration()
		{
			Configuration = new PluginConfiguration(); 
		}

		public void SaveConfiguration()
		{
			Plugin.PluginInterface.SavePluginConfig(Configuration);
		}

		public void LoadConfigurationFromFile(string path)
		{
			if (!Path.Exists(path))
				throw new FileNotFoundException("Specified config path is invalid");

			Configuration = ConvertConfigIfNeeded(path);
			if (Configuration != null)
				SaveConfiguration();
			else
				Configuration = JsonConvert.DeserializeObject<PluginConfiguration>(File.ReadAllText(path));
		}

		// Used any time a scale is added or enabled to ensure multiple scales for a single character
		// aren't on at the same time.
		public void ToggleOffAllOtherMatching(string characterName, string highlanderScaleName)
		{
			if (highlanderScaleName == null)
			{
				highlanderScaleName = string.Empty;
			}

			foreach (BodyScale scale in Configuration.BodyScales)
			{
				if (characterName == scale.CharacterName && highlanderScaleName != scale.ScaleName)
				{
					scale.BodyScaleEnabled = false;
				}
			}

			SaveConfiguration();
		}

		private PluginConfiguration ConvertConfigIfNeeded(string path)
		{
			int? currentConfigVersion = GetCurrentConfigurationVersion();
			if (currentConfigVersion == null || currentConfigVersion == PluginConfiguration.CurrentVersion)
				return null;

			ILegacyConfiguration legacyConfiguration = null;
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
		/// Returns current configuration file version, returns null if configuration file does not exist
		/// </summary>
		/// <returns></returns>
		private int? GetCurrentConfigurationVersion()
		{
			if (!Plugin.PluginInterface.ConfigFile.Exists)
				return null;

			return JsonConvert.DeserializeObject<ConfigurationVersion>(File.ReadAllText(Plugin.PluginInterface.ConfigFile.FullName)).Version;
		}
	}
}