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
	}
}
