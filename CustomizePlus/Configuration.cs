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

		public void Save()
		{
			Plugin.PluginInterface.SavePluginConfig(this);
		}

		public void ToggleOffAllOtherMatching(String characterName, String highlanderScaleName)
		{
			if (highlanderScaleName == null)
			{
				highlanderScaleName = String.Empty;
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
