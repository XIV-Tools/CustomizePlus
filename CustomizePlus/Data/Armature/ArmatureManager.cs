// © Customize+.
// Licensed under the MIT license.

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
            RefreshActiveArmatures(profiles);
            RefreshArmatureVisibility();
            ApplyArmatureTransforms();
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
        }


        private void RefreshArmatureVisibility()
        {
            foreach (var arm in _armatures)
            {
                arm.IsVisible = arm.Profile.Enabled && arm.TryLinkSkeleton();
            }
        }

        private unsafe void ApplyArmatureTransforms()
        {
            foreach(GameObject obj in DalamudServices.ObjectTable)
            {
                CharacterBase* cBase = obj.ToCharacterBase();

                if (cBase == null || cBase->Skeleton == null) continue;

                Armature? arm = _armatures
                    .Where(x => x.IsVisible)
                    .FirstOrDefault(x => x.AppliesTo(obj.Name.TextValue));

                if (arm != null)
                {
                    arm.ApplyTransformation(cBase);
                }
                else if (_defaultArmature != null)
                {
                    _defaultArmature.ApplyTransformation(cBase);
                }
            }
        }
    }
}