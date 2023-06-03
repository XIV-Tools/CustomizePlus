// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Data.Profile;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;

namespace CustomizePlus.Data.Armature
{
	/// <summary>
	/// Represents an interface between the bone edits made by the user and the actual
	/// bone information used ingame. 
	/// </summary>
	public unsafe class Armature
	{
		private static int NextGlobalID = 0;
		private readonly int LocalID;

		public CharacterProfile Profile;

		public bool Visible { get; set; }


		public CharacterBase* CharacterBase;
		public Skeleton* Skeleton => this.CharacterBase->Skeleton;

		private bool _snapToReference = false;


		public readonly Dictionary<string, ModelBone> Bones;

		public Armature(CharacterProfile prof)
		{
			this.LocalID = NextGlobalID++;

			this.Profile = prof;
			this.Visible = false;
			this.CharacterBase = null;
			this.Bones = new();

			this.Profile.Armature = this;

			this.TryLinkSkeleton();

			Dalamud.Logging.PluginLog.LogDebug($"Instantiated {this}, attached to {this.Profile}");
		}

		public override string ToString()
		{
			if (this.CharacterBase == null)
			{
				return $"Armature ({this.LocalID}) on {this.Profile.CharName} with no skeleton reference";
			}
			else
			{
				return $"Armature ({this.LocalID}) on {this.Profile.CharName} with {this.Bones.Count} bones";
			}
		}

		public IEnumerable<string> GetExtantBoneNames()
		{
			return this.Bones.Keys;
		}

		public bool GetReferenceSnap()
		{
			if (Profile != Plugin.ProfileManager.ProfileOpenInEditor)
			{
				_snapToReference = false;
			}
			return _snapToReference;
		}

		public void SetReferenceSnap(bool value)
		{
			if (value && Profile == Plugin.ProfileManager.ProfileOpenInEditor)
			{
				_snapToReference = false;
			}
			_snapToReference = value;
		}

		public bool TryLinkSkeleton()
		{
			if (Helpers.GameDataHelper.TryLookupCharacterBase(this.Profile.CharName, out CharacterBase* cBase)
				&& cBase != null)
			{
				if (cBase != this.CharacterBase)
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
			if (this.CharacterBase == null)
			{
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
							if (currentPose->Skeleton->Bones[boneIndex].Name.String is string boneName && boneName != null)
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

				DiscoverParentage();
				DiscoverSiblings();

				Dalamud.Logging.PluginLog.LogDebug($"Rebuilt {this}:");
				foreach(var kvp in this.Bones)
				{
					Dalamud.Logging.PluginLog.LogDebug($"\t- {kvp.Value}");
				}
			}
			catch (Exception ex)
			{
				Dalamud.Logging.PluginLog.LogError($"Error rebuilding armature skeleton: {ex}");
			}
		}

		public void UpdateBoneTransform(string boneName, BoneTransform bt, bool mirror = false, bool propagate = false)
		{
			if (this.Bones.TryGetValue(boneName, out var mb) && mb != null)
			{
				mb.UpdateModel(bt, mirror, propagate);
			}
			else
			{
				Dalamud.Logging.PluginLog.LogError($"{boneName} doesn't exist in armature {this}");
			}

			this.Bones[boneName].UpdateModel(bt, mirror, propagate);
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

		private void DiscoverParentage()
		{
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
