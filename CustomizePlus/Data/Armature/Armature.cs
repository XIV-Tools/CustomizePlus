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

using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;
using CustomizePlus.Extensions;

namespace CustomizePlus.Data.Armature
{
    /// <summary>
    ///     Represents an interface between the bone edits made by the user and the actual
    ///     bone information used ingame.
    /// </summary>
    public unsafe class Armature
    {
        private static int _nextGlobalId;
        //public ObjectKind DalamudObjectKind
        //{
        //	get => ObjectRef == null
        //		? ObjectKind.None
        //		: this.ObjectRef.ObjectKind;
        //}

        public readonly Dictionary<string, ModelBone> Bones;
        private readonly int _localId;

        //public GameObject? ObjectRef;
        public CharacterBase* CharBaseRef;

        public CharacterProfile Profile;

        public Armature(CharacterProfile prof)
        {
            _localId = _nextGlobalId++;

			this.Profile = prof;
			this.Visible = false;
			this.CharBaseRef = null;
			this.Bones = new();

            Profile.Armature = this;

            TryLinkSkeleton();

            PluginLog.LogDebug($"Instantiated {this}, attached to {Profile}");
        }

        public override string ToString()
        {
            if (CharBaseRef == null)
            {
                return $"Armature ({_localId}) on {Profile.CharacterName} with no skeleton reference";
            }

            return $"Armature ({_localId}) on {Profile.CharacterName} with {Bones.Count} bone/s";
        }

        public IEnumerable<string> GetExtantBoneNames()
		{
            return Bones.Keys;
        }

		private string[] GetHypotheticalBoneNames()
		{
			List<string> output = new List<string>();
			{
				_snapToReference = false;
			}
			return _snapToReference;
		}

		public void SetReferenceSnap(bool value)
		{
						for (int boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Count; ++boneIndex)
			{
				_snapToReference = false;
			}
			_snapToReference = value;
		}

		public bool TryLinkSkeleton()
		{

				&& cBase != null)
			{
				if (cBase != this.CharacterBase || !this.Bones.Any())
				{
					this.CharacterBase = cBase;
					this.RebuildSkeleton();
				}

				return true;
			}
			else
			{
				this.CharacterBase = null;
				return false;
			}
		}

