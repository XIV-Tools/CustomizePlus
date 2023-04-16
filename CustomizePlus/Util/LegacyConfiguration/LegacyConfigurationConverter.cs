// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Interface.LegacyConfiguration.Data;
using CustomizePlus.Util.LegacyConfiguration.Data;
using CustomizePlus.Util.LegacyConfiguration.Data.Version0;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomizePlus.Util.LegacyConfiguration
{
	internal class LegacyConfigurationConverter
	{
		public void ConvertConfigIfNeeded()
		{
			int? currentConfigVersion = GetCurrentConfigVersion();
			if (currentConfigVersion == null || currentConfigVersion == Configuration.CurrentVersion)
				return;

			ILegacyConfiguration legacyConfiguration = null;
			if (currentConfigVersion == 0)
				legacyConfiguration = Version0Configuration.LoadFromFile(Plugin.PluginInterface.ConfigFile.FullName);

			if (legacyConfiguration == null)
				return;

			Configuration configuration = legacyConfiguration.ConvertToLatestVersion();

			if (configuration == null) 
				return;

			string legacyConfigName = $"{Path.GetFileNameWithoutExtension(Plugin.PluginInterface.ConfigFile.Name)}_v{currentConfigVersion}{Path.GetExtension(Plugin.PluginInterface.ConfigFile.FullName)}";
			File.Copy(Plugin.PluginInterface.ConfigFile.FullName, Path.Combine(Plugin.PluginInterface.ConfigFile.DirectoryName, legacyConfigName));
			PluginLog.Information($"Customize+ legacy config copy saved to {legacyConfigName}");

			configuration.Save();

			var stringBuilder = new SeStringBuilder();
			stringBuilder.AddUiForeground(45);
			stringBuilder.AddText($"Customize+ configuration has been updated to latest version, the copy of your old configuration file was saved to {legacyConfigName}");
			stringBuilder.AddUiForegroundOff();
			Plugin.ChatGui.Print(stringBuilder.BuiltString);
		}

		/// <summary>
		/// Returns current configuration file version, returns null if configuration file does not exist
		/// </summary>
		/// <returns></returns>
		private int? GetCurrentConfigVersion()
		{
			if (!Plugin.PluginInterface.ConfigFile.Exists)
				return null;

			return JsonConvert.DeserializeObject<ConfigurationVersion>(File.ReadAllText(Plugin.PluginInterface.ConfigFile.FullName)).Version;
		}
	}
}