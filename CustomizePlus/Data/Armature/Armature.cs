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

namespace CustomizePlus.Data.Armature
{
    /// <summary>
    /// Represents a "copy" of the ingame skeleton upon which the linked character profile is meant to operate.
    /// Acts as an interface by which the in-game skeleton can be manipulated on a bone-by-bone basis.
    /// </summary>
    public unsafe class Armature : IBoneContainer
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
        private ModelBone[][] _weaponPartialsRight;
        private ModelBone[][] _weaponPartialsLeft;

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

        /// <summary>
        /// Get all individual model bones making up this armature
        /// </summary>
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
        /// Get all individual model bones making up this armature EXCEPT for partial root bones
        /// </summary>
        public IEnumerable<ModelBone> GetAllEditableBones() => GetAllBones().Where(x => x is not PartialRootBone);

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
                    else if (NewBonesAvailable(cBase))
                    {
                        AugmentSkeleton(cBase);
                    }
                    return cBase;
                }
            }
            catch
            {
                PluginLog.LogError($"Error occured while attempting to link skeleton: {this}");
            }

            return null;
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
                            ModelBone newBone;

                            if (pSkeleIndex == 0 && boneIndex == 0)
                            {
                                newBone = new ModelRootBone(arm, boneName);
                            }
                            else if (boneIndex == 0)
                            {
                                ModelBone cloneOf = newPartials[0][currentPartial.ConnectedBoneIndex];
                                newBone = new PartialRootBone(arm, cloneOf, boneName, pSkeleIndex);
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

                                if (arm.Profile.Bones.TryGetValue(boneName, out BoneTransform? bt)
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
            }
            catch (Exception ex)
            {
                PluginLog.LogError($"Error parsing armature skeleton from {cBase->ToString()}:\n\t{ex}");
            }

            return newPartials;
        }

        /// <summary>
        /// Iterate through this armature's model bones and apply their associated transformations
        /// to all of their in-game siblings.
        /// </summary>
        public unsafe void ApplyTransformation(GameObject obj)
        {
            CharacterBase* cBase = obj.ToCharacterBase();

            if (cBase != null)
            {
                for (int pSkeleIndex = 0; pSkeleIndex < cBase->Skeleton->PartialSkeletonCount; ++pSkeleIndex)
                {
                    hkaPose* currentPose = cBase->Skeleton->PartialSkeletons[pSkeleIndex].GetHavokPose(Constants.TruePoseIndex);

                    if (currentPose != null)
                    {
                        //if (SnapToReferencePose)
                        //{
                        //    currentPose->SetToReferencePose();
                        //}

                        for (int boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Length; ++boneIndex)
                        {
                            if (GetBoneAt(pSkeleIndex, boneIndex) is ModelBone mb
                                && mb != null
                                && mb.BoneName == currentPose->Skeleton->Bones[boneIndex].Name.String)
                            {
                                if (GameStateHelper.GameInPosingMode())
                                {
                                    mb.ApplyModelScale(cBase);
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
            if (cBase != null && _partialSkeletons.Any() && _partialSkeletons.First().Any())
            {
                _partialSkeletons[0][0].ApplyModelTranslationAsIs(cBase);
            }
        }

        private static bool AreTwinnedNames(string name1, string name2)
        {
            return (name1[^1] == 'r' ^ name2[^1] == 'r')
                && (name1[^1] == 'l' ^ name2[^1] == 'l')
                && (name1[0..^1] == name2[0..^1]);
        }

        public IEnumerable<TransformInfo> GetBoneTransformValues(BoneAttribute attribute, PosingSpace space)
        {
            return GetAllEditableBones().Select(x => new TransformInfo(this, x, attribute, space));
        }

        public void UpdateBoneTransformValue(TransformInfo newTransform, BoneUpdateMode mode, bool mirrorChanges, bool propagateChanges)
        {
            if (GetAllBones().FirstOrDefault(x => x.BoneName == newTransform.BoneCodeName) is ModelBone mb
                && mb != null)
            {
                BoneTransform oldTransform = mb.GetTransformation();

                BoneAttribute att = mode switch
                {
                    BoneUpdateMode.Position => BoneAttribute.Position,
                    BoneUpdateMode.Rotation => BoneAttribute.Rotation,
                    _ => BoneAttribute.Scale
                };

                oldTransform.UpdateAttribute(att, newTransform.TransformationValue);
                mb.UpdateModel(oldTransform, mirrorChanges, propagateChanges);
            }
        }
    }
}