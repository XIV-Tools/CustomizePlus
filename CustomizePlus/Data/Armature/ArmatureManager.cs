// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using CustomizePlus.Data.Profile;
using CustomizePlus.Helpers;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace CustomizePlus.Data.Armature
{
    public sealed class ArmatureManager
    {
        private Armature? _defaultArmature = null;
        private readonly HashSet<Armature> _armatures = new();

        public void RenderCharacterProfiles(params CharacterProfile[] profiles)
        {
            try
            {
                RefreshActiveArmatures(profiles);
                RefreshArmatureVisibility();
                ApplyArmatureTransforms();
            }
            catch (Exception ex)
            {
                Dalamud.Logging.PluginLog.LogError($"Exception while rendering character profiles:\n\t{ex}");
            }
        }

        public void ConstructArmatureForProfile(CharacterProfile newProfile)
        {
            if (!_armatures.Any(x => x.Profile == newProfile))
            {
                var newArm = new Armature(newProfile);
                _armatures.Add(newArm);
                PluginLog.LogDebug($"Added '{newArm}' to cache");
            }
        }

        private void RefreshActiveArmatures(params CharacterProfile[] profiles)
        {
            foreach (var prof in profiles)
            {
                ConstructArmatureForProfile(prof);
            }

            foreach(var arm in _armatures.Except(profiles.Select(x => x.Armature)))
            {
                if (arm != null && _armatures.Remove(arm))
                {
                    PluginLog.LogDebug($"Removed '{arm}' from cache");
                }
            }
        }


        private unsafe void RefreshArmatureVisibility()
        {
            foreach (var arm in _armatures)
            {
                arm.IsVisible = arm.Profile.Enabled && arm.TryLinkSkeleton() != null;
            }
        }

        private unsafe void ApplyArmatureTransforms()
        {
            foreach(GameObject obj in DalamudServices.ObjectTable)
            {
                CharacterProfile? prof = Plugin.ProfileManager
                    .GetProfilesByGameObject(obj)
                    .FirstOrDefault(x => x.Enabled);

                if (prof != null
                    && prof.Armature != null
                    && prof.Armature.IsVisible)
                {
                    prof.Armature.ApplyTransformation(obj);
                }
            }
        }
    }
}