// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CustomizePlus.Data.Profile
{
    /// <summary>
    ///     Encapsulates the user-controlled aspects of a character profile, ie all of
    ///     the information that gets saved to disk by the plugin.
    /// </summary>
    [Serializable]
    public sealed class CharacterProfile
    {
        [NonSerialized] private static int NextGlobalID;

        [NonSerialized] private readonly int LocalID;

        [NonSerialized] public Armature.Armature? Armature;

        [NonSerialized] public string? OriginalFilePath;

        public CharacterProfile()
        {
            LocalID = NextGlobalID++;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CharacterProfile" /> class by
        ///     creating a deep copy of the one provided.
        /// </summary>
        public CharacterProfile(CharacterProfile original) : this()
        {
            CharName = original.CharName;
            ProfName = original.ProfName;
            Enabled = original.Enabled;
            CreationDate = original.CreationDate;
            ModifiedDate = DateTime.Now;
            OriginalFilePath = original.OriginalFilePath;
            Armature = null;

            foreach (var kvp in original.Bones)
            {
                Bones[kvp.Key] = new BoneTransform();
                Bones[kvp.Key].UpdateToMatch(kvp.Value);
            }
        }

        public string CharName { get; set; } = "Default";
        public string ProfName { get; set; } = "Profile";
        public bool Enabled { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        [JsonIgnore] public int UniqueID => CreationDate.GetHashCode();

        public Dictionary<string, BoneTransform> Bones { get; init; } = new();

        public override string ToString()
        {
            return $"Profile ({LocalID}) '{ProfName}' on {CharName}";
        }

        public override bool Equals(object? obj)
        {
            if (obj is CharacterProfile other && other != null)
            {
                return UniqueID == other.UniqueID
                       && CharName == other.CharName
                       && ProfName == other.ProfName;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return UniqueID;
        }
    }
}