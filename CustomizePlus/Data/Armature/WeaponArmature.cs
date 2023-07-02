using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CustomizePlus.Data.Profile;
using CustomizePlus.Extensions;
using CustomizePlus.Helpers;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace CustomizePlus.Data.Armature
{
    public unsafe class WeaponArmature : Armature
    {
        public CharacterArmature ParentArmature;
        private bool _mainHand;
        private bool _offHand => !_mainHand;

        private WeaponArmature(CharacterArmature chArm, bool mainHand) : base()
        {
            ParentArmature = chArm;
            _mainHand = mainHand;
        }

        public static WeaponArmature CreateMainHand(CharacterArmature arm, CharacterBase* cBase)
        {
            WeaponArmature output = new WeaponArmature(arm, true);
            output.RebuildSkeleton(cBase);
            return output;
        }
        public static WeaponArmature CreateOffHand(CharacterArmature arm, CharacterBase* cBase)
        {
            WeaponArmature output = new WeaponArmature(arm, false);
            output.RebuildSkeleton(cBase);
            return output;
        }

        public override void UpdateOrDeleteRecord(string recordKey, BoneTransform? trans)
        {
            UpdateOrDeleteRecord(recordKey, trans, _mainHand
                ? ParentArmature.Profile.MHBones
                : ParentArmature.Profile.OHBones);
        }

        private static void UpdateOrDeleteRecord(string key, BoneTransform? trans, Dictionary<string, BoneTransform> records)
        {
            if (trans == null)
                records.Remove(key);
            else
                records[key] = trans;
        }

        public override unsafe void RebuildSkeleton(CharacterBase* cBase)
        {
            if (cBase == null)
                return;

            List<List<ModelBone>> newPartials = _mainHand
                ? ParseBonesFromObject(this, cBase->GetChild1(), ParentArmature.Profile.MHBones)
                : ParseBonesFromObject(this, cBase->GetChild2(), ParentArmature.Profile.OHBones);

            _partialSkeletons = newPartials.Select(x => x.ToArray()).ToArray();

            foreach(ModelBone mb in newPartials.SelectMany(x => x))
            {
                mb.FamilyName = _mainHand ? BoneData.BoneFamily.MainHand : BoneData.BoneFamily.OffHand;
            }
        }

        public override void ApplyTransformation(CharacterBase* cBase, bool applyScaling)
        {
            base.ApplyTransformation(_mainHand ? cBase->GetChild1() : cBase->GetChild2(), applyScaling);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string wep = _mainHand ? "MH" : "OH";
            string bInf = Built ? $"{BoneCount()} bone/s" : "no skeleton reference";

            return $"Armature (#{_localId:00000}) on {ParentArmature.Profile.CharacterName}'s {wep} weapon with {bInf}";
        }

        public override IEnumerable<TransformInfo> GetBoneTransformValues(BoneAttribute attribute, PosingSpace space)
        {
            foreach(ModelBone mb in GetLocalAndDownstreamBones().Where(x => x is not PartialRootBone))
            {
                TransformInfo trInfo = new(this, mb, attribute, space);
                trInfo.BoneDisplayName = $"{(_mainHand ? "Main Hand" : "Off Hand")} {trInfo.BoneDisplayName}";
                trInfo.BoneFamilyName = _mainHand ? BoneData.BoneFamily.MainHand : BoneData.BoneFamily.OffHand;

                yield return trInfo;
            }
        }
    }
}