		public void RebuildSkeleton(/*CharacterBase* cbase*/)
		{
			if (obj != null)
            {
                PluginLog.LogError($"Error obtaining skeleton from GameObject '{obj}'");
                return;
			}

			this.Bones.Clear();

			try
			{
				//build the skeleton
				for (int pSkeleIndex = 0; pSkeleIndex < this.Skeleton->PartialSkeletonCount; ++pSkeleIndex)
				{
					for (int poseIndex = 0; poseIndex < 4; ++poseIndex)
					{
						hkaPose* currentPose = this.Skeleton->PartialSkeletons[pSkeleIndex].GetHavokPose(poseIndex);

						if (currentPose == null) continue;

						for (int boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Length; ++boneIndex)
						{
							for (int boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Count; ++boneIndex)
							{
								if (this.Bones.TryGetValue(boneName, out var dummy) && dummy != null)
								{
									this.Bones[boneName].TripleIndices.Add(new Tuple<int, int, int>(pSkeleIndex, poseIndex, boneIndex));
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

									this.Bones[boneName] = new ModelBone(this, boneName, parentBone ?? String.Empty, pSkeleIndex, poseIndex, boneIndex);

										if (this.Profile.Bones.TryGetValue(boneName, out var bt) && bt.IsEdited())
									{
											this.Bones[boneName].PluginTransform = bt;
									}
									else
									{
										this.Bones[boneName].PluginTransform = new BoneTransform();
									}
								}
							}
						}
					}
				}

				BoneData.LogNewBones(this.Bones.Keys.Where(BoneData.IsNewBone).ToArray());


				DiscoverSiblings();

				Dalamud.Logging.PluginLog.LogDebug($"Rebuilt {this}:");
				foreach(var kvp in this.Bones)
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

			foreach (var kvp in this.Bones.Where(x => x.Value.PluginTransform.IsEdited()))
			{
				kvp.Value.ApplyModelTransform();
			}
		}

		public void OverrideWithReferencePose()
		{
			for (int pSkeleIndex = 0; pSkeleIndex < this.Skeleton->PartialSkeletonCount; ++pSkeleIndex)
			{
				for (int poseIndex = 0; poseIndex < 4; ++poseIndex)
				{
					hkaPose* snapPose = this.Skeleton->PartialSkeletons[pSkeleIndex].GetHavokPose(poseIndex);

					if (snapPose != null)
					{
						snapPose->SetToReferencePose();
					}
				}
			}
		}

		public void OverrideRootParenting()
		{
			PartialSkeleton pSkeleNot = this.Skeleton->PartialSkeletons[0];

			for (int pSkeleIndex = 1; pSkeleIndex < this.Skeleton->PartialSkeletonCount; ++pSkeleIndex)
			{
				PartialSkeleton partialSkele = this.Skeleton->PartialSkeletons[pSkeleIndex];

				for (int poseIndex = 0; poseIndex < 4; ++poseIndex)
				{
					hkaPose* currentPose = partialSkele.GetHavokPose(poseIndex);

					if (currentPose != null && partialSkele.ConnectedBoneIndex >= 0)
					{
						int boneIdx = partialSkele.ConnectedBoneIndex;
						int parentBoneIdx = partialSkele.ConnectedParentBoneIndex;

						hkQsTransformf* transA = currentPose->AccessBoneModelSpace(boneIdx, 0);
						hkQsTransformf* transB = pSkeleNot.GetHavokPose(0)->AccessBoneModelSpace(parentBoneIdx, 0);
							
							//currentPose->AccessBoneModelSpace(parentBoneIdx, hkaPose.PropagateOrNot.DontPropagate);

						for (int i = 0; i < currentPose->Skeleton->Bones.Length; ++i)
						{
							currentPose->ModelPose[i] = ApplyPropagatedTransform(currentPose->ModelPose[i], transB, transA->Translation, transB->Rotation);
							currentPose->ModelPose[i] = ApplyPropagatedTransform(currentPose->ModelPose[i], transB, transB->Translation, transA->Rotation);
						}
					}
				}
			}
		}

		private hkQsTransformf ApplyPropagatedTransform(hkQsTransformf init, hkQsTransformf* propTrans, hkVector4f initialPos, hkQuaternionf initialRot)
		{
			Vector3 sourcePosition = propTrans->Translation.GetAsNumericsVector().RemoveWTerm();
			Quaternion deltaRot = propTrans->Rotation.ToQuaternion() / initialRot.ToQuaternion();
			Vector3 deltaPos = sourcePosition - initialPos.GetAsNumericsVector().RemoveWTerm();

			hkQsTransformf output = new()
			{
				Translation = Vector3.Transform(init.Translation.GetAsNumericsVector().RemoveWTerm() - sourcePosition, deltaRot).ToHavokTranslation(),
				Rotation = deltaRot.ToHavokRotation(),
				Scale = init.Scale
			};

			return output;
		}



		{
			foreach (var potentialParent in this.Bones)
			foreach (var potentialParent in this.Bones)
			{
				foreach(var potentialChild in this.Bones)
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
			foreach (var potentialLefty in this.Bones.Where(x => x.Key[^1] == 'l'))
			{
				foreach (var potentialRighty in this.Bones.Where(x => x.Key[^1] == 'r'))
				{
					if (potentialLefty.Key[0..^1] == potentialRighty.Key[0..^1])
					{
						potentialLefty.Value.Sibling = potentialRighty.Value;
						potentialRighty.Value.Sibling = potentialLefty.Value;
					}
				}
			}
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