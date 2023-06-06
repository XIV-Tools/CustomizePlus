// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CustomizePlus.Data.Profile;
using CustomizePlus.Extensions;
using CustomizePlus.Helpers;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Havok;

namespace CustomizePlus.Data.Armature
{
    /// <summary>
    ///     Represents an interface between the bone edits made by the user and the actual
    ///     bone information used ingame.
    /// </summary>
    public unsafe class Armature
    {
        public readonly Dictionary<string, ModelBone> Bones;
        public CharacterBase* CharacterBaseRef;

        public CharacterProfile Profile;

        public bool IsVisible { get; set; }


        private static int _nextGlobalId;
        private readonly int _localId;

        private bool _snapToReference;

        private Skeleton* Skeleton => CharacterBaseRef->Skeleton;


        public Armature(CharacterProfile prof)
        {
            _localId = _nextGlobalId++;

            Profile = prof;
            IsVisible = false;
            CharacterBaseRef = null;
            Bones = new Dictionary<string, ModelBone>();

            Profile.Armature = this;

            TryLinkSkeleton();

            PluginLog.LogDebug($"Instantiated {this}, attached to {Profile}");
        }

        public override string ToString()
        {
            return CharacterBaseRef == null
                ? $"Armature ({_localId}) on {Profile.CharacterName} with no skeleton reference"
                : $"Armature ({_localId}) on {Profile.CharacterName} with {Bones.Count} bone/s";
        }

        public IEnumerable<string> GetExtantBoneNames()
        {
            return Bones.Keys;
        }

        public bool GetReferenceSnap()
        {
            if (Profile != Plugin.ProfileManager.ProfileOpenInEditor)
                _snapToReference = false;

            return _snapToReference;
        }

        public void SetReferenceSnap(bool value)
        {
            if (value && Profile == Plugin.ProfileManager.ProfileOpenInEditor)
                _snapToReference = false;

            _snapToReference = value;
        }

        public bool TryLinkSkeleton()
        {
            if (GameDataHelper.TryLookupCharacterBase(Profile.CharacterName, out var cBase)
                && cBase != null)
            {
                if (cBase != CharacterBaseRef || !Bones.Any())
                {
                    CharacterBaseRef = cBase;
                    RebuildSkeleton();
                }

                return true;
            }

            CharacterBaseRef = null;
            return false;
        }

        public void RebuildSkeleton( /*CharacterBase* cbase*/)
        {
            if (CharacterBaseRef == null) 
                return;

            Bones.Clear();

            try
            {
                //build the skeleton
                for (var pSkeleIndex = 0; pSkeleIndex < Skeleton->PartialSkeletonCount; ++pSkeleIndex)
                {
                    for (var poseIndex = 0; poseIndex < 4; ++poseIndex)
                    {
                        var currentPose = Skeleton->PartialSkeletons[pSkeleIndex].GetHavokPose(poseIndex);

                        if (currentPose == null)
                            continue;
                        
                        for (var boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Length; ++boneIndex)
                        {
                            if (currentPose->Skeleton->Bones[boneIndex].Name.String is string boneName &&
                                boneName != null)
                            {
                                if (Bones.TryGetValue(boneName, out var dummy) && dummy != null)
                                {
                                    Bones[boneName].TripleIndices
                                        .Add(new Tuple<int, int, int>(pSkeleIndex, poseIndex, boneIndex));
                                }
                                else
                                {
                                    string? parentBone = null;

                                    if (currentPose->Skeleton->ParentIndices.Length > boneIndex
                                        && currentPose->Skeleton->ParentIndices[boneIndex] is short pIndex
                                        && pIndex >= 0
                                        && currentPose->Skeleton->Bones.Length > pIndex
                                        && currentPose->Skeleton->Bones[pIndex].Name.String is string outParentBone
                                        && outParentBone != null)
                                    {
                                        parentBone = outParentBone;
                                    }

                                    Bones[boneName] = new ModelBone(this, boneName, parentBone ?? string.Empty,
                                        pSkeleIndex, poseIndex, boneIndex)
                                    {
                                        PluginTransform =
                                            Profile.Bones.TryGetValue(boneName, out var bt) && bt.IsEdited()
                                                ? bt
                                                : new BoneTransform()
                                    };
                                }
                            }
                        }
                    }
                }

                BoneData.LogNewBones(Bones.Keys.Where(BoneData.IsNewBone).ToArray());

                DiscoverParentage();
                DiscoverSiblings();

                PluginLog.LogDebug($"Rebuilt {this}:");
                foreach (var kvp in Bones)
                {
                    PluginLog.LogDebug($"\t- {kvp.Value}");
                }
            }
            catch (Exception ex)
            {
                PluginLog.LogError($"Error rebuilding armature skeleton: {ex}");
            }
        }

