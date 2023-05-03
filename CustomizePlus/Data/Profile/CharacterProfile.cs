// © Customize+.
// Licensed under the MIT license.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;
using Newtonsoft.Json.Linq;
using System.Numerics;

namespace CustomizePlus.Data
{
	/// <summary>
	/// Encapsulates the user-controlled aspects of a character profile, ie all of
	/// the information that gets saved to disk by the plugin.
	/// </summary>
	[Serializable]
	public class CharacterProfile
	{
		public string CharacterName { get; set; } = "Default";
		public string ProfileName { get; set; } = "Profile";
		public bool Enabled { get; set; } = false;
		public DateTime CreationDate { get; set; } = DateTime.Now;
		public int UniqueID => this.CreationDate.GetHashCode();

		public Dictionary<string, BoneTransform> Bones { get; init; } = new();

		public override string ToString()
		{
			return $"Profile '{this.ProfileName}' on {this.CharacterName}";
		}
	}
}
