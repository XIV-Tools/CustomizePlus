﻿// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

using CustomizePlus.Extensions;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.Havok;

//using CustomizePlus.Memory;

namespace CustomizePlus.Data.Armature
{
    /// <summary>
    /// Represents a frame of reference in which a model bone's transformations are being modified.
    /// </summary>
    public enum PosingSpace
    {
        Self, Parent, Character
    }

    /// <summary>
    ///     Represents a single bone of an ingame character's skeleton.
    /// </summary>
    public unsafe class ModelBone
    {
        public readonly Armature MasterArmature;

        public readonly int PartialSkeletonIndex;
        public readonly int BoneIndex;

        /// <summary>
        /// Gets the model bone corresponding to this model bone's parent, if it exists.
        /// (It should in all cases but the root of the skeleton)
        /// </summary>
        public ModelBone? ParentBone => (_parentPartialIndex >= 0 && _parentPartialIndex < MasterArmature.PartialSkeletonCount
                && _parentBoneIndex >= 0 && _parentBoneIndex < MasterArmature.GetBoneCountOfPartial(_parentPartialIndex))
            ? MasterArmature[_parentPartialIndex, _parentBoneIndex]
            : null;
        private int _parentPartialIndex = -1;
        private int _parentBoneIndex = -1;

        /// <summary>
        /// Gets each model bone for which this model bone corresponds to a direct parent thereof.
        /// A model bone may have zero children.
        /// </summary>
        public IEnumerable<ModelBone> ChildBones => _childPartialIndices.Zip(_childBoneIndices, (x, y) => MasterArmature[x, y]);
        private List<int> _childPartialIndices = new();
        private List<int> _childBoneIndices = new();

        /// <summary>
        /// Gets the model bone that forms a mirror image of this model bone, if one exists.
        /// </summary>
        public ModelBone? TwinBone => (_twinPartialIndex >= 0 && _twinBoneIndex >= 0)
            ? MasterArmature[_twinPartialIndex, _twinBoneIndex]
            : null;
        private int _twinPartialIndex = -1;
        private int _twinBoneIndex = -1;

        /// <summary>
        /// The name of the bone within the in-game skeleton. Referred to in some places as its "code name".
        /// </summary>
        public string BoneName;

        /// <summary>
        /// The transform that this model bone will impart upon its in-game sibling when the master armature
        /// is applied to the in-game skeleton.
        /// </summary>
        protected virtual BoneTransform CustomizedTransform { get; }

        public virtual bool HasActiveTransform { get => CustomizedTransform.IsEdited(); }

        #region Model Bone Construction

        public ModelBone(Armature arm, string codeName, int partialIdx, int boneIdx)
        {
            MasterArmature = arm;
            PartialSkeletonIndex = partialIdx;
            BoneIndex = boneIdx;

            BoneName = codeName;

            CustomizedTransform = new();
        }

        /// <summary>
        /// Indicate a bone to act as this model bone's "parent".
        /// </summary>
        public void AddParent(int parentPartialIdx, int parentBoneIdx)
        {
            if (_parentPartialIndex != -1 || _parentBoneIndex != -1)
            {
                throw new Exception($"Tried to add redundant parent to model bone -- {this}");
            }

            _parentPartialIndex = parentPartialIdx;
            _parentBoneIndex = parentBoneIdx;
        }

        /// <summary>
        /// Indicate that a bone is one of this model bone's "children".
        /// </summary>
        public void AddChild(int childPartialIdx, int childBoneIdx)
        {
            _childPartialIndices.Add(childPartialIdx);
            _childBoneIndices.Add(childBoneIdx);
        }

        /// <summary>
        /// Indicate a bone that acts as this model bone's mirror image.
        /// </summary>
        public void AddTwin(int twinPartialIdx, int twinBoneIdx)
        {
            _twinPartialIndex = twinPartialIdx;
            _twinBoneIndex = twinBoneIdx;
        }

        #endregion

