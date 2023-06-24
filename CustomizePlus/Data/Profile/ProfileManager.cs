﻿// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using CustomizePlus.Helpers;
using Dalamud.Game.ClientState.Objects.Enums;
using Newtonsoft.Json;

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

        private readonly Dictionary<ObjectKind, CharacterProfile> _defaultProfiles = new();

        public readonly HashSet<CharacterProfile> Profiles = new(new ProfileEquality());

        public readonly Dictionary<string, CharacterProfile> TempLocalProfiles = new();

        private bool _initializationComplete = false;

        //public readonly HashSet<CharacterProfile> ProfilesOpenInEditor = new(new ProfileEquality());
        public CharacterProfile? ProfileOpenInEditor { get; private set; }

        public void CompleteInitialization() => _initializationComplete = true;

        public void LoadProfiles()
        {
            Dalamud.Logging.PluginLog.LogInformation("Loading profiles from directory...");

            foreach (var path in ProfileReaderWriter.GetProfilePaths())
            {
                if (ProfileReaderWriter.TryLoadProfile(path, out var prof)
                    && prof != null
                    && !Profiles.Contains(prof))
                {
                    PruneIdempotentTransforms(prof);

                    Profiles.Add(prof);
                    Dalamud.Logging.PluginLog.LogDebug($"Loading {prof}");

                    if (prof.Enabled)
                    {
                        AssertEnabledProfile(prof);
                    }
                }
            }

            Dalamud.Logging.PluginLog.LogInformation("Directory load complete");
        }

        public void CheckForNewProfiles()
        {
            Dalamud.Logging.PluginLog.LogInformation($"Seeking new profiles in {Configuration.ConfigurationManager.ConfigDirectory}...");
            bool foundAny = false;

            foreach (var path in ProfileReaderWriter.GetProfilePaths())
            {
                if (ProfileReaderWriter.TryLoadProfile(path, out var prof)
                    && prof != null
                    && !Profiles.Contains(prof))
                {
                    Dalamud.Logging.PluginLog.LogInformation($"Found new profile {prof}. Loading...");
                    foundAny = true;

                    PruneIdempotentTransforms(prof);

                    Profiles.Add(prof);
                }
            }

            if (!foundAny)
            {
                Dalamud.Logging.PluginLog.LogInformation($"No new profiles detected");
            }
        }

        /// <summary>
        ///     Adds the given profile to the list of those managed, and immediately
        ///     saves it to disk. If the profile already exists (and it is not forced to be new)
        ///     the given profile will overwrite the old one.
        /// </summary>
        public unsafe void AddAndSaveProfile(CharacterProfile prof, bool forceNew = false)
        {
            PruneIdempotentTransforms(prof);
            if (prof.Armature != null
                && prof.Armature.TryLinkSkeleton() == null)
            {
                prof.Armature = null;
            }

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

                //only let this new profile be enabled if
                // (1) it wants to be in the first place
                // (2) the character it's for doesn't already have an enabled profile
                prof.Enabled = prof.Enabled && !GetEnabledProfiles().Any(x => x.CharacterName == prof.CharacterName);

                Profiles.Add(prof);
                ProfileReaderWriter.SaveProfile(prof);
            }
        }

        public void DuplicateProfile(CharacterProfile prof, string? newCharName, string? newProfName)
        {
            if (Profiles.Contains(prof))
            {
                CharacterProfile dupe = new CharacterProfile(prof);

                if (newCharName != null)
                {
                    dupe.CharacterName = newCharName;
                }

                if (newProfName != null)
                {
                    dupe.CharacterName = newProfName;
                }

                AddAndSaveProfile(dupe);
            }
        }

        public void DeleteProfile(CharacterProfile prof)
        {
            if (Profiles.Remove(prof))
            {
                Dalamud.Logging.PluginLog.LogInformation($"{prof} deleted");
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
            Dalamud.Logging.PluginLog.LogInformation($"Asserting that {activeProfile} is enabled...");
            activeProfile.Enabled = true;

            foreach (var profile in Profiles
                         .Where(x => x.CharacterName == activeProfile.CharacterName && x != activeProfile && x.Enabled))
            {
                Dalamud.Logging.PluginLog.LogInformation($"\t-> {profile} disabled");
                profile.Enabled = false;
            }
        }

        /// <summary>
        ///     Mark the given profile (if any) as currently being edited, and return
        ///     a copy that can be safely mangled without affecting the old one.
        /// </summary>
        public CharacterProfile? GetWorkingCopy(CharacterProfile prof)
        {
            if (prof != null && ProfileOpenInEditor == null)
            {
                Dalamud.Logging.PluginLog.LogInformation($"Creating new copy of {prof} for editing...");

                ProfileOpenInEditor = new CharacterProfile(prof);

                PruneIdempotentTransforms(ProfileOpenInEditor);
                return ProfileOpenInEditor;
            }

            return null;
        }

        public void SaveWorkingCopy(bool editingComplete = false)
        {
            if (ProfileOpenInEditor != null)
            {
                Dalamud.Logging.PluginLog.LogInformation($"Saving changes to {ProfileOpenInEditor} to manager...");

                AddAndSaveProfile(ProfileOpenInEditor);

                //Send OnProfileUpdate if this is profile of the current player and it's enabled
                if (ProfileOpenInEditor.CharacterName == GameDataHelper.GetPlayerName() && ProfileOpenInEditor.Enabled)
                    Plugin.IPCManager.OnLocalPlayerProfileUpdate();

                if (editingComplete)
                {
                    StopEditing();
                }
            }
        }

        public void RevertWorkingCopy()
        {
            if (ProfileOpenInEditor != null)
            {
                var original = GetProfileByUniqueId(ProfileOpenInEditor.UniqueId);

                if (original != null)
                {
                    Dalamud.Logging.PluginLog.LogInformation($"Reverting {ProfileOpenInEditor} to its original state...");

                    foreach (var kvp in ProfileOpenInEditor.Bones)
                    {
                        if (original.Bones.TryGetValue(kvp.Key, out var bt) && bt != null)
                        {
                            ProfileOpenInEditor.Bones[kvp.Key].UpdateToMatch(bt);
                        }
                        else
                        {
                            ProfileOpenInEditor.Bones.Remove(kvp.Key);
                        }
                    }
                }
            }
        }

        public void StopEditing()
        {
            Dalamud.Logging.PluginLog.LogInformation($"{ProfileOpenInEditor} deleted");
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
            Dalamud.Logging.PluginLog.LogInformation("Loading and converting legacy profiles...");

            foreach (var prof in ConvertedProfiles)
            {
                if (ConvertedProfiles.Remove(prof))
                {
                    AddAndSaveProfile(prof);
                    Dalamud.Logging.PluginLog.LogDebug($"Loaded/Converted {prof}");

                    if (prof.Enabled)
                    {
                        AssertEnabledProfile(prof);
                    }
                }
            }

            Dalamud.Logging.PluginLog.LogInformation("Legacy load complete");
        }

        /// <summary>
        ///     Return a list of managed profiles the user has indicated should be rendered.
        /// </summary>
        public CharacterProfile[] GetEnabledProfiles()
        {
            if (!_initializationComplete) return Array.Empty<CharacterProfile>();

            var enabledProfiles = Profiles.Where(x => x.Enabled);

            //add any temp profiles from mare
            enabledProfiles = enabledProfiles.Concat(TempLocalProfiles.Values);

            //if the being-edited profile is individually enabled, then pass it along
            //potentially replacing a profile already in the list
            if (ProfileOpenInEditor?.Enabled ?? false)
            {
                enabledProfiles = enabledProfiles
                    .Where(x => x.UniqueId != ProfileOpenInEditor.UniqueId)
                    .Append(ProfileOpenInEditor);
            }

            return enabledProfiles.ToArray();
        }

        public CharacterProfile? GetProfileByCharacterName(string name, bool enabledOnly = false)
        {
            var query = Profiles.Where(x => x.CharacterName == name);
            if (enabledOnly)
                query = query.Where(x => x.Enabled);

            return query.FirstOrDefault();
        }

        public CharacterProfile? GetProfileByUniqueId(int id)
        {
            return Profiles.FirstOrDefault(x => x.UniqueId == id);
        }

        /// <summary>
        /// Returns all the profiles which might apply to the given object, prioritizing any open in the editor.
        /// </summary>
        public IEnumerable<CharacterProfile> GetProfilesByGameObject(Dalamud.Game.ClientState.Objects.Types.GameObject obj)
        {
            string name = Helpers.GameDataHelper.GetObjectName(obj);

            List<CharacterProfile> output = new();

            if (ProfileOpenInEditor != null)
            {
                if (ProfileOpenInEditor.CharacterName == name)
                {
                    output.Add(ProfileOpenInEditor);
                }
                else if (ProfileOpenInEditor.CharacterName == Constants.DefaultProfileCharacterName && DefaultOnly(obj))
                {
                    output.Add(ProfileOpenInEditor);
                }
            }

            if (Profiles.Any(x => x.CharacterName == name))
            {
                return output.Concat(Profiles.Where(x => x.CharacterName == name));
            }
            else
            {
                return output.Concat(Profiles.Where(x => x.CharacterName == Constants.DefaultProfileCharacterName));
            }
        }

        /// <summary>
        /// Returns true iff the profile manager contains at least one Default profile,
        /// and there are zero non-Default profiles that could apply to the given object.
        /// </summary>
        public bool DefaultOnly(Dalamud.Game.ClientState.Objects.Types.GameObject obj)
        {
            string name = Helpers.GameDataHelper.GetObjectName(obj);

            return Profiles.Any(x => x.CharacterName == Constants.DefaultProfileCharacterName)
                && !Profiles.Any(x => x.CharacterName == name);
        }
    }
}