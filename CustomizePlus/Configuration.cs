// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Collections.Generic;
	using Dalamud.Configuration;

	[Serializable]
	public class Configuration : IPluginConfiguration
	{
		public int Version { get; set; } = 0;
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
		public bool EditBodyBones { get; set; } = true;
		public bool EditAccessoryBones { get; set; } = false;
		public bool EditClothBones { get; set; } = false;
		public bool EditArmorBones { get; set; } = false;
		public bool EditWeaponBones { get; set; } = false;

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