        protected virtual void UpdateTransformation(BoneTransform newTransform)
        {
            //update the transform locally
            CustomizedTransform.UpdateToMatch(newTransform);

            //the model bones should(?) be the same, by reference
            //but we still may need to delete them 
            if (newTransform.IsEdited())
            {
                MasterArmature.Profile.Bones[BoneName] = new(newTransform);
            }
            else
            {
                MasterArmature.Profile.Bones.Remove(BoneName);
            }
        }

        public override string ToString()
        {
            //string numCopies = _copyIndices.Count > 0 ? $" ({_copyIndices.Count} copies)" : string.Empty;
            return $"{BoneName} ({BoneData.GetBoneDisplayName(BoneName)}) @ <{PartialSkeletonIndex}, {BoneIndex}>";
        }

        public BoneTransform GetTransformation() => new(CustomizedTransform);

        /// <summary>
        /// Update the transformation associated with this model bone. Optionally extend the transformation
        /// to the model bone's twin (in which case it will be appropriately mirrored) and/or children.
        /// </summary>
        public virtual void UpdateModel(BoneTransform newTransform, bool extendToClones = true)
        {
            UpdateTransformation(newTransform);

            if (extendToClones)
            {
                IEnumerable<ModelBone> clones = MasterArmature.GetAllBones()
                    .Where(x => x is not ModelRootBone)
                    .Where(x => x.BoneName == BoneName && !ReferenceEquals(x, this));

                foreach (ModelBone clone in clones)
                {
                    clone.UpdateModel(newTransform, false);
                }
            }
        }

        //private void PropagateModelUpdate(bool mirror,
        //    Vector3 translationDelta, Vector3 rotationDelta,
        //    Vector3 accumulatedTranslation, Vector3 accumulatedRotation)
        //{
        //    foreach (ModelBone mb in ChildBones)
        //    {
        //        Vector3 accRotate = accumulatedRotation + mb.CustomizedTransform.BoneRotation;
        //        Vector3 accTranslate = accumulatedTranslation + mb.CustomizedTransform.BoneTranslation;

        //        BoneTransform modTransform = new(mb.CustomizedTransform);

        //        //slide and rotate this bone back into the reference frame of the progenitor of the update
        //        modTransform.BoneRotation -= accRotate;
        //        modTransform.BoneTranslation -= Vector3.Transform(accTranslate, accRotate.ToQuaternion());

        //        //apply the delta value
        //        //remember, since the user can only update one attribute at a time
        //        //only one of these wll have non-zero values
        //        modTransform.BoneRotation += rotationDelta;
        //        modTransform.BoneTranslation += translationDelta;

        //        //reapply the accumulated translation in the updated direction
        //        modTransform.BoneTranslation += Vector3.Transform(accTranslate, (accRotate + rotationDelta).ToQuaternion());

        //        //reapply the accumulated rotation
        //        modTransform.BoneRotation += accRotate;


        //        mb.UpdateModel(modTransform, mirror, false, true);
        //        mb.PropagateModelUpdate(mirror, translationDelta, rotationDelta, accTranslate, accRotate);
        //    }
        //}

        /// <summary>
        /// Given a character base to which this model bone's master armature (presumably) applies,
        /// return the game's current transform value for the bone corresponding to this model bone (in model space).
        /// </summary>
        public virtual hkQsTransformf GetGameTransform(CharacterBase* cBase, bool? modelSpace = null)
        {
            FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton* skelly = cBase->Skeleton;
            FFXIVClientStructs.FFXIV.Client.Graphics.Render.PartialSkeleton pSkelly = skelly->PartialSkeletons[PartialSkeletonIndex];
            hkaPose* targetPose = pSkelly.GetHavokPose(Constants.TruePoseIndex);
            //hkaPose* targetPose = cBase->Skeleton->PartialSkeletons[PartialSkeletonIndex].GetHavokPose(Constants.TruePoseIndex);

            if (targetPose == null) return Constants.NullTransform;

            if (modelSpace == null)
            {
                return targetPose->AccessUnsyncedPoseLocalSpace()->Data[BoneIndex];
            }
            else if (modelSpace == true)
            { 
                return targetPose->AccessSyncedPoseModelSpace()->Data[BoneIndex];
            }
            else
            {
                return targetPose->AccessSyncedPoseLocalSpace()->Data[BoneIndex];
            }
        }

