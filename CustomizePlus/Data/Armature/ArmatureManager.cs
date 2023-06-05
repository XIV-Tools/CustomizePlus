// © Customize+.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using CustomizePlus.Data.Profile;
using CustomizePlus.Helpers;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;

namespace CustomizePlus.Data.Armature
{
    public sealed class ArmatureManager
    {
        private readonly HashSet<Armature> _armatures = new();

        public void RenderCharacterProfiles(params CharacterProfile[] profiles)
        {
            RefreshActiveArmatures(profiles);
            RefreshArmatureVisibility();
            ApplyArmatureTransforms();
        }

        public unsafe void RenderArmatureByObject(GameObject obj)
        {
            if (_armatures.FirstOrDefault(x => x.CharacterBaseRef == obj.ToCharacterBase()) is Armature arm &&
                arm != null)
            {
                if (arm.IsVisible)
                {
                    arm.ApplyTransformation();
                }
            }
        }

        private void RefreshActiveArmatures(params CharacterProfile[] profiles)
        {
            foreach (var prof in profiles)
            {
                if (!_armatures.Any(x => x.Profile == prof))
                {
                    var newArm = new Armature(prof);
                    _armatures.Add(newArm);
                    PluginLog.LogDebug($"Added '{newArm}' to cache");
                }
            }

            foreach (var arm in _armatures.Except(profiles.Select(x => x.Armature)))
            {
                if (arm != null)
                {
                    _armatures.Remove(arm);
                    PluginLog.LogDebug($"Removed '{arm}' from cache");
                }
            }
        }

        private void RefreshArmatureVisibility()
        {
            foreach (var arm in _armatures)
            {
                arm.IsVisible = arm.Profile.Enabled && arm.TryLinkSkeleton();
            }
        }

        private void ApplyArmatureTransforms()
        {
            foreach (var arm in _armatures.Where(x => x.IsVisible))
            {
                if (arm.GetReferenceSnap())
                {
                    arm.OverrideWithReferencePose();
                }

                arm.ApplyTransformation();

                //if (arm.GetReferenceSnap())
                //{
                //	arm.OverrideRootParenting();
                //}
            }
        }
    }
}