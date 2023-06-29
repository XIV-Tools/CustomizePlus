using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CustomizePlus.Data.Profile;
using CustomizePlus.Helpers;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace CustomizePlus.Data.Armature
{
    public unsafe class CharacterArmature : Armature
    {
        /// <summary>
        /// Gets the Customize+ profile for which this mockup applies transformations.
        /// </summary>
        public CharacterProfile Profile { get; init; }

        public CharacterArmature(CharacterProfile prof) : base()
        {
            Profile = prof;
            //cross-link the two, though I'm not positive the profile ever needs to refer back
            Profile.Armature = this;

            TryLinkSkeleton();

            PluginLog.LogDebug($"Instantiated {this}, attached to {Profile}");
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Built
                ? $"Armature (#{_localId:00000}) on {Profile.CharacterName} with {BoneCount()} bone/s"
                : $"Armature (#{_localId:00000}) on {Profile.CharacterName} with no skeleton reference";
        }

        protected override void SetFrozenStatus(bool value)
        {
            base.SetFrozenStatus(value && Profile == Plugin.ProfileManager.ProfileOpenInEditor);
        }

        /// <summary>
        /// Returns whether or not a link can be established between the armature and an in-game object.
        /// If unbuilt, the armature will use this opportunity to rebuild itself.
        /// </summary>
        public unsafe CharacterBase* TryLinkSkeleton(bool forceRebuild = false)
        {
            try
            {
                if (GameDataHelper.TryLookupCharacterBase(Profile.CharacterName, out CharacterBase* cBase)
                    && cBase != null)
                {
                    if (!Built || forceRebuild)
                    {
                        RebuildSkeleton(cBase);
                    }
                    return cBase;
                }
            }
            catch (Exception ex)
            {
                PluginLog.LogError($"Error occured while attempting to link skeleton '{this}': {ex}");
            }

            return null;
        }

        public override void UpdateOrDeleteRecord(string recordKey, BoneTransform? trans)
        {
            if (trans == null)
            {
                Profile.Bones.Remove(recordKey);
            }
            else
            {
                Profile.Bones[recordKey] = trans;
            }
        }
        public override unsafe void RebuildSkeleton(CharacterBase* cBase)
        {
            if (cBase == null)
                return;

            List<List<ModelBone>> newPartials = ParseBonesFromObject(this, cBase, Profile.Bones);

            _partialSkeletons = newPartials.Select(x => x.ToArray()).ToArray();

            List<Armature> weapons = new();
            if (WeaponArmature.CreateMainHand(this, cBase) is WeaponArmature main && main != null)
                weapons.Add(main);
            if (WeaponArmature.CreateOffHand(this, cBase) is WeaponArmature off && off != null)
                weapons.Add(off);

            if (weapons.Any()) _subArmatures = weapons.ToArray();

            PluginLog.LogDebug($"Rebuilt {this}");
        }
    }
}
