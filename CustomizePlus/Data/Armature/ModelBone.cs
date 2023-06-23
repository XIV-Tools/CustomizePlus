// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
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
        public ModelBone? ParentBone => (_parentPartialIndex >= 0 && _parentBoneIndex >= 0)
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

        private void UpdateTransformation(BoneTransform newTransform)
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
        public void UpdateModel(BoneTransform newTransform, bool mirror = false, bool propagate = false)
        {
            if (mirror && TwinBone is ModelBone mb && mb != null)
            {
                BoneTransform mirroredTransform = BoneData.IsIVCSBone(BoneName)
                    ? newTransform.GetSpecialReflection()
                    : newTransform.GetStandardReflection();

                mb.UpdateModel(mirroredTransform, false, propagate);
            }

            if (propagate && this is not ModelRootBone)
            {
                BoneTransform delta = new BoneTransform()
                {
                    Translation = newTransform.Translation - CustomizedTransform.Translation,
                    Rotation = newTransform.Rotation - CustomizedTransform.Rotation,
                    Scaling = newTransform.Scaling - CustomizedTransform.Scaling
                };

                PropagateModelUpdate(delta);
            }

            UpdateTransformation(newTransform);

            IEnumerable<ModelBone> clones = MasterArmature.GetAllBones()
                .Where(x => x.BoneName == BoneName && !ReferenceEquals(x, this));

            foreach (ModelBone clone in clones)
            {
                clone.UpdateModel(newTransform, mirror, propagate);
            }
        }

        private void PropagateModelUpdate(BoneTransform deltaTransform)
        {
            foreach (ModelBone mb in ChildBones)
            {
                BoneTransform modTransform = new(CustomizedTransform);
                modTransform.Translation += deltaTransform.Translation;
                modTransform.Rotation += deltaTransform.Rotation;
                modTransform.Scaling += deltaTransform.Scaling;

                mb.UpdateTransformation(modTransform);
                mb.PropagateModelUpdate(deltaTransform);
            }
        }

        /// <summary>
        /// Given a character base to which this model bone's master armature (presumably) applies,
        /// return the game's current transform value for the bone corresponding to this model bone (in model space).
        /// </summary>
        public virtual hkQsTransformf GetGameTransform(CharacterBase* cBase)
        {

            FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton* skelly = cBase->Skeleton;
            FFXIVClientStructs.FFXIV.Client.Graphics.Render.PartialSkeleton pSkelly = skelly->PartialSkeletons[PartialSkeletonIndex];
            hkaPose* targetPose = pSkelly.GetHavokPose(Constants.TruePoseIndex);
            //hkaPose* targetPose = cBase->Skeleton->PartialSkeletons[PartialSkeletonIndex].GetHavokPose(Constants.TruePoseIndex);

            if (targetPose == null) return Constants.NullTransform;

            return targetPose->GetSyncedPoseModelSpace()->Data[BoneIndex];
        }

        /// <summary>
        /// Given a character base to which this model bone's master armature (presumably) applies,
        /// change to the given transform value the value for the bone corresponding to this model bone.
        /// </summary>
        protected virtual void SetGameTransform(CharacterBase* cBase, hkQsTransformf transform)
        {
            FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton* skelly = cBase->Skeleton;
            FFXIVClientStructs.FFXIV.Client.Graphics.Render.PartialSkeleton pSkelly = skelly->PartialSkeletons[PartialSkeletonIndex];
            hkaPose* targetPose = pSkelly.GetHavokPose(Constants.TruePoseIndex);
            //hkaPose* targetPose = cBase->Skeleton->PartialSkeletons[PartialSkeletonIndex].GetHavokPose(Constants.TruePoseIndex);

            if (targetPose == null) return;

            targetPose->AccessSyncedPoseModelSpace()->Data[BoneIndex] = transform;
            targetPose->BoneFlags[BoneIndex] = 1;
        }

        /// <summary>
        /// Apply this model bone's associated transformation to its in-game sibling within
        /// the skeleton of the given character base.
        /// </summary>
        public virtual void ApplyModelTransform(CharacterBase* cBase)
        {
            if (cBase != null
                && CustomizedTransform.IsEdited()
                && GetGameTransform(cBase) is hkQsTransformf gameTransform
                && !gameTransform.Equals(Constants.NullTransform))
            {
                if (CustomizedTransform.ModifyExistingTransform(gameTransform) is hkQsTransformf modTransform
                    && !modTransform.Equals(Constants.NullTransform))
                {
                    SetGameTransform(cBase, modTransform);
                }
            }
        }

        public void ApplyModelScale(CharacterBase* cBase) => ApplyTransFunc(cBase, CustomizedTransform.ModifyExistingScale);
        public void ApplyModelRotation(CharacterBase* cBase) => ApplyTransFunc(cBase, CustomizedTransform.ModifyExistingRotation);
        public void ApplyModelTranslationAtAngle(CharacterBase* cBase) => ApplyTransFunc(cBase, CustomizedTransform.ModifyExistingTranslationWithRotation);
        public void ApplyModelTranslationAsIs(CharacterBase* cBase) => ApplyTransFunc(cBase, CustomizedTransform.ModifyExistingTranslation);

        private void ApplyTransFunc(CharacterBase* cBase, Func<hkQsTransformf, hkQsTransformf> modTrans)
        {
            if (cBase != null
                && CustomizedTransform.IsEdited()
                && GetGameTransform(cBase) is hkQsTransformf gameTransform
                && !gameTransform.Equals(Constants.NullTransform))
            {
                hkQsTransformf modTransform = modTrans(gameTransform);

                if (!modTransform.Equals(gameTransform) && !modTransform.Equals(Constants.NullTransform))
                {
                    SetGameTransform(cBase, modTransform);
                }
            }
        }
    }
}