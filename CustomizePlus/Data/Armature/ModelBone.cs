// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Memory;
using System;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Collections.Generic;

namespace CustomizePlus.Data.Armature
{
	/// <summary>
	/// Represents a single bone of an ingame character's skeleton.
	/// </summary>
	public unsafe class ModelBone
	{
		public readonly Armature Armature;

		//public readonly int SkeletonIndex;
		//public readonly int PoseIndex;
		//public readonly int BoneIndex;

		public readonly List<Tuple<int, int, int>> TripleIndices = new();

		public readonly string BoneName;
		public readonly string? ParentBoneName;

		public BoneTransform PluginTransform;

		public ModelBone? Parent;
		public ModelBone? Sibling;
		public List<ModelBone> Children = new();

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			if (this.Parent != null) sb.Append($"[{this.Parent.BoneName}]-> ");
			sb.Append(this.BoneName);
			if (this.Sibling != null) sb.Append($" <->[{this.Sibling.BoneName}]");
			if (this.Children.Any()) sb.Append($" ({this.Children.Count} children)");
			else sb.Append($" (no children)");

			return sb.ToString();
		}

		public ModelBone(Armature arm, string name, string? parentName, int skeleIndex, int poseIndex, int boneIndex)
		{
			this.Armature = arm;

			//this.SkeletonIndex = skeleIndex;
			//this.PoseIndex = poseIndex;
			//this.BoneIndex = boneIndex;

			this.TripleIndices.Add(new Tuple<int, int, int>(skeleIndex, poseIndex, boneIndex));

			this.BoneName = name;
			this.ParentBoneName = parentName;

			if (arm.Profile.Bones.TryGetValue(name, out var bec) && bec != null)
			{
				this.PluginTransform = bec;
			}
			else
			{
				this.PluginTransform = new BoneTransform();
			}

			this.Parent = null;
			this.Sibling = null;
			this.Children = new();
		}

		public void UpdateModel(BoneTransform newTransform, bool mirror = false, bool propagate = false)
		{
			this.UpdateTransformation(newTransform);

			if (mirror && this.Sibling != null)
			{
				this.Sibling.UpdateModel(newTransform, false, propagate);
			}

			if (propagate)
			{
				foreach (var child in this.Children)
				{
					CascadeTransformation(newTransform,
						this.PluginTransform.Translation,
						this.PluginTransform.Rotation,
						Vector3.One);
				}
			}
		}

		private void UpdateTransformation(BoneTransform newTransform)
		{
			//update the transform locally
			this.PluginTransform.UpdateToMatch(newTransform);

			//these should be connected by reference already, I think?
			//but I suppose it doesn't hurt...?
			if (newTransform.IsEdited())
			{
				this.Armature.Profile.Bones[this.BoneName] = this.PluginTransform;
			}
			else
			{
				this.Armature.Profile.Bones.Remove(this.BoneName);
			}
		}

		private void CascadeTransformation(BoneTransform aggregateTransform, Vector3 pointPos, Vector3 pointRot, Vector3 priorScaling)
		{
			BoneTransform newAggregate = this.PluginTransform.ReorientKinematically(aggregateTransform, pointPos, pointRot, priorScaling);

			foreach (var child in this.Children)
			{
				child.CascadeTransformation(newAggregate,
					pointPos,
					pointRot,
					this.PluginTransform.Scaling);
			}
		}

		/// <summary>
		/// Updates the ingame transformation values associated with this model bone.
		/// </summary>
		public void ApplyModelTransform()
		{
			foreach(var triplex in this.TripleIndices)
			{
				HkaPose* currentPose = triplex.Item2 switch
				{
					0 => this.Armature.InGameSkeleton->PartialSkeletons[triplex.Item1].Pose1,
					1 => this.Armature.InGameSkeleton->PartialSkeletons[triplex.Item1].Pose2,
					2 => this.Armature.InGameSkeleton->PartialSkeletons[triplex.Item1].Pose3,
					3 => this.Armature.InGameSkeleton->PartialSkeletons[triplex.Item1].Pose4,
					_ => null
				};

				if (currentPose == null)
				{
					return;
				}

				Transform t = currentPose->Transforms[triplex.Item3];
				Transform tNew = this.PluginTransform.ModifyExistingTransformation(t);
				currentPose->Transforms[triplex.Item3] = tNew;
			}


			//if (this.GameTransform != null)
			//{
			//	this.GameTransform = this.PluginTransform.ModifyExistingTransformation((Transform)this.GameTransform);
			//}
		}
	}
}
