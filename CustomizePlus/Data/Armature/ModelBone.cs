// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Extensions;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Havok;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;
using System.Reflection;

//using CustomizePlus.Memory;

namespace CustomizePlus.Data.Armature
{
    /// <summary>
    ///     Represents a single bone of an ingame character's skeleton.
    /// </summary>
    public unsafe class ModelBone
    {
        public enum PoseType
        {
            Local, Model, BindPose, World
        }

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
        public BoneTransform CustomizedTransform { get; }

        internal bool MainHandBone { get; set; } = false;
        internal bool OffHandBone { get; set; } = false;

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
        /// Indicate a bone that acts as this model bone's mirror image, or "twin".
        /// </summary>
        public void AddTwin(int twinPartialIdx, int twinBoneIdx)
        {
            _twinPartialIndex = twinPartialIdx;
            _twinBoneIndex = twinBoneIdx;
        }

        private void UpdateTransformation(BoneTransform newTransform)
        {
            //update the transform locally
            CustomizedTransform.UpdateToMatch(newTransform);

            //the model bones should(?) be the same, by reference
            //but we still need to delete them 
            if (newTransform.IsEdited())
            {
                //if (MainHandBone)
                //{
                //    MasterArmature.Profile.Bones_MH[BoneName] = new(newTransform);
                //}
                //else if (OffHandBone)
                //{
                //    MasterArmature.Profile.Bones_OH[BoneName] = new(newTransform);
                //}
                //else
                //{
                MasterArmature.Profile.Bones[BoneName] = new(newTransform);
                //}
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

        /// <summary>
        /// Get the lineage of this model bone, going back to the skeleton's root bone.
        /// </summary>
        public IEnumerable<ModelBone> GetAncestors(bool includeSelf = true) => includeSelf
            ? GetAncestors(new List<ModelBone>() { this })
            : GetAncestors(new List<ModelBone>());

        private IEnumerable<ModelBone> GetAncestors(List<ModelBone> tail)
        {
            tail.Add(this);
            if (ParentBone is ModelBone mb && mb != null)
            {
                return mb.GetAncestors(tail);
            }
            else
            {
                return tail;
            }
        }

        /// <summary>
        /// Gets all model bones with a lineage that contains this one.
        /// </summary>
        public IEnumerable<ModelBone> GetDescendants(bool includeSelf = false) => includeSelf
            ? GetDescendants(this)
            : GetDescendants(null);

        private IEnumerable<ModelBone> GetDescendants(ModelBone? first)
        {
            List<ModelBone> output = first != null
                ? new List<ModelBone>() { first }
                : new List<ModelBone>();

            output.AddRange(ChildBones);

            using (var iter = output.GetEnumerator())
            {
                while (iter.MoveNext())
                {
                    output.AddRange(iter.Current.ChildBones);
                    yield return iter.Current;
                }
            }
        }

        /// <summary>
        /// Update the transformation associated with this model bone. Optionally extend the transformation
        /// to the model bone's twin (in which case it will be appropriately mirrored) and/or children.
        /// </summary>
        public void UpdateModel(BoneTransform newTransform, bool mirror = false, bool propagate = false)
        {
            UpdateModel(newTransform, mirror, propagate, true);
        }

        private void UpdateModel(BoneTransform newTransform, bool mirror, bool propagate, bool clone)
        {
            if (mirror && TwinBone is ModelBone mb && mb != null)
            {
                BoneTransform mirroredTransform = BoneData.IsIVCSBone(BoneName)
                    ? newTransform.GetSpecialReflection()
                    : newTransform.GetStandardReflection();

                mb.UpdateModel(mirroredTransform, false, propagate);
            }

            if (propagate && this is not AliasedBone)
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

            if (clone)
            {
                UpdateClones(newTransform);
            }
        }

        private void PropagateModelUpdate(BoneTransform deltaTransform)
        {
            foreach(ModelBone mb in ChildBones)
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
        /// For each OTHER bone that shares the name of this one, direct
        /// it to update its transform to match the one provided.
        /// </summary>
        private void UpdateClones(BoneTransform newTransform)
        {
            foreach(ModelBone mb in MasterArmature.GetBones()
                .Where(x => x.BoneName == this.BoneName && x != this))
            {
                mb.UpdateTransformation(newTransform);
            }
        }

        private static hkQsTransformf Subtract(hkQsTransformf termLeft, hkQsTransformf termRight)
        {
            return new hkQsTransformf()
            {
                Translation = (termLeft.Translation.GetAsNumericsVector() - termRight.Translation.GetAsNumericsVector()).ToHavokVector(),
                Rotation = Quaternion.Divide(termLeft.Rotation.ToQuaternion(), termRight.Rotation.ToQuaternion()).ToHavokRotation(),
                Scale = (termLeft.Scale.GetAsNumericsVector() - termRight.Scale.GetAsNumericsVector()).ToHavokVector()
            };
        }

        private static hkQsTransformf Add(hkQsTransformf term1, hkQsTransformf term2)
        {
            return new hkQsTransformf()
            {
                Translation = (term1.Translation.GetAsNumericsVector() + term2.Translation.GetAsNumericsVector()).ToHavokVector(),
                Rotation = Quaternion.Multiply(term1.Rotation.ToQuaternion(), term2.Rotation.ToQuaternion()).ToHavokRotation(),
                Scale = (term1.Scale.GetAsNumericsVector() + term2.Scale.GetAsNumericsVector()).ToHavokVector()
            };
        }

        /// <summary>
        /// Given a character base to which this model bone's master armature (presumably) applies,
        /// return the game's transform value for this model's in-game sibling within the given reference frame.
        /// </summary>
        public virtual hkQsTransformf GetGameTransform(CharacterBase* cBase, PoseType refFrame)
        {

            FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton* skelly = cBase->Skeleton;
            FFXIVClientStructs.FFXIV.Client.Graphics.Render.PartialSkeleton pSkelly = skelly->PartialSkeletons[PartialSkeletonIndex];
            hkaPose* targetPose = pSkelly.GetHavokPose(Constants.TruePoseIndex);
            //hkaPose* targetPose = cBase->Skeleton->PartialSkeletons[PartialSkeletonIndex].GetHavokPose(Constants.TruePoseIndex);

            if (targetPose == null) return Constants.NullTransform;

            return refFrame switch
            {
                PoseType.Local => targetPose->LocalPose[BoneIndex],
                PoseType.Model => targetPose->ModelPose[BoneIndex],
                _ => Constants.NullTransform
                //TODO properly implement the other options
            };
        }

        protected virtual void SetGameTransform(CharacterBase* cBase, hkQsTransformf transform, PoseType refFrame)
        {
            FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton* skelly = cBase->Skeleton;
            FFXIVClientStructs.FFXIV.Client.Graphics.Render.PartialSkeleton pSkelly = skelly->PartialSkeletons[PartialSkeletonIndex];
            hkaPose* targetPose = pSkelly.GetHavokPose(Constants.TruePoseIndex);
            //hkaPose* targetPose = cBase->Skeleton->PartialSkeletons[PartialSkeletonIndex].GetHavokPose(Constants.TruePoseIndex);

            if (targetPose == null) return;

            switch (refFrame)
            {
                case PoseType.Local:
                    targetPose->LocalPose.Data[BoneIndex] = transform;
                    return;

                case PoseType.Model:
                    targetPose->ModelPose.Data[BoneIndex] = transform;
                    return;

                default:
                    return;

                    //TODO properly implement the other options
            }
        }

        /// <summary>
        /// Apply this model bone's associated transformation to its in-game sibling within
        /// the skeleton of the given character base.
        /// </summary>
        public virtual void ApplyModelTransform(CharacterBase* cBase)
        {
            if (cBase != null
                && CustomizedTransform.IsEdited()
                && GetGameTransform(cBase, PoseType.Model) is hkQsTransformf gameTransform
                && !gameTransform.Equals(Constants.NullTransform))
            {
                if (CustomizedTransform.ModifyExistingTransform(gameTransform) is hkQsTransformf modTransform
                    && !modTransform.Equals(Constants.NullTransform))
                {
                    SetGameTransform(cBase, modTransform, PoseType.Model);
                }
            }
        }

        public void ApplyModelScale(CharacterBase* cBase) => ApplyTransFunc(cBase, CustomizedTransform.ModifyExistingScale);
        public void ApplyModelRotation(CharacterBase* cBase) => ApplyTransFunc(cBase, CustomizedTransform.ModifyExistingRotation);
        public void ApplyModelFullTranslation(CharacterBase* cBase) => ApplyTransFunc(cBase, CustomizedTransform.ModifyExistingTranslationWithRotation);
        public void ApplyStraightModelTranslation(CharacterBase* cBase) => ApplyTransFunc(cBase, CustomizedTransform.ModifyExistingTranslation);

        private void ApplyTransFunc(CharacterBase* cBase, Func<hkQsTransformf, hkQsTransformf> modTrans)
        {
            if (cBase != null
                && CustomizedTransform.IsEdited()
                && GetGameTransform(cBase, PoseType.Model) is hkQsTransformf gameTransform
                && !gameTransform.Equals(Constants.NullTransform))
            {
                hkQsTransformf modTransform = modTrans(gameTransform);

                if (!modTransform.Equals(gameTransform) && !modTransform.Equals(Constants.NullTransform))
                {
                    SetGameTransform(cBase, modTransform, PoseType.Model);
                }
            }
        }
    }
}