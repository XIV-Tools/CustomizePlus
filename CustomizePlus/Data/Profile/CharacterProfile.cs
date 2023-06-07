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
        [NonSerialized] private static int _nextGlobalId;

        [NonSerialized] private readonly int _localId;

        [NonSerialized] public Armature.Armature? Armature;

        [NonSerialized] public string? OriginalFilePath;

        public CharacterProfile()
        {
            _localId = _nextGlobalId++;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CharacterProfile" /> class by
        ///     creating a deep copy of the one provided.
        /// </summary>
        public CharacterProfile(CharacterProfile original) : this()
        {
            CharacterName = original.CharacterName;
            ProfileName = original.ProfileName;
            ConfigVersion = original.ConfigVersion;
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

        public string CharacterName { get; set; } = "Default";
        public string CharName { set => CharacterName = value; }
        public string ProfileName { get; set; } = "Profile";
        public string ProfName { set => ProfileName = value; }
        public int ConfigVersion { get; set; } = Constants.ConfigurationVersion;
        public bool Enabled { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        [JsonIgnore] public int UniqueId => CreationDate.GetHashCode();

        public Dictionary<string, BoneTransform> Bones { get; init; } = new();

        public override string ToString()
        {
            return $"Profile '{ProfileName}' on {CharacterName}";
        }

        public string ToDebugString()
        {
            return $"Profile ({_localId}) '{ProfileName}' on {CharacterName}";
        }

        public override bool Equals(object? obj)
        {
            return obj is CharacterProfile other
                ? UniqueId == other.UniqueId
                  && CharacterName == other.CharacterName
                  && ProfileName == other.ProfileName
                : base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return UniqueId;
        }
    }
}