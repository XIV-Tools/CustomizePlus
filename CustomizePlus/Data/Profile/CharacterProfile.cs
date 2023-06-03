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
using Newtonsoft.Json;

namespace CustomizePlus.Data.Profile
{
	/// <summary>
	/// Encapsulates the user-controlled aspects of a character profile, ie all of
	/// the information that gets saved to disk by the plugin.
	/// </summary>
	[Serializable]
	public sealed class CharacterProfile
	{
		[NonSerialized]
		private static int NextGlobalID = 0;
		[NonSerialized]
		private readonly int LocalID;

		public string CharName { get; set; } = "Default";
		public string ProfName { get; set; } = "Profile";
		public bool Enabled { get; set; } = false;
		public DateTime CreationDate { get; set; } = DateTime.Now;
		public DateTime ModifiedDate { get; set; } = DateTime.Now;

		[JsonIgnore]
		public int UniqueID => this.CreationDate.GetHashCode();

		[NonSerialized]
		public string? OriginalFilePath = null;
		[NonSerialized]
		public Armature.Armature? Armature = null;

		public Dictionary<string, BoneTransform> Bones { get; init; } = new();

		public CharacterProfile()
		{
			this.LocalID = NextGlobalID++;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CharacterProfile"/> class by
		/// creating a deep copy of the one provided.
		/// </summary>
		public CharacterProfile(CharacterProfile original) : this()
		{
			this.CharName = original.CharName;
			this.ProfName = original.ProfName;
			this.Enabled = original.Enabled;
			this.CreationDate = original.CreationDate;
			this.ModifiedDate = DateTime.Now;
			this.OriginalFilePath = original.OriginalFilePath;
			this.Armature = null;

			foreach(var kvp in original.Bones)
			{
				this.Bones[kvp.Key] = new BoneTransform();
				this.Bones[kvp.Key].UpdateToMatch(kvp.Value);
			}
		}

		public override string ToString()
		{
			return $"Profile ({this.LocalID}) '{this.ProfName}' on {this.CharName}";
		}

		public override bool Equals(object? obj)
		{
			if (obj is CharacterProfile other && other != null)
			{
				return this.UniqueID == other.UniqueID
					&& this.CharName == other.CharName
					&& this.ProfName == other.ProfName;
			}
			else
			{
				return base.Equals(obj);
			}
		}

		public override int GetHashCode()
		{
			return this.UniqueID;
		}
	}
}