        /// <summary>
        /// Given a character base to which this model bone's master armature (presumably) applies,
        /// change to the given transform value the value for the bone corresponding to this model bone.
        /// </summary>
        protected virtual void SetLocalTransform(CharacterBase* cBase, hkQsTransformf transform)
        {
            FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton* skelly = cBase->Skeleton;
            FFXIVClientStructs.FFXIV.Client.Graphics.Render.PartialSkeleton pSkelly = skelly->PartialSkeletons[PartialSkeletonIndex];
            hkaPose* targetPose = pSkelly.GetHavokPose(Constants.TruePoseIndex);
            //hkaPose* targetPose = cBase->Skeleton->PartialSkeletons[PartialSkeletonIndex].GetHavokPose(Constants.TruePoseIndex);

            if (targetPose == null) return;

            targetPose->AccessSyncedPoseLocalSpace()->Data[BoneIndex] = transform;

            //targetPose->AccessSyncedPoseModelSpace()->Data[BoneIndex] = *targetPose->CalculateBoneModelSpace(BoneIndex);
        }

        protected virtual void SetModelTransform(CharacterBase* cBase, hkQsTransformf transform)
        {
            FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton* skelly = cBase->Skeleton;
            FFXIVClientStructs.FFXIV.Client.Graphics.Render.PartialSkeleton pSkelly = skelly->PartialSkeletons[PartialSkeletonIndex];
            hkaPose* targetPose = pSkelly.GetHavokPose(Constants.TruePoseIndex);

            if (targetPose == null) return;

            targetPose->AccessSyncedPoseModelSpace()->Data[BoneIndex] = transform;
        }

        public void ApplyIndividualScale(CharacterBase* cBase) => ApplyModelTransform(cBase, CustomizedTransform.ModifyScale);

        public void ApplyInheritableRotation(CharacterBase* cBase) => ApplyLocalTransform(cBase, CustomizedTransform.ModifyKinematicRotation);
        public void ApplyIndividualRotation(CharacterBase* cBase) => ApplyModelTransform(cBase, CustomizedTransform.ModifyRotation);

        public void ApplyInheritableTranslationAtAngle(CharacterBase* cBase) => ApplyLocalTransform(cBase, CustomizedTransform.ModifyKineTranslationWithRotation);
        public void ApplyIndividualTranslationAtAngle(CharacterBase* cBase) => ApplyModelTransform(cBase, CustomizedTransform.ModifyTranslationWithRotation);

        public void ApplyInheritableTranslationAsIs(CharacterBase* cBase) => ApplyLocalTransform(cBase, CustomizedTransform.ModifyKineTranslation);
        public void ApplyIndividualTranslationAsIs(CharacterBase* cBase) => ApplyModelTransform(cBase, CustomizedTransform.ModifyTranslationAsIs);

        protected virtual void ApplyLocalTransform(CharacterBase* cBase, Func<hkQsTransformf, hkQsTransformf> modTrans)
        {
            if (cBase != null
                && CustomizedTransform.IsEdited()
                && GetGameTransform(cBase, false) is hkQsTransformf gameTransform
                && !gameTransform.Equals(Constants.NullTransform))
            {
                hkQsTransformf modTransform = modTrans(gameTransform);

                if (!modTransform.Equals(gameTransform) && !modTransform.Equals(Constants.NullTransform))
                {
                    SetLocalTransform(cBase, modTransform);
                }
            }
        }
        
        protected virtual void ApplyModelTransform(CharacterBase* cBase, Func<hkQsTransformf, hkQsTransformf> modTrans)
        {
            if (cBase != null
                && CustomizedTransform.IsEdited()
                && GetGameTransform(cBase, true) is hkQsTransformf gameTransform
                && !gameTransform.Equals(Constants.NullTransform))
            {
                hkQsTransformf modTransform = modTrans(gameTransform);

                if (!modTransform.Equals(gameTransform) && !modTransform.Equals(Constants.NullTransform))
                {
                    SetModelTransform(cBase, modTransform);
                }
            }
        }
    }
}