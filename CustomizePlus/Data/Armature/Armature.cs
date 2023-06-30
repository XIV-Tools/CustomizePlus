// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;

using CustomizePlus.Data.Profile;
using CustomizePlus.Helpers;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Havok;

using CustomizePlus.Extensions;

namespace CustomizePlus.Data.Armature
{
    /// <summary>
    /// Represents a "copy" of the ingame skeleton upon which the linked character profile is meant to operate.
    /// Acts as an interface by which the in-game skeleton can be manipulated on a bone-by-bone basis.
    /// </summary>
    public abstract unsafe class Armature : IBoneContainer
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not this armature has any renderable objects on which it should act.
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// For debugging purposes, each armature is assigned a globally-unique ID number upon creation.
        /// </summary>
        private static uint _nextGlobalId;
        protected readonly uint _localId;

        /// <summary>
        /// Each skeleton is made up of several smaller "partial" skeletons.
        /// Each partial skeleton has its own list of bones, with a root bone at index zero.
        /// The root bone of a partial skeleton may also be a regular bone in a different partial skeleton.
        /// </summary>
        protected ModelBone[][] _partialSkeletons { get; set; }
        protected Armature[] _subArmatures { get; set; }

        #region Bone Accessors -------------------------------------------------------------------------------

        /// <summary>
        /// Gets the number of partial skeletons contained in this armature.
        /// </summary>
        public int PartialSkeletonCount => _partialSkeletons.Length;

        /// <summary>
        /// Returns the number of bones contained within the partial skeleton with the given index.
        /// </summary>
        public int GetBoneCountOfPartial(int partialIndex) => _partialSkeletons[partialIndex].Length;

        /// <summary>
        /// Get the bone at index 'j' within the partial skeleton at index 'i'.
        /// </summary>
        private ModelBone this[int i, int j]
        {
            get => _partialSkeletons[i][j];
        }

        protected int BoneCount() => _partialSkeletons.Sum(x => x.Length) + _subArmatures.Sum(x => x.BoneCount());

        /// <summary>
        /// Return the bone at the given indices, if it exists
        /// </summary>
        public ModelBone? GetBoneAt(int partialIndex, int boneIndex)
        {
            if (_partialSkeletons.Length > partialIndex
                && _partialSkeletons[partialIndex].Length > boneIndex)
            {
                return this[partialIndex, boneIndex];
            }

            return null;
        }

        /// <summary>
        /// Returns the root bone of the partial skeleton with the given index.
        /// </summary>
        public ModelBone GetRootBoneOfPartial(int partialIndex) => this[partialIndex, 0];

        /// <summary>
        /// Get all individual model bones making up this armature.
        /// </summary>
        public IEnumerable<ModelBone> GetLocalBones()
        {
            for (int i = 0; i < _partialSkeletons.Length; ++i)
            {
                for (int j = 0; j < _partialSkeletons[i].Length; ++j)
                {
                    yield return this[i, j];
                }
            }
        }

