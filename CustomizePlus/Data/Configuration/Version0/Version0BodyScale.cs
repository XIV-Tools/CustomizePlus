// © Customize+.
// Licensed under the MIT license.

using FFXIVClientStructs.Havok;
using System;
using System.Collections.Generic;
using CustomizePlus.Memory;

namespace CustomizePlus.Data.Configuration.Version0
{
	[Serializable]
	public class Version0BodyScale
	{
		public string CharacterName { get; set; } = string.Empty;
		public string ScaleName { get; set; } = string.Empty;
		public bool BodyScaleEnabled { get; set; } = true;
		public Dictionary<string, hkVector4f> Bones { get; } = new();
		public hkVector4f RootScale { get; set; } = new() { X = 0, Y = 0, Z = 0, W = 0 };
	}
}