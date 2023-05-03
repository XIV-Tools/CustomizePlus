// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CustomizePlus.Memory;
using Dalamud.Game.ClientState.Objects.Types;
using static Anamnesis.Files.PoseFile;

namespace CustomizePlus.Data.Armature
{
	/// <summary>
	/// Represents an interface between the bone edits made by the user and the actual
	/// bone information used ingame. 
	/// </summary>
	public unsafe class Armature
	{
		public CharacterProfile Profile;

		RenderSkeleton* RSkeleton;

		ModelBone ModelRoot;

		private Dictionary<string, ModelBone> Bones;

		public Armature(CharacterProfile prof, GameObject obj)
		{
			this.Profile = prof;
			this.RSkeleton = RenderObject.FromActor(obj)->Skeleton;

			//build the skeleton
			for(int i = 0; i < this.RSkeleton->Length; ++i)
			{
				HkaPose* currentPose = this.RSkeleton->PartialSkeletons[i].Pose1;

				for (int j = 0; j < currentPose->Skeleton->Bones.Count; ++j)
				{
					var bone = currentPose->Skeleton->Bones[j];

					if (bone.GetName() is string boneName)
					{
						this.Bones[boneName] = new ModelBone(this, boneName, j, currentPose);
					}
				}
			}

			if (this.Bones.TryGetValue(Constants.RootBoneName, out ModelBone mb))
			{
				this.ModelRoot = mb;
				DiscoverParentage();
				DiscoverSiblings();
			}
			else
			{
				Dalamud.Logging.PluginLog.LogError("Armature didn't contain root bone");
			}
		}

		public void UpdateTransformation(string boneName, BoneTransform bec)
		{
			this.Bones[boneName].PluginTransform = bec;
			this.Profile.Bones[boneName] = bec;
		}

		public void ApplyTransformation()
		{
			foreach(var kvp in this.Bones)
			{
				kvp.Value.ApplyModelTransform();
			}
		}

		private void DiscoverParentage()
		{
			foreach(var kvp in this.Bones)
			{
				List<ModelBone> children = new List<ModelBone>();

				//iterate through all the bones and tally up which ones are
				//children of the one we're looking at

				for (int i = 0; i < this.RSkeleton->Length; ++i)
				{
					HkaPose* currentPose = this.RSkeleton->PartialSkeletons[i].Pose1;

					for (int j = 0; j < currentPose->Skeleton->Bones.Count; ++j)
					{
						if (currentPose->Skeleton->ParentIndices[j] == kvp.Value.BoneIndex
							&& currentPose->Skeleton->Bones[j].GetName() is string childName)
						{
							ModelBone mb = this.Bones[childName];
							children.Add(mb);

							mb.Parent = kvp.Value;
						}
					}
				}

				kvp.Value.Children = children.ToArray();
			}
		}

		private void DiscoverSiblings()
		{
			HashSet<ModelBone> bonesByLevel = new HashSet<ModelBone>() { this.ModelRoot };
			int depth = 0;

			while(bonesByLevel.Any())
			{
				//grab up all the bones on the level following this one
				HashSet<ModelBone> nextLevel = new(bonesByLevel.SelectMany(x => x.Children));

				//pick off one bone at a time and check to see if it looks like part of a pair
				//if so, see if any other bones at this level match it, and pair them up
				while(bonesByLevel.Any())
				{
					ModelBone mb = bonesByLevel.First();
					bonesByLevel.Remove(mb);

					if (mb.BoneName[^1] == 'l' || mb.BoneName[^1] == 'r')
					{
						ModelBone? mb2 = bonesByLevel.Where(x => x.BoneName[0..1] == mb.BoneName[0..1]).FirstOrDefault();

						if (mb2 != null)
						{
							bonesByLevel.Remove(mb2);
							mb.Sibling = mb2;
							mb2.Sibling = mb;
						}
					}
				}

				//once we've checked every bone at this level, move onto the next

				bonesByLevel = new HashSet<ModelBone>(nextLevel);
				++depth;
			}
		}
	}
}
