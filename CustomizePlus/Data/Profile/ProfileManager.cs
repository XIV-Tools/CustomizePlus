// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;

namespace CustomizePlus.Data.Profile
{
    /// <summary>
    ///     Container class for administrating <see cref="CharacterProfile" />s during runtime.
    /// </summary>
    public class ProfileManager
    {
        /// <summary>
        ///     Config is loaded before the profile manager is necessarily instantiated.
        ///     In the case that a legacy config needs to be converted, this container can
        ///     hold any profiles parsed out of it because of its static nature.
        /// </summary>
        public static readonly HashSet<CharacterProfile> ConvertedProfiles = new();

        private readonly Dictionary<ObjectKind, CharacterProfile> defaultProfiles = new();

        public readonly HashSet<CharacterProfile> Profiles = new(new ProfileEquality());

        public readonly Dictionary<string, CharacterProfile> TempLocalProfiles = new();

        //public readonly HashSet<CharacterProfile> ProfilesOpenInEditor = new(new ProfileEquality());
        public CharacterProfile? ProfileOpenInEditor { get; private set; }

        public void LoadProfiles()
        {
            foreach (var path in ProfileReaderWriter.GetProfilePaths())
            {
                if (ProfileReaderWriter.TryLoadProfile(path, out var prof) && prof != null)
                {
                    PruneIdempotentTransforms(prof);

                    Profiles.Add(prof);
                    if (prof.Enabled)
                    {
                        AssertEnabledProfile(prof);
                    }
                }
            }
        }

        /// <summary>
        ///     Adds the given profile to the list of those managed, and immediately
        ///     saves it to disk. If the profile already exists (and it is not forced to be new)
        ///     the given profile will overwrite the old one.
        /// </summary>
        public void AddAndSaveProfile(CharacterProfile prof, bool forceNew = false)
        {
            PruneIdempotentTransforms(prof);

            //if the profile is already in the list, simply replace it
            if (!forceNew && Profiles.Remove(prof))
            {
                prof.ModifiedDate = DateTime.Now;
                Profiles.Add(prof);
                ProfileReaderWriter.SaveProfile(prof);
            }
            else
            {
                //otherwise it must be a new profile, obviously
                //in which case we update its creation date
                //(which incidentally prevents it from inheriting a hash code)
                //and add it to the list of managed profiles

                prof.CreationDate = DateTime.Now;
                prof.ModifiedDate = DateTime.Now;
                Profiles.Add(prof);
                ProfileReaderWriter.SaveProfile(prof);
            }
        }

        public void DeleteProfile(CharacterProfile prof)
        {
            if (Profiles.Remove(prof))
            {
                ProfileReaderWriter.DeleteProfile(prof);
            }
        }

        /// <summary>
        ///     Direct the manager to save every managed profile to disk.
        /// </summary>
        public void SaveAllProfiles()
        {
            foreach (var prof in Profiles)
            {
                PruneIdempotentTransforms(prof);

                ProfileReaderWriter.SaveProfile(prof);
            }
        }

        public void AssertEnabledProfile(CharacterProfile activeProfile)
        {
            activeProfile.Enabled = true;

            foreach (var profile in Profiles
                         .Where(x => x.CharName == activeProfile.CharName && x != activeProfile))
            {
                profile.Enabled = false;
            }
        }

        /// <summary>
        ///     Mark the given profile (if any) as currently being edited, and return
        ///     a copy that can be safely mangled without affecting the old one.
        /// </summary>
        public bool GetWorkingCopy(CharacterProfile prof, out CharacterProfile? copy)
        {
            if (prof != null && ProfileOpenInEditor != prof)
            {
                copy = new CharacterProfile(prof);

                PruneIdempotentTransforms(copy);
                ProfileOpenInEditor = copy;
                return true;
            }

            copy = null;
            return false;
        }

        public void SaveWorkingCopy(CharacterProfile prof, bool editingComplete = false)
        {
            if (ProfileOpenInEditor == prof)
            {
                AddAndSaveProfile(prof);

                if (editingComplete)
                {
                    StopEditing(prof);
                }
            }
        }

        public void RevertWorkingCopy(CharacterProfile prof)
        {
            var original = GetProfileByUniqueID(prof.UniqueID);

            if (original != null
                && Profiles.Contains(prof)
                && ProfileOpenInEditor == prof)
            {
                foreach (var kvp in prof.Bones)
                {
                    if (original.Bones.TryGetValue(kvp.Key, out var bt) && bt != null)
                    {
                        prof.Bones[kvp.Key].UpdateToMatch(bt);
                    }
                    else
                    {
                        prof.Bones.Remove(kvp.Key);
                    }
                }
            }
        }

        public void StopEditing(CharacterProfile prof)
        {
            ProfileOpenInEditor = null;
        }

        public void AddTemporaryProfile(string characterName, CharacterProfile prof)
        {
            TempLocalProfiles[characterName] = prof;
        }

        public void RemoveTemporaryProfile(string characterName)
        {
            TempLocalProfiles.Remove(characterName);
        }

        public static void PruneIdempotentTransforms(CharacterProfile prof)
        {
            foreach (var kvp in prof.Bones)
            {
                if (!kvp.Value.IsEdited())
                {
                    prof.Bones.Remove(kvp.Key);
                }
            }
        }

        public void ProcessConvertedProfiles()
        {
            foreach (var prof in ConvertedProfiles)
            {
                if (ConvertedProfiles.Remove(prof))
                {
                    AddAndSaveProfile(prof);
                }
            }
        }

        /// <summary>
        ///     Return a list of managed profiles the user has indicated should be rendered.
        /// </summary>
        public CharacterProfile[] GetEnabledProfiles()
        {
            //if a profile is being edited it's defacto considered disabled
            var enabledProfiles = Profiles.Where(x => x.Enabled && x.UniqueID != (ProfileOpenInEditor?.UniqueID ?? 0));

            //add any temp profiles from mare
            enabledProfiles = enabledProfiles.Concat(TempLocalProfiles.Values);

            //add any being-edited profiles that are enabled, though
            if (ProfileOpenInEditor?.Enabled ?? false)
            {
                enabledProfiles = enabledProfiles.Append(ProfileOpenInEditor);
            }

            return enabledProfiles.ToArray();
        }

        public CharacterProfile? GetProfileByCharacterName(string name)
        {
            return Profiles.FirstOrDefault(x => x.CharName == name);
        }

        public CharacterProfile? GetProfileByUniqueID(int id)
        {
            return Profiles.FirstOrDefault(x => x.UniqueID == id);
        }

        //public void UpdateDefaults(ObjectKind kind, CharacterProfile prof)
        //{
        //	this.defaultProfiles[kind] = prof;
        //}
    }
}