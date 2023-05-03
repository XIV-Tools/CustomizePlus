// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CustomizePlus.Memory;

using System.Numerics;

using CustomizePlus.Extensions;

namespace CustomizePlus.Data.Armature
{
	/// <summary>
	/// Represents a single bone of an ingame character's skeleton.
	/// </summary>
	public unsafe class ModelBone
	{
		public readonly Armature Armature;

		public readonly HkaPose* PartialPose;
		public readonly int BoneIndex;
		public readonly string BoneName;

		public Transform GameTransform;
		public BoneTransform PluginTransform;

		public ModelBone? Parent;
		public ModelBone? Sibling;
		public ModelBone[] Children;

		public ModelBone(Armature arm, string name, int index, HkaPose* pose)
		{
			this.Armature = arm;

			this.PartialPose = pose;
			this.BoneIndex = index;
			this.BoneName = name;

			this.GameTransform = pose->Transforms[index];

			if (arm.Profile.Bones.TryGetValue(name, out var bec) && bec != null)
			{
				this.PluginTransform = bec;
			}
			else
			{
				arm.Profile.Bones[name] = new BoneTransform();
				this.PluginTransform = arm.Profile.Bones[name];
			}

			this.Parent = null;
			this.Sibling = null;
			this.Children = Array.Empty<ModelBone>();
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
				foreach(var child in this.Children)
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
			this.PluginTransform.UpdateToMatch(newTransform);

			this.Armature.Profile.Bones[this.BoneName] = this.PluginTransform;
		}

		private void CascadeTransformation(BoneTransform aggregateTransform, Vector3 pointPos, Quaternion pointRot, Vector3 priorScaling)
		{
			BoneTransform newAggregate = this.PluginTransform.ReorientKinematically(aggregateTransform, pointPos, pointRot, priorScaling);

			foreach (var child in this.Children)
			{
				CascadeTransformation(newAggregate,
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
			this.PluginTransform.ModifyExistingTransformation(
				ref this.GameTransform.Translation,
				ref this.GameTransform.Rotation,
				ref this.GameTransform.Scale);
		}
	}
}
