// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Data.Configuration.Interfaces;
using CustomizePlus.Data.Profile;
using Dalamud.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace CustomizePlus.Data.Configuration.Version2
{
	internal class Version2Configuration : IPluginConfiguration, ILegacyConfiguration
	{
		public int Version { get; set; } = 2;
		public List<Version2BodyScale> BodyScales { get; set; } = new();
		public bool Enable { get; set; } = true;
		public bool AutomaticEditMode { get; set; } = false;
		public bool MirrorMode { get; set; } = false;
		public Data.BoneAttribute EditingAttribute { get; set; } = 0;

		public bool ApplyToNpcs { get; set; } = true;
		public bool ApplyToNpcsInCutscenes { get; set; } = true;
		public bool DebuggingMode { get; set; } = false;

		public ILegacyConfiguration? LoadFromFile(string path)
		{
			if (!Path.Exists(path))
				throw new ArgumentException("Specified config path is invalid");

			return JsonConvert.DeserializeObject<Version2Configuration>(File.ReadAllText(path));
		}

		public PluginConfiguration ConvertToLatestVersion()
		{
			PluginConfiguration config = new PluginConfiguration()
			{
				Version = PluginConfiguration.CurrentVersion,
				PluginEnabled = this.Enable,
				DebuggingMode = this.DebuggingMode,
				ApplytoNPCs = this.ApplyToNpcs,
				ApplytoNPCsInCutscenes = this.ApplyToNpcsInCutscenes
			};

			foreach (var bodyScale in BodyScales)
			{
				var newProf = ProfileConverter.ConvertFromConfigV2(bodyScale);

				ProfileManager.ConvertedProfiles.Add(newProf);
			}

			return config;
		}
	}
}
