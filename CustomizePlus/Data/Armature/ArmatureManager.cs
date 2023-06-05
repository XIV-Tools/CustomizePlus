// © Customize+.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;

using CustomizePlus.Helpers;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;

namespace CustomizePlus.Data.Armature
{
    public sealed class ArmatureManager
    {
        private readonly HashSet<Armature> armatures = new();

        public void RenderCharacterProfiles(params Profile.CharacterProfile[] profiles)
        {
            RefreshActiveArmatures(profiles);
            RefreshArmatureVisibility();
            ApplyArmatureTransforms();
        }

        public unsafe void RenderArmatureByObject(GameObject obj)
        {
            if (armatures.FirstOrDefault(x => x.CharacterBaseRef == obj.ToCharacterBase()) is Armature arm && arm != null)
            {
                if (arm.Visible)
                {
                    arm.ApplyTransformation();
                }
            }
        }

        private void RefreshActiveArmatures(params Profile.CharacterProfile[] profiles)
        {
            foreach (var prof in profiles)
            {
                if (!armatures.Any(x => x.Profile == prof))
                {
                    var newArm = new Armature(prof);
                    armatures.Add(newArm);
                    PluginLog.LogDebug($"Added '{newArm}' to cache");
                }
            }

            foreach (var arm in armatures.Except(profiles.Select(x => x.Armature)))
            {
                if (arm != null)
                {
                    armatures.Remove(arm);
                    PluginLog.LogDebug($"Removed '{arm}' from cache");
                }
            }
        }

        private void RefreshArmatureVisibility()
        {
            foreach (var arm in armatures)
            {
                arm.Visible = arm.Profile.Enabled && arm.TryLinkSkeleton();
            }
        }

        private void ApplyArmatureTransforms()
        {
            foreach (var arm in armatures.Where(x => x.Visible))
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