﻿// © Customize+.
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
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.Havok;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using CustomizePlus.Services;

namespace CustomizePlus.Data.Armature
{
    /// <summary>
    /// Represents a "copy" of the ingame skeleton upon which the linked character profile is meant to operate.
    /// Acts as an interface by which the in-game skeleton can be manipulated on a bone-by-bone basis.
    /// </summary>
    public unsafe class Armature
    {
        /// <summary>
        /// Gets the Customize+ profile for which this mockup applies transformations.
        /// </summary>
        public CharacterProfile Profile { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether or not this armature has any renderable objects on which it should act.
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// Gets a value indicating whether or not this armature has successfully built itself with bone information.
        /// </summary>
        public bool IsBuilt { get; private set; }

        /// <summary>
        /// For debugging purposes, each armature is assigned a globally-unique ID number upon creation.
        /// </summary>
        private static uint _nextGlobalId;
        private readonly uint _localId;

        /// <summary>
        /// Each skeleton is made up of several smaller "partial" skeletons.
        /// Each partial skeleton has its own list of bones, with a root bone at index zero.
        /// The root bone of a partial skeleton may also be a regular bone in a different partial skeleton.
        /// </summary>
        private ModelBone[][] _partialSkeletons;

        #region Bone Accessors -------------------------------------------------------------------------------

        /// <summary>
        /// Gets the number of partial skeletons contained in this armature.
        /// </summary>
        public int PartialSkeletonCount => _partialSkeletons.Length;

        /// <summary>
        /// Get the list of bones belonging to the partial skeleton at the given index.
        /// </summary>
        public ModelBone[] this[int i]
        {
            get => _partialSkeletons[i];
        }

        /// <summary>
        /// Returns the number of bones contained within the partial skeleton with the given index.
        /// </summary>
        public int GetBoneCountOfPartial(int partialIndex) => _partialSkeletons[partialIndex].Length;

        /// <summary>
        /// Get the bone at index 'j' within the partial skeleton at index 'i'.
        /// </summary>
        public ModelBone this[int i, int j]
        {
            get => _partialSkeletons[i][j];
        }

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

        public ModelBone MainRootBone => GetRootBoneOfPartial(0);

        /// <summary>
        /// Get the total number of bones in each partial skeleton combined.
        /// </summary>
        // In exactly one partial skeleton will the root bone be an independent bone. In all others, it's a reference to a separate, real bone.
        // For that reason we must subtract the number of duplicate bones
        public int TotalBoneCount => _partialSkeletons.Sum(x => x.Length);

        public IEnumerable<ModelBone> GetAllBones()
        {
            for (int i = 0; i < _partialSkeletons.Length; ++i)
            {
                for (int j = 0; j < _partialSkeletons[i].Length; ++j)
                {
                    yield return this[i, j];
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
        public bool SnapToReferencePose
        {
            get => GetReferenceSnap();
            set => SetReferenceSnap(value);
        }
        private bool _snapToReference;

        public Armature(CharacterProfile prof)
        {
            _localId = _nextGlobalId++;

            _partialSkeletons = Array.Empty<ModelBone[]>();

            Profile = prof;
            IsVisible = false;

            //cross-link the two, though I'm not positive the profile ever needs to refer back
            Profile.Armature = this;

            TryLinkSkeleton();

            PluginLog.LogDebug($"Instantiated {this}, attached to {Profile}");

        }

        /// <summary>
        /// Returns whether or not this armature was designed to apply to an object with the given name.
        /// </summary>
        public bool AppliesTo(string objectName) => Profile.AppliesTo(objectName);

        /// <inheritdoc/>
        public override string ToString()
        {
            return Built
                ? $"Armature (#{_localId}) on {Profile.CharacterName} with {TotalBoneCount} bone/s"
                : $"Armature (#{_localId}) on {Profile.CharacterName} with no skeleton reference";
        }

        private bool GetReferenceSnap()
        {
            if (Profile != Plugin.ProfileManager.ProfileOpenInEditor)
                _snapToReference = false;

            return _snapToReference;
        }

        private void SetReferenceSnap(bool value)
        {
            if (value && Profile == Plugin.ProfileManager.ProfileOpenInEditor)
                _snapToReference = false;

            _snapToReference = value;
        }

        /// <summary>
        /// Returns whether or not a link can be established between the armature and an in-game object.
        /// If unbuilt, the armature will use this opportunity to rebuild itself.
        /// </summary>
        public unsafe bool TryLinkSkeleton(bool forceRebuild = false)
        {
            try
            {
                if (Profile.OwnedOnly)
                {
                    foreach (var obj in DalamudServices.ObjectTable.GetPlayerOwnedCharacters())
                    {
                        if (!GameDataHelper.IsValidGameObject(obj) || !Profile.AppliesTo(obj))
                            continue;

                        CharacterBase* cBase = obj.ToCharacterBase();

                        if (!Built || forceRebuild)
                        {
                            RebuildSkeleton(cBase);
                        }
                        else if (NewBonesAvailable(cBase))
                        {
                            AugmentSkeleton(cBase);
                        }
                        return true;
                    }
                } else
                {
                    foreach (var obj in DalamudServices.ObjectTable)
                    {
                        if (!GameDataHelper.IsValidGameObject(obj) || !Profile.AppliesTo(obj))
                            continue;

                        CharacterBase* cBase = obj.ToCharacterBase();

                        if (!Built || forceRebuild)
                        {
                            RebuildSkeleton(cBase);
                        }
                        else if (NewBonesAvailable(cBase))
                        {
                            AugmentSkeleton(cBase);
                        }
                        return true;
                    }
                }
            }
            catch
            {
                // This is on wait until isse #191 on Github responds. Keeping it in code, delete it if I forget and this is longer then a month ago.
                
                // Disabling this if its any Default Profile due to Log spam. A bit crazy but hey, if its for me id Remove Default profiles all together so this is as much as ill do for now! :)
                //if(!(Profile.CharacterName.Equals(Constants.DefaultProfileCharacterName) || Profile.CharacterName.Equals("DefaultCutscene"))) {
                    PluginLog.LogError($"Error occured while attempting to link skeleton: {this}");
                //}
            }

            return false;
        }

        private bool NewBonesAvailable(CharacterBase* cBase)
        {
            if (cBase == null)
            {
                return false;
            }
            else if (cBase->Skeleton->PartialSkeletonCount > _partialSkeletons.Length)
            {
                return true;
            }
            else
            {
                for (int i = 0; i < cBase->Skeleton->PartialSkeletonCount; ++i)
                {
                    hkaPose* newPose = cBase->Skeleton->PartialSkeletons[i].GetHavokPose(Constants.TruePoseIndex);
                    if (newPose != null
                        && newPose->Skeleton->Bones.Length > _partialSkeletons[i].Length)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Rebuild the armature using the provided character base as a reference.
        /// </summary>
        public void RebuildSkeleton(CharacterBase* cBase)
        {
            if (cBase == null) 
                return;

            List<List<ModelBone>> newPartials = ParseBonesFromObject(this, cBase);

            _partialSkeletons = newPartials.Select(x => x.ToArray()).ToArray();

            PluginLog.LogDebug($"Rebuilt {this}");
        }

        public void AugmentSkeleton(CharacterBase* cBase)
        {
            if (cBase == null)
                return;

            List<List<ModelBone>> oldPartials = _partialSkeletons.Select(x => x.ToList()).ToList();
            List<List<ModelBone>> newPartials = ParseBonesFromObject(this, cBase);

            //for each of the new partial skeletons discovered...
            for (int i = 0; i < newPartials.Count; ++i)
            {
                //if the old skeleton doesn't contain the new partial at all, add the whole thing
                if (i > oldPartials.Count)
                {
                    oldPartials.Add(newPartials[i]);
                }
                //otherwise, add every model bone the new partial has that the old one doesn't
                else
                {
                    for (int j = oldPartials[i].Count; j < newPartials[i].Count; ++j)
                    {
                        oldPartials[i].Add(newPartials[i][j]);
                    }
                }
            }

            _partialSkeletons = oldPartials.Select(x => x.ToArray()).ToArray();

            PluginLog.LogDebug($"Augmented {this} with new bones");
        }

        private static unsafe List<List<ModelBone>> ParseBonesFromObject(Armature arm, CharacterBase* cBase)
        {
            List<List<ModelBone>> newPartials = new();

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
                            //time to build a new bone
                            ModelBone newBone = new(arm, boneName, pSkeleIndex, boneIndex);

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

                            if (arm.Profile.Bones.TryGetValue(boneName, out BoneTransform? bt)
                                && bt != null)
                            {
                                newBone.UpdateModel(bt);
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
            }
            catch (Exception ex)
            {
                PluginLog.LogError($"Error parsing armature skeleton from {cBase->ToString()}:\n\t{ex}");
            }

            return newPartials;
        }

        public void UpdateBoneTransform(int partialIdx, int boneIdx, BoneTransform bt, bool mirror = false, bool propagate = false)
        {
            this[partialIdx, boneIdx].UpdateModel(bt, mirror, propagate);
        }

        /// <summary>
        /// Iterate through this armature's model bones and apply their associated transformations
        /// to all of their in-game siblings
        /// </summary>
        public unsafe void ApplyTransformation(GameObject obj)
        {
            CharacterBase* cBase = obj.ToCharacterBase();

            if (cBase != null)
            {
                foreach (ModelBone mb in GetAllBones().Where(x => x.CustomizedTransform.IsEdited()))
                {
                    if (mb == MainRootBone)
                    {
                        //the main root bone's position information is handled by a different hook
                        //so there's no point in trying to update it here
                        //meanwhile root scaling has special rules
                        if (!IsModifiedScale(mb))
                            continue;
                        if (obj.HasScalableRoot() && cBase->DrawObject.IsVisible)
                            cBase->DrawObject.Object.Scale = mb.CustomizedTransform.Scaling;
                    }
                    else
                    {
                        mb.ApplyModelTransform(cBase);
                    }
                }
            }
        }

        /// <summary>
        /// Checks for a non-zero and non-identity (root) scale.
        /// </summary>
        /// <param name="mb">The bone to check</param>
        /// <returns>If the scale should be applied.</returns>
        private static bool IsModifiedScale(ModelBone mb)
        {
            return (mb.CustomizedTransform.Scaling.X != 0 && mb.CustomizedTransform.Scaling.X != 1) ||
                   (mb.CustomizedTransform.Scaling.Y != 0 && mb.CustomizedTransform.Scaling.Y != 1) ||
                   (mb.CustomizedTransform.Scaling.Z != 0 && mb.CustomizedTransform.Scaling.Z != 1);
        }


        /// <summary>
        /// Iterate through the skeleton of the given character base, and apply any transformations
        /// for which this armature contains corresponding model bones. This method of application
        /// is safer but more computationally costly
        /// </summary>
        public unsafe void ApplyPiecewiseTransformation(GameObject obj)
        {
            CharacterBase* cBase = obj.ToCharacterBase();

            if (cBase != null)
            {
                for (int pSkeleIndex = 0; pSkeleIndex < cBase->Skeleton->PartialSkeletonCount; ++pSkeleIndex)
                {
                    hkaPose* currentPose = cBase->Skeleton->PartialSkeletons[pSkeleIndex].GetHavokPose(Constants.TruePoseIndex);

                    if (currentPose != null)
                    {
                        for (int boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Length; ++boneIndex)
                        {
                            if (GetBoneAt(pSkeleIndex, boneIndex) is ModelBone mb
                                && mb != null
                                && mb.BoneName == currentPose->Skeleton->Bones[boneIndex].Name.String)
                            {
                                if (mb == MainRootBone)
                                {
                                    if (obj.HasScalableRoot() && IsModifiedScale(mb))
                                        cBase->DrawObject.Object.Scale = mb.CustomizedTransform.Scaling;
                                }
                                else
                                {
                                    mb.ApplyModelTransform(cBase);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ApplyRootTranslation(CharacterBase* cBase)
        {
            //I'm honestly not sure if we should or even can check if cBase->DrawObject or cBase->DrawObject.Object is a valid object
            //So for now let's assume we don't need to check for that
            if (cBase != null)
            {
                if (!Profile.Bones.TryGetValue("n_root", out BoneTransform? rootBoneTransform))
                    return;

                if (rootBoneTransform.Translation.X == 0 &&
                    rootBoneTransform.Translation.Y == 0 &&
                    rootBoneTransform.Translation.Z == 0)
                    return;

                if (!cBase->DrawObject.IsVisible)
                    return;

                var newPosition = new FFXIVClientStructs.FFXIV.Common.Math.Vector3
                {
                    X = cBase->DrawObject.Object.Position.X + MathF.Max(rootBoneTransform.Translation.X, 0.01f),
                    Y = cBase->DrawObject.Object.Position.Y + MathF.Max(rootBoneTransform.Translation.Y, 0.01f),
                    Z = cBase->DrawObject.Object.Position.Z + MathF.Max(rootBoneTransform.Translation.Z, 0.01f)
                };

                cBase->DrawObject.Object.Position = newPosition;
            }
        }

        private static bool AreTwinnedNames(string name1, string name2)
        {
            return (name1[^1] == 'r' ^ name2[^1] == 'r')
                && (name1[^1] == 'l' ^ name2[^1] == 'l')
                && (name1[0..^1] == name2[0..^1]);
        }

        //public void OverrideWithReferencePose()
        //{
        //    for (var pSkeleIndex = 0; pSkeleIndex < Skeleton->PartialSkeletonCount; ++pSkeleIndex)
        //    {
        //        for (var poseIndex = 0; poseIndex < 4; ++poseIndex)
        //        {
        //            var snapPose = Skeleton->PartialSkeletons[pSkeleIndex].GetHavokPose(poseIndex);

        //            if (snapPose != null)
        //            {
        //                snapPose->SetToReferencePose();
        //            }
        //        }
        //    }
        //}

        //public void OverrideRootParenting()
        //{
        //    var pSkeleNot = Skeleton->PartialSkeletons[0];

        //    for (var pSkeleIndex = 1; pSkeleIndex < Skeleton->PartialSkeletonCount; ++pSkeleIndex)
        //    {
        //        var partialSkele = Skeleton->PartialSkeletons[pSkeleIndex];

        //        for (var poseIndex = 0; poseIndex < 4; ++poseIndex)
        //        {
        //            var currentPose = partialSkele.GetHavokPose(poseIndex);

        //            if (currentPose != null && partialSkele.ConnectedBoneIndex >= 0)
        //            {
        //                int boneIdx = partialSkele.ConnectedBoneIndex;
        //                int parentBoneIdx = partialSkele.ConnectedParentBoneIndex;

        //                var transA = currentPose->AccessBoneModelSpace(boneIdx, 0);
        //                var transB = pSkeleNot.GetHavokPose(0)->AccessBoneModelSpace(parentBoneIdx, 0);

        //                //currentPose->AccessBoneModelSpace(parentBoneIdx, hkaPose.PropagateOrNot.DontPropagate);

        //                for (var i = 0; i < currentPose->Skeleton->Bones.Length; ++i)
        //                {
        //                    currentPose->ModelPose[i] = ApplyPropagatedTransform(currentPose->ModelPose[i], transB,
        //                        transA->Translation, transB->Rotation);
        //                    currentPose->ModelPose[i] = ApplyPropagatedTransform(currentPose->ModelPose[i], transB,
        //                        transB->Translation, transA->Rotation);
        //                }
        //            }
        //        }
        //    }
        //}

        //private hkQsTransformf ApplyPropagatedTransform(hkQsTransformf init, hkQsTransformf* propTrans,
        //    hkVector4f initialPos, hkQuaternionf initialRot)
        //{
        //    var sourcePosition = propTrans->Translation.GetAsNumericsVector().RemoveWTerm();
        //    var deltaRot = propTrans->Rotation.ToQuaternion() / initialRot.ToQuaternion();
        //    var deltaPos = sourcePosition - initialPos.GetAsNumericsVector().RemoveWTerm();

        //    hkQsTransformf output = new()
        //    {
        //        Translation = Vector3
        //            .Transform(init.Translation.GetAsNumericsVector().RemoveWTerm() - sourcePosition, deltaRot)
        //            .ToHavokTranslation(),
        //        Rotation = deltaRot.ToHavokRotation(),
        //        Scale = init.Scale
        //    };

        //    return output;
        //}
    }
}