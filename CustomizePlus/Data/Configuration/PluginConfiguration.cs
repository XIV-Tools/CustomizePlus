// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using CustomizePlus.Interface;
using Dalamud.Configuration;

namespace CustomizePlus.Data.Configuration
{
	[Serializable]
	public class PluginConfiguration : IPluginConfiguration
	{
		public const int CurrentVersion = Constants.ConfigurationVersion;

		public int Version { get; set; } = CurrentVersion;
		public HashSet<BodyScale> BodyScales { get; set; } = new();
		public bool Enable { get; set; } = true;
		public bool AutomaticEditMode { get; set; } = false;
		public bool MirrorMode { get; set; } = false;
		public EditMode EditingAttribute { get; set; } = EditMode.Scale;

		public bool ApplyToNpcs { get; set; } = true;
		// public bool	ApplyToNpcsInBusyAreas { get; set; } = false;
		public bool ApplyToNpcsInCutscenes { get; set; } = true;

		public bool DebuggingMode { get; set; } = false;
		public HashSet<string> ViewedMessageWindows = new();

		// Upcoming feature
		/*
		public bool GroupByScale { get; set; } = false;
		public bool GroupByCharacter { get; set; } = false;
		*/
	}
}
