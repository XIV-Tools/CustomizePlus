// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Data.Profile;
using CustomizePlus.Memory;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

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

		//public GameObject? ObjectRef;
		public RenderObject* ObjectRef;

		public RenderSkeleton* InGameSkeleton
		{
			get => this.ObjectRef != null
				? this.ObjectRef->Skeleton
				: (RenderSkeleton*)IntPtr.Zero;
		}
		//public ObjectKind DalamudObjectKind
		//{
		//	get => ObjectRef == null
		//		? ObjectKind.None
		//		: this.ObjectRef.ObjectKind;
		//}

		public readonly Dictionary<string, ModelBone> Bones;

		public Armature(CharacterProfile prof)
		{
			this.LocalID = NextGlobalID++;

			this.Profile = prof;
			this.Visible = false;
			this.ObjectRef = null;
			this.Bones = new();

			this.Profile.Armature = this;

			this.TryLinkSkeleton();

			Dalamud.Logging.PluginLog.LogDebug($"Instantiated {this}, attached to {this.Profile}");
		}

		public override string ToString()
		{
			if (this.ObjectRef == null)
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

		private string[] GetHypotheticalBoneNames()
		{
			List<string> output = new List<string>();

			for (int pSkeleIndex = 0; pSkeleIndex < this.InGameSkeleton->Length; ++pSkeleIndex)
			{
				for (int poseIndex = 0; poseIndex < 4; ++poseIndex)
				{
					HkaPose* currentPose = poseIndex switch
					{
						0 => this.InGameSkeleton->PartialSkeletons[pSkeleIndex].Pose1,
						1 => this.InGameSkeleton->PartialSkeletons[pSkeleIndex].Pose1,
						3 => this.InGameSkeleton->PartialSkeletons[pSkeleIndex].Pose1,
						_ => null
					};

					if (currentPose != null)
					{
						for (int boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Count; ++boneIndex)
						{
							if (currentPose->Skeleton->Bones[boneIndex].GetName() is string boneName && boneName != null)
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
			if (Helpers.GameDataHelper.FindModelByName(this.Profile.CharName) is GameObject obj && obj != null)
			{
				RenderObject* ro = RenderObject.FromActor(obj);

				if (ro != this.ObjectRef)
				{
					this.ObjectRef = ro;
					this.RebuildSkeleton(obj);
				}

				return true;
			}
			else
			{
				this.ObjectRef = null;
				return false;
			}
		}

		public void RebuildSkeleton(GameObject? obj = null)
		{
			if (obj != null)
			{
				this.ObjectRef = RenderObject.FromActor(obj);
			}

			if (this.InGameSkeleton == null)
			{
				Dalamud.Logging.PluginLog.LogError($"Error obtaining skeleton from GameObject '{obj}'");
				return;
			}

			this.Bones.Clear();

			try
			{
				//build the skeleton
				for (int pSkeleIndex = 0; pSkeleIndex < this.InGameSkeleton->Length; ++pSkeleIndex)
				{
					for (int poseIndex = 0; poseIndex < 4; ++poseIndex)
					{
						HkaPose* currentPose = poseIndex switch
						{
							0 => this.InGameSkeleton->PartialSkeletons[pSkeleIndex].Pose1,
							1 => this.InGameSkeleton->PartialSkeletons[pSkeleIndex].Pose1,
							3 => this.InGameSkeleton->PartialSkeletons[pSkeleIndex].Pose1,
							_ => null
						};

						if (currentPose != null)
						{
							for (int boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Count; ++boneIndex)
							{
								if (currentPose->Skeleton->Bones[boneIndex].GetName() is string boneName && boneName != null)
								{
									if (this.Bones.TryGetValue(boneName, out var dummy) && dummy != null)
									{
										this.Bones[boneName].TripleIndices.Add(new Tuple<int, int, int>(pSkeleIndex, poseIndex, boneIndex));
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


		private void DiscoverParentage()
		{
			foreach (var potentialParent in this.Bones)
			{
				foreach(var potentialChild in this.Bones)
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

		private static ModelBone TraceAncestry(ModelBone mb)
		{
			ModelBone ancestor = mb;

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