        public IEnumerable<ModelBone> GetLocalAndDownstreamBones()
        {
            foreach (ModelBone mb in GetLocalBones()) yield return mb;

            foreach(Armature arm in _subArmatures)
            {
                foreach(ModelBone mbd in arm.GetLocalAndDownstreamBones())
                {
                    yield return mbd;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this armature has yet built its skeleton.
        /// </summary>
        public bool Built => _partialSkeletons.Any();

        //----------------------------------------------------------------------------------------------------
        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether or not this armature should snap all of its bones to their reference "bindposes".
        /// i.e. force the character ingame to assume their "default" pose.
        /// </summary>
        public bool FrozenPose
        {
            get => GetFrozenStatus();
            set => SetFrozenStatus(value);
        }
        private bool _frozenInDefaultPose;

        public Armature()
        {
            _localId = ++_nextGlobalId;

            _partialSkeletons = Array.Empty<ModelBone[]>();
            _subArmatures = Array.Empty<Armature>();

            IsVisible = false;
        }

        /// <inheritdoc/>
        public abstract override string ToString();

        protected bool GetFrozenStatus()
        {
            return _frozenInDefaultPose;
        }

        protected virtual void SetFrozenStatus(bool value)
        {
            _frozenInDefaultPose = value;
            foreach(Armature arm in _subArmatures)
            {
                arm.SetFrozenStatus(value);
            }
        }

        public abstract void UpdateOrDeleteRecord(string recordKey, BoneTransform? trans);

        /// <summary>
        /// Rebuild the armature using the provided character base as a reference.
        /// </summary>
        public abstract void RebuildSkeleton(CharacterBase* cBase);

        protected static unsafe List<List<ModelBone>> ParseBonesFromObject(Armature arm, CharacterBase* cBase, Dictionary<string, BoneTransform>? records)
        {
            List<List<ModelBone>> newPartials = new();

            if (cBase == null)
            {
                return newPartials;
            }

            try
            {
                //build the skeleton
                for (var pSkeleIndex = 0; pSkeleIndex < cBase->Skeleton->PartialSkeletonCount; ++pSkeleIndex)
                {
                    PartialSkeleton currentPartial = cBase->Skeleton->PartialSkeletons[pSkeleIndex];
                    hkaPose* currentPose = currentPartial.GetHavokPose(Constants.TruePoseIndex);

                    newPartials.Add(new());

                    if (currentPose == null)
                        continue;

                    for (var boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Length; ++boneIndex)
                    {
                        if (currentPose->Skeleton->Bones[boneIndex].Name.String is string boneName &&
                            boneName != null)
                        {
                            ModelBone newBone;

                            if (pSkeleIndex == 0 && boneIndex == 0)
                            {
                                newBone = new ModelRootBone(arm, boneName);
                                PluginLog.LogDebug($"Main root @ <{pSkeleIndex}, {boneIndex}> ({boneName})");
                            }
                            else if (currentPartial.ConnectedBoneIndex == boneIndex)
                            {
                                ModelBone cloneOf = newPartials[0][currentPartial.ConnectedParentBoneIndex];
                                newBone = new PartialRootBone(arm, cloneOf, boneName, pSkeleIndex);
                                PluginLog.LogDebug($"Partial root @ <{pSkeleIndex}, {boneIndex}> ({boneName})");
                            }
                            else
                            {
                                newBone = new ModelBone(arm, boneName, pSkeleIndex, boneIndex);
                            }

                            //skip adding parents/children/twins if it's the root bone
                            if (pSkeleIndex > 0 || boneIndex > 0)
                            {
                                if (currentPose->Skeleton->ParentIndices[boneIndex] is short parentIndex
                                    && parentIndex >= 0)
                                {
                                    newBone.AddParent(pSkeleIndex, parentIndex);
                                    newPartials[pSkeleIndex][parentIndex].AddChild(pSkeleIndex, boneIndex);
                                }

                                foreach (ModelBone mb in newPartials.SelectMany(x => x))
                                {
                                    if (AreTwinnedNames(boneName, mb.BoneName))
                                    {
                                        newBone.AddTwin(mb.PartialSkeletonIndex, mb.BoneIndex);
                                        mb.AddTwin(pSkeleIndex, boneIndex);
                                        break;
                                    }
                                }

                                if (records != null && records.TryGetValue(boneName, out BoneTransform? bt)
                                    && bt != null)
                                {
                                    newBone.UpdateModel(bt);
                                }
                            }

                            newPartials.Last().Add(newBone);
                        }
                        else
                        {
                            PluginLog.LogError($"Failed to process bone @ <{pSkeleIndex}, {boneIndex}> while parsing bones from {cBase->ToString()}");
                        }
                    }
                }

                BoneData.LogNewBones(newPartials.SelectMany(x => x.Select(y => y.BoneName)).ToArray());

                if (newPartials.Any())
                {
                    PluginLog.LogDebug($"Rebuilt {arm}");
                    PluginLog.LogDebug($"Height: {cBase->Height()}");
                    PluginLog.LogDebug($"Attachment Info:");
                    PluginLog.LogDebug($"\t  Type: {cBase->AttachType()}");
                    PluginLog.LogDebug($"\tTarget: {(cBase->AttachTarget() == null ? "N/A" : cBase->AttachTarget()->PartialSkeletonCount)} partial/s");
                    PluginLog.LogDebug($"\tParent: {(cBase->AttachParent() == null ? "N/A" : cBase->AttachParent()->PartialSkeletonCount)} partial/s");
                    PluginLog.LogDebug($"\t Count: {cBase->AttachCount()}");
                    PluginLog.LogDebug($"\tBoneID: {cBase->AttachBoneID()}");
                    PluginLog.LogDebug($"\t Scale: {cBase->AttachBoneScale()}");
                }

            }
            catch (Exception ex)
            {
                PluginLog.LogError($"Error parsing armature skeleton from {cBase->ToString()}:\n\t{ex}");
            }

            return newPartials;
        }
        //protected virtual bool NewBonesAvailable(CharacterBase* cBase)
        //{
        //    if (cBase == null)
        //    {
        //        return false;
        //    }
        //    else if (cBase->Skeleton->PartialSkeletonCount > _partialSkeletons.Length)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        for (int i = 0; i < cBase->Skeleton->PartialSkeletonCount; ++i)
        //        {
        //            hkaPose* newPose = cBase->Skeleton->PartialSkeletons[i].GetHavokPose(Constants.TruePoseIndex);
        //            if (newPose != null
        //                && newPose->Skeleton->Bones.Length > _partialSkeletons[i].Length)
        //            {
        //                return true;
        //            }
        //        }
        //    }

        //    return false;
        //}

        /// <summary>
        /// Iterate through this armature's model bones and apply their associated transformations
        /// to all of their in-game siblings.
        /// </summary>
        public virtual unsafe void ApplyTransformation(CharacterBase* cBase, bool applyScaling)
        {
            if (cBase != null)
            {
                for (int pSkeleIndex = 0; pSkeleIndex < cBase->Skeleton->PartialSkeletonCount; ++pSkeleIndex)
                {
                    hkaPose* currentPose = cBase->Skeleton->PartialSkeletons[pSkeleIndex].GetHavokPose(Constants.TruePoseIndex);

                    if (currentPose != null)
                    {
                        if (FrozenPose)
                        {
                            currentPose->SetToReferencePose();
                            currentPose->SyncModelSpace();
                        }

                        for (int boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Length; ++boneIndex)
                        {
                            if (GetBoneAt(pSkeleIndex, boneIndex) is ModelBone mb
                                && mb != null
                                && mb is not PartialRootBone
                                && (mb.BoneName == currentPose->Skeleton->Bones[boneIndex].Name.String
                                    || mb.BoneName[3..] == currentPose->Skeleton->Bones[boneIndex].Name.String)
                                && mb.HasActiveTransform)
                            {
                                //Partial root bones aren't guaranteed to be parented the way that would
                                //logically make sense. For that reason, don't bother trying to transform them locally.

                                if (applyScaling)
                                {
                                    mb.ApplyIndividualScale(cBase);
                                }

                                if (!GameStateHelper.GameInPosingModeWithFrozenRotation())
                                {
                                    mb.ApplyRotation(cBase, false);
                                }

                                if (!GameStateHelper.GameInPosingModeWithFrozenPosition())
                                {
                                    mb.ApplyTranslationAtAngle(cBase, false);
                                }

                            }
                        }

                        currentPose->SyncModelSpace();
                        currentPose->SyncLocalSpace();

                        for (int boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Length; ++boneIndex)
                        {
                            if (GetBoneAt(pSkeleIndex, boneIndex) is ModelBone mb
                                && mb != null
                                && mb.BoneName == currentPose->Skeleton->Bones[boneIndex].Name.String
                                && mb.HasActiveTransform)
                            {
                                if (mb is PartialRootBone prb)
                                {
                                    //In the case of partial root bones, simply copy the transform in model space
                                    //wholesale from the bone that they're a copy of
                                    prb.ApplyOriginalTransform(cBase);
                                    continue;
                                }

                                if (!GameStateHelper.GameInPosingModeWithFrozenRotation())
                                {
                                    mb.ApplyRotation(cBase, true);
                                }
                                else if (GameStateHelper.GameInPosingMode())
                                {
                                    mb.ApplyTranslationAtAngle(cBase, true);
                                }

                            }
                        }
                    }
                }

                foreach(Armature subArm in _subArmatures)
                {
                    //subs are responsible for figuring out how they want to parse the character base
                    subArm.ApplyTransformation(cBase, applyScaling);
                }
            }
        }


        private static bool AreTwinnedNames(string name1, string name2)
        {
            return (name1[^1] == 'r' ^ name2[^1] == 'r')
                && (name1[^1] == 'l' ^ name2[^1] == 'l')
                && (name1[0..^1] == name2[0..^1]);
        }

        public virtual IEnumerable<TransformInfo> GetBoneTransformValues(BoneAttribute attribute, PosingSpace space)
        {
            foreach(ModelBone mb in GetLocalBones().Where(x => x is not PartialRootBone))
            {
                yield return new TransformInfo(this, mb, attribute, space);
            }

            foreach(Armature arm in _subArmatures)
            {
                foreach(TransformInfo trInfo in arm.GetBoneTransformValues(attribute, space))
                {
                    yield return trInfo;
                }
            }
        }

        public void UpdateBoneTransformValue(TransformInfo newTransform, BoneAttribute attribute, bool mirrorChanges)
        {
            foreach(ModelBone mb in GetLocalBones().Where(x => x.BoneName == newTransform.BoneCodeName))
            {
                BoneTransform oldTransform = mb.GetTransformation();
                oldTransform.UpdateAttribute(attribute, newTransform.TransformationValue);
                mb.UpdateModel(oldTransform);

                if (mirrorChanges && mb.TwinBone is ModelBone twin && twin != null)
                {
                    if (BoneData.IsIVCSBone(twin.BoneName))
                    {
                        twin.UpdateModel(oldTransform.GetSpecialReflection());
                    }
                    else
                    {
                        twin.UpdateModel(oldTransform.GetStandardReflection());
                    }
                }
            }
        }
    }
}