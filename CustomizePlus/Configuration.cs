// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Dalamud.Configuration;
	using Newtonsoft.Json;

	[Serializable]
	public class Configuration : IPluginConfiguration
	{
		/// <summary>
		/// This usually should match ConfigurationInterface.scaleVersion
		/// </summary>
		public const int CurrentVersion = 2;

		public int Version { get; set; } = CurrentVersion;
		public List<BodyScale> BodyScales { get; set; } = new();
		public bool Enable { get; set; } = true;
		public bool AutomaticEditMode { get; set; } = false;

		public bool ApplyToNpcs { get; set; } = true;
		// public bool	ApplyToNpcsInBusyAreas { get; set; } = false;
		public bool ApplyToNpcsInCutscenes { get; set; } = true;

		public bool DebuggingMode { get; set; } = false;

		// Upcoming feature
		/*
		public bool GroupByScale { get; set; } = false;
		public bool GroupByCharacter { get; set; } = false;
		*/

		public void Save()
		{
			Plugin.PluginInterface.SavePluginConfig(this);
		}

		// Used any time a scale is added or enabled to ensure multiple scales for a single character
		// aren't on at the same time.
		public void ToggleOffAllOtherMatching(string characterName, string highlanderScaleName)
		{
			if (highlanderScaleName == null)
			{
				highlanderScaleName = string.Empty;
			}

			foreach (BodyScale scale in this.BodyScales)
			{
				if (characterName == scale.CharacterName && highlanderScaleName != scale.ScaleName)
				{
					scale.BodyScaleEnabled = false;
				}
			}

			Plugin.PluginInterface.SavePluginConfig(this);
		}
	}
}