        public void UpdateBoneTransform(string boneName, BoneTransform bt, bool mirror = false, bool propagate = false)
        {
            if (Bones.TryGetValue(boneName, out var mb) && mb != null)
                mb.UpdateModel(bt, mirror, propagate);
            else
                PluginLog.LogError($"{boneName} doesn't exist in armature {this}");

            Bones[boneName].UpdateModel(bt, mirror, propagate);
        }

        public void ApplyTransformation()
        {
            foreach (var kvp in Bones.Where(x => x.Value.PluginTransform.IsEdited()))
            {
                kvp.Value.ApplyModelTransform();
            }
        }

        public void OverrideWithReferencePose()
        {
            for (var pSkeleIndex = 0; pSkeleIndex < Skeleton->PartialSkeletonCount; ++pSkeleIndex)
            {
                for (var poseIndex = 0; poseIndex < 4; ++poseIndex)
                {
                    var snapPose = Skeleton->PartialSkeletons[pSkeleIndex].GetHavokPose(poseIndex);

                    if (snapPose != null)
                    {
                        snapPose->SetToReferencePose();
                    }
                }
            }
        }

        public void OverrideRootParenting()
        {
            var pSkeleNot = Skeleton->PartialSkeletons[0];

            for (var pSkeleIndex = 1; pSkeleIndex < Skeleton->PartialSkeletonCount; ++pSkeleIndex)
            {
                var partialSkele = Skeleton->PartialSkeletons[pSkeleIndex];

                for (var poseIndex = 0; poseIndex < 4; ++poseIndex)
                {
                    var currentPose = partialSkele.GetHavokPose(poseIndex);

                    if (currentPose != null && partialSkele.ConnectedBoneIndex >= 0)
                    {
                        int boneIdx = partialSkele.ConnectedBoneIndex;
                        int parentBoneIdx = partialSkele.ConnectedParentBoneIndex;

                        var transA = currentPose->AccessBoneModelSpace(boneIdx, 0);
                        var transB = pSkeleNot.GetHavokPose(0)->AccessBoneModelSpace(parentBoneIdx, 0);

                        //currentPose->AccessBoneModelSpace(parentBoneIdx, hkaPose.PropagateOrNot.DontPropagate);

                        for (var i = 0; i < currentPose->Skeleton->Bones.Length; ++i)
                        {
                            currentPose->ModelPose[i] = ApplyPropagatedTransform(currentPose->ModelPose[i], transB,
                                transA->Translation, transB->Rotation);
                            currentPose->ModelPose[i] = ApplyPropagatedTransform(currentPose->ModelPose[i], transB,
                                transB->Translation, transA->Rotation);
                        }
                    }
                }
            }
        }

        private hkQsTransformf ApplyPropagatedTransform(hkQsTransformf init, hkQsTransformf* propTrans,
            hkVector4f initialPos, hkQuaternionf initialRot)
        {
            var sourcePosition = propTrans->Translation.GetAsNumericsVector().RemoveWTerm();
            var deltaRot = propTrans->Rotation.ToQuaternion() / initialRot.ToQuaternion();
            var deltaPos = sourcePosition - initialPos.GetAsNumericsVector().RemoveWTerm();

            hkQsTransformf output = new()
            {
                Translation = Vector3
                    .Transform(init.Translation.GetAsNumericsVector().RemoveWTerm() - sourcePosition, deltaRot)
                    .ToHavokTranslation(),
                Rotation = deltaRot.ToHavokRotation(),
                Scale = init.Scale
            };

            return output;
        }


        private void DiscoverParentage()
        {
            foreach (var potentialParent in Bones)
            {
                foreach (var potentialChild in Bones)
                {
                    if (potentialChild.Value.ParentBoneName == potentialParent.Value.BoneName)
                    {
                        potentialParent.Value.Children.Add(potentialChild.Value);
                        potentialChild.Value.Parent = potentialParent.Value;
                    }
                }
            }
        }

        private void DiscoverSiblings()
        {
            foreach (var potentialLefty in Bones.Where(x => x.Key[^1] == 'l'))
            {
                foreach (var potentialRighty in Bones.Where(x => x.Key[^1] == 'r'))
                {
                    if (potentialLefty.Key[..^1] == potentialRighty.Key[..^1])
                    {
                        potentialLefty.Value.Sibling = potentialRighty.Value;
                        potentialRighty.Value.Sibling = potentialLefty.Value;
                    }
                }
            }
        }
    }
}