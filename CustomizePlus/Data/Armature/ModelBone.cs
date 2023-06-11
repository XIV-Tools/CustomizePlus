// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using CustomizePlus.Extensions;
using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

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
        private BoneTransform _customizedTransform;

        public ModelBone(Armature arm, string codeName, int partialIdx, int boneIdx)
        {
            MasterArmature = arm;
            PartialSkeletonIndex = partialIdx;
            BoneIndex = boneIdx;

            BoneName = codeName;

            _customizedTransform = new();
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

            MasterArmature[parentPartialIdx, parentBoneIdx]?.AddChild(PartialSkeletonIndex, BoneIndex);
        }

        private void AddChild(int childPartialIdx, int childBoneIdx)
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

            MasterArmature[twinPartialIdx, twinBoneIdx].AddTwin(PartialSkeletonIndex, BoneIndex);
        }

        private void UpdateTransformation(BoneTransform newTransform)
        {
            //update the transform locally
            _customizedTransform.UpdateToMatch(newTransform);

            //these should be connected by reference already, I think?
            //but I suppose it doesn't hurt...?
            if (newTransform.IsEdited())
            {
                MasterArmature.Profile.Bones[BoneName] = _customizedTransform;
            }
            else
            {
                MasterArmature.Profile.Bones.Remove(BoneName);
            }
        }

        public override string ToString()
        {
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
            if (TwinBone is ModelBone mb && mb != null)
            {
                BoneTransform mirroredTransform = BoneData.IsIVCSBone(BoneName)
                    ? newTransform.GetSpecialReflection()
                    : newTransform.GetStandardReflection();

                mb.UpdateModel(mirroredTransform, false, propagate);
            }

            UpdateTransformation(newTransform);
        }

        /// <summary>
        /// Given a character base to which this model bone's master armature (presumably) applies,
        /// return the game's transform value for this model's in-game sibling within the given reference frame.
        /// </summary>
        public hkQsTransformf GetGameTransform(CharacterBase* cBase, PoseType refFrame)
        {
            hkaPose* targetPose = cBase->Skeleton->PartialSkeletons[PartialSkeletonIndex].GetHavokPose(Constants.TruePoseIndex);

            return refFrame switch
            {
                PoseType.Local => targetPose->LocalPose[BoneIndex],
                PoseType.Model => targetPose->ModelPose[BoneIndex],
                _ => Constants.NullTransform
                //TODO properly implement the other options
            };
        }

        public void SetGameTransform(CharacterBase* cBase, hkQsTransformf transform, PoseType refFrame)
        {
            hkaPose* targetPose = cBase->Skeleton->PartialSkeletons[PartialSkeletonIndex].GetHavokPose(Constants.TruePoseIndex);

            switch (refFrame)
            {
                case PoseType.Local:
                    targetPose->LocalPose[BoneIndex] = transform;
                    return;

                case PoseType.Model:
                    targetPose->ModelPose[BoneIndex] = transform;
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
        public void ApplyModelTransform(CharacterBase* cBase)
        {

            hkQsTransformf gameTransform = GetGameTransform(cBase, PoseType.Model);
            hkQsTransformf moddedTransform = _customizedTransform.ModifyExistingTransformation(gameTransform);
            SetGameTransform(cBase, moddedTransform, PoseType.Model);

            //TODO set up the reference pose stuff

            //if (MasterArmature.GetReferenceSnap())
            //{
            //    //Referencing from Ktisis, Function 'SetFromLocalPose' @ Line 39 in 'Havok.cs'

            //    //hkQsTransformf tRef = currentPose->Skeleton->ReferencePose.Data[triplex.Item3];
            //    var tRef = currentPose->LocalPose[triplex.Item3];

            //    var tParent = currentPose->ModelPose[currentPose->Skeleton->ParentIndices[triplex.Item3]];
            //    var tModel =
            //        *currentPose->AccessBoneModelSpace(triplex.Item3, hkaPose.PropagateOrNot.DontPropagate);

            //    tModel.Translation =
            //    (
            //        tParent.Translation.GetAsNumericsVector().RemoveWTerm()
            //        + Vector3.Transform(tRef.Translation.GetAsNumericsVector().RemoveWTerm(),
            //            tParent.Rotation.ToQuaternion())
            //    ).ToHavokTranslation();

            //    tModel.Rotation =
            //        (tParent.Rotation.ToQuaternion() * tRef.Rotation.ToQuaternion()).ToHavokRotation();
            //    tModel.Scale = tRef.Scale;

            //    var t = tModel;
            //    var tNew = _customizedTransform.ModifyExistingTransformation(t);
            //    currentPose->ModelPose.Data[triplex.Item3] = tNew;
            //}
            //else
            //{
            //    var t = currentPose->ModelPose.Data[triplex.Item3];
            //    var tNew = _customizedTransform.ModifyExistingTransformation(t);
            //    currentPose->ModelPose.Data[triplex.Item3] = tNew;
            //}
        }
    }
}