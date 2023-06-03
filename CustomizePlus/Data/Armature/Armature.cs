// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using CustomizePlus.Data.Profile;
using CustomizePlus.Helpers;
using CustomizePlus.Memory;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;

namespace CustomizePlus.Data.Armature
{
    /// <summary>
    ///     Represents an interface between the bone edits made by the user and the actual
    ///     bone information used ingame.
    /// </summary>
    public unsafe class Armature
    {
        private static int NextGlobalID;
        //public ObjectKind DalamudObjectKind
        //{
        //	get => ObjectRef == null
        //		? ObjectKind.None
        //		: this.ObjectRef.ObjectKind;
        //}

        public readonly Dictionary<string, ModelBone> Bones;
        private readonly int LocalID;

        //public GameObject? ObjectRef;
        public RenderObject* ObjectRef;

        public CharacterProfile Profile;

        public Armature(CharacterProfile prof)
        {
            LocalID = NextGlobalID++;

            Profile = prof;
            Visible = false;
            ObjectRef = null;
            Bones = new Dictionary<string, ModelBone>();

            Profile.Armature = this;

            TryLinkSkeleton();

            PluginLog.LogDebug($"Instantiated {this}, attached to {Profile}");
        }

        public bool Visible { get; set; }

        public RenderSkeleton* InGameSkeleton =>
            ObjectRef != null
                ? ObjectRef->Skeleton
                : (RenderSkeleton*)IntPtr.Zero;

        public override string ToString()
        {
            if (ObjectRef == null)
            {
                return $"Armature ({LocalID}) on {Profile.CharName} with no skeleton reference";
            }

            return $"Armature ({LocalID}) on {Profile.CharName} with {Bones.Count} bones";
        }

        public IEnumerable<string> GetExtantBoneNames()
        {
            return Bones.Keys;
        }

        private string[] GetHypotheticalBoneNames()
        {
            var output = new List<string>();

            for (var pSkeleIndex = 0; pSkeleIndex < InGameSkeleton->Length; ++pSkeleIndex)
            {
                for (var poseIndex = 0; poseIndex < 4; ++poseIndex)
                {
                    var currentPose = poseIndex switch
                    {
                        0 => InGameSkeleton->PartialSkeletons[pSkeleIndex].Pose1,
                        1 => InGameSkeleton->PartialSkeletons[pSkeleIndex].Pose1,
                        3 => InGameSkeleton->PartialSkeletons[pSkeleIndex].Pose1,
                        _ => null
                    };

                    if (currentPose != null)
                    {
                        for (var boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Count; ++boneIndex)
                        {
                            if (currentPose->Skeleton->Bones[boneIndex].GetName() is string boneName &&
                                boneName != null)
                            {
                                output.Add(boneName);
                            }
                        }
                    }
                }
            }

            return output.ToArray();
        }

        public bool TryLinkSkeleton()
        {
            if (GameDataHelper.FindModelByName(Profile.CharName) is GameObject obj && obj != null)
            {
                var ro = RenderObject.FromActor(obj);

                if (ro != ObjectRef)
                {
                    ObjectRef = ro;
                    RebuildSkeleton(obj);
                }

                return true;
            }

            ObjectRef = null;
            return false;
        }

        public void RebuildSkeleton(GameObject? obj = null)
        {
            if (obj != null)
            {
                ObjectRef = RenderObject.FromActor(obj);
            }

            if (InGameSkeleton == null)
            {
                PluginLog.LogError($"Error obtaining skeleton from GameObject '{obj}'");
                return;
            }

            Bones.Clear();

            try
            {
                //build the skeleton
                for (var pSkeleIndex = 0; pSkeleIndex < InGameSkeleton->Length; ++pSkeleIndex)
                {
                    for (var poseIndex = 0; poseIndex < 4; ++poseIndex)
                    {
                        var currentPose = poseIndex switch
                        {
                            0 => InGameSkeleton->PartialSkeletons[pSkeleIndex].Pose1,
                            1 => InGameSkeleton->PartialSkeletons[pSkeleIndex].Pose1,
                            3 => InGameSkeleton->PartialSkeletons[pSkeleIndex].Pose1,
                            _ => null
                        };

                        if (currentPose != null)
                        {
                            for (var boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Count; ++boneIndex)
                            {
                                if (currentPose->Skeleton->Bones[boneIndex].GetName() is string boneName &&
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

                                        if (currentPose->Skeleton->ParentIndices.Count > boneIndex
                                            && currentPose->Skeleton->ParentIndices[boneIndex] is short pIndex
                                            && pIndex >= 0
                                            && currentPose->Skeleton->Bones.Count > pIndex
                                            && currentPose->Skeleton->Bones[pIndex].GetName() is string outParentBone
                                            && outParentBone != null)
                                        {
                                            parentBone = outParentBone;
                                        }

                                        Bones[boneName] = new ModelBone(this, boneName, parentBone ?? string.Empty,
                                            pSkeleIndex, poseIndex, boneIndex);

                                        if (Profile.Bones.TryGetValue(boneName, out var bt) && bt.IsEdited())
                                        {
                                            Bones[boneName].PluginTransform = bt;
                                        }
                                        else
                                        {
                                            Bones[boneName].PluginTransform = new BoneTransform();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

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
            {
                mb.UpdateModel(bt, mirror, propagate);
            }
            else
            {
                PluginLog.LogError($"{boneName} doesn't exist in armature {this}");
            }

            Bones[boneName].UpdateModel(bt, mirror, propagate);
        }

        public void ApplyTransformation()
        {
            //for (int i = 0; i < this.InGameSkeleton->Length; ++i)
            //{
            //	for (int k = 0; k < this.InGameSkeleton->PartialSkeletons[i].Pose1->Skeleton->Bones.Count; ++k)
            //	{
            //		string name = this.InGameSkeleton->PartialSkeletons[i].Pose1->Skeleton->Bones[k].GetName() ?? String.Empty;

            //		if (this.Bones.TryGetValue(name, out ModelBone? mb) && mb != null)
            //		{
            //			Transform temp = this.InGameSkeleton->PartialSkeletons[i].Pose1->Transforms[k];
            //			temp = mb.PluginTransform.ModifyExistingTransformation(temp);
            //			this.InGameSkeleton->PartialSkeletons[i].Pose1->Transforms[k] = temp;
            //		}
            //	}
            //}

            foreach (var kvp in Bones.Where(x => x.Value.PluginTransform.IsEdited()))
            {
                kvp.Value.ApplyModelTransform();
            }
        }


        private void DiscoverParentage()
        {
            foreach (var potentialParent in Bones)
            {
                foreach (var potentialChild in Bones)
                {
                    if (potentialChild.Value.ParentBoneName == potentialParent.Key)
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

        private static ModelBone TraceAncestry(ModelBone mb)
        {
            var ancestor = mb;

            while (ancestor.Parent != null)
            {
                ancestor = ancestor.Parent;
            }

            return ancestor;
        }

        //private bool TryDiscoverDangling(out ModelBone[] dangling)
        //{
        //	if (this.modelRoot == null)
        //	{
        //		dangling = Array.Empty<ModelBone>();
        //		return true; //the whole skeleton is dangling
        //	}

        //	List<ModelBone> danglingBones = new();

        //	foreach (ModelBone mb in this.Bones.Values)
        //	{
        //		if (TraceAncestry(mb) != this.modelRoot)
        //		{
        //			danglingBones.Add(mb);
        //		}
        //	}

        //	dangling = danglingBones.ToArray();
        //	return danglingBones.Any();
        //}
    }
}