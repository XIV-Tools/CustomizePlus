// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using CustomizePlus.Extensions;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;
//using CustomizePlus.Memory;

namespace CustomizePlus.Data.Armature
{
	/// <summary>
	/// Represents a single bone of an ingame character's skeleton.
	/// </summary>
	public unsafe class ModelBone
	{
		public readonly Armature Armature;

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

		public string GetDisplayName()
		{
			var partials = this.TripleIndices.Select(x => x.Item1).Distinct();
			var poses = this.TripleIndices.Select(x => x.Item2).Distinct();
			var bones = this.TripleIndices.Select(x => x.Item3).Distinct();

			string t1 = partials.Count() > 1 ? $"[{partials.Count()}]" : (partials.FirstOrDefault().ToString() ?? "?");
			string t2 = poses.Count() > 1 ? $"[{poses.Count()}]" : (poses.FirstOrDefault().ToString() ?? "?");
			string t3 = bones.Count() > 1 ? $"[{bones.Count()}]" : (bones.FirstOrDefault().ToString() ?? "?");

			return $"{this.BoneName} @ <{t1}, {t2}, {t3}>";
		}

		public ModelBone(Armature arm, string name, string parentName, int skeleIndex, int poseIndex, int boneIndex)
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

			if (mirror && this.Sibling != null)
			{
				var mirroredTransform = BoneData.IsIVCSBone(this.BoneName)
					? newTransform.GetSpecialReflection()
					: newTransform.GetStandardReflection();

				this.Sibling.UpdateModel(mirroredTransform, false, propagate);
			}

			if (propagate)
			{
				foreach (var child in this.Children)
				{
					child.CascadeTransformation(newTransform, this.PluginTransform.Translation);
				}
			}

			this.UpdateTransformation(newTransform);
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

		private void CascadeTransformation(BoneTransform delta, Vector3 pointPos)
		{
			this.PluginTransform.ReorientKinematically(delta, pointPos);

			foreach (var child in this.Children)
			{
				child.CascadeTransformation(delta, pointPos);
			}
		}

		public enum PoseType { Local, Model, Reference /*, World*/}

		public bool TryGetGameTransform(int triplexNo, PoseType refFrame, out hkQsTransformf output)
		{
			if (this.TripleIndices.ElementAtOrDefault(triplexNo) is var triplex && triplex != null)
			{
				PartialSkeleton pSkele = this.Armature.Skeleton->PartialSkeletons[triplex.Item1];
				hkaPose* currentPose = pSkele.GetHavokPose(triplex.Item2);

				if (currentPose != null)
				{
					output = refFrame switch
					{
						PoseType.Local => currentPose->LocalPose[triplex.Item3],
						PoseType.Model => currentPose->ModelPose[triplex.Item3],
						PoseType.Reference => currentPose->Skeleton->ReferencePose[triplex.Item3],
						_ => throw new NotImplementedException(),
					};

					return true;
				}
			}

			output = Constants.NullTransform;
			return false;
		}

		public hkQsTransformf[] GetGameTransforms()
		{
			List<hkQsTransformf> output = new();

			foreach (var x in this.TripleIndices)
			{
				hkaPose* currentPose = x.Item2 switch
				{
					0 => this.Armature.Skeleton->PartialSkeletons[x.Item1].GetHavokPose(0),
					1 => this.Armature.Skeleton->PartialSkeletons[x.Item1].GetHavokPose(1),
					2 => this.Armature.Skeleton->PartialSkeletons[x.Item1].GetHavokPose(2),
					3 => this.Armature.Skeleton->PartialSkeletons[x.Item1].GetHavokPose(3),
					_ => null
				};

				if (currentPose != null && currentPose->LocalPose[x.Item3] is hkQsTransformf pose)
				{
					output.Add(pose);
				}
			}

			return output.ToArray();
		}

		/// <summary>
		/// Updates the ingame transformation values associated with this model bone.
		/// </summary>
		public void ApplyModelTransform()
		{
			foreach (var triplex in this.TripleIndices)
			{
				//if (triplex.Item1 == 1 && triplex.Item2 == 3 && triplex.Item3 == 0)
				//{
				//	Dalamud.Logging.PluginLog.LogDebug("Something's fishy");
				//	//what's going on here?
				//}

				hkaPose* currentPose = this.Armature.Skeleton->PartialSkeletons[triplex.Item1].GetHavokPose(triplex.Item2);

				if (currentPose == null)
				{
					return;
				}

				if (Armature.GetReferenceSnap())
				{
					//Referencing from Ktisis, Function 'SetFromLocalPose' @ Line 39 in 'Havok.cs'

					//hkQsTransformf tRef = currentPose->Skeleton->ReferencePose.Data[triplex.Item3];
					hkQsTransformf tRef = currentPose->LocalPose[triplex.Item3];

					hkQsTransformf tParent = currentPose->ModelPose[currentPose->Skeleton->ParentIndices[triplex.Item3]];
					hkQsTransformf tModel = * currentPose->AccessBoneModelSpace(triplex.Item3, hkaPose.PropagateOrNot.DontPropagate);

					tModel.Translation =
						(
							tParent.Translation.GetAsNumericsVector().RemoveWTerm()
							+ Vector3.Transform(tRef.Translation.GetAsNumericsVector().RemoveWTerm(), tParent.Rotation.ToQuaternion())
						).ToHavokTranslation();

					tModel.Rotation = (tParent.Rotation.ToQuaternion() * tRef.Rotation.ToQuaternion()).ToHavokRotation();
					tModel.Scale = tRef.Scale;

					hkQsTransformf t = tModel;
					hkQsTransformf tNew = this.PluginTransform.ModifyExistingTransformation(t);
					currentPose->ModelPose.Data[triplex.Item3] = tNew;
				}
				else
				{
					hkQsTransformf t = currentPose->ModelPose.Data[triplex.Item3];
					hkQsTransformf tNew = this.PluginTransform.ModifyExistingTransformation(t);
					currentPose->ModelPose.Data[triplex.Item3] = tNew;
				}
			}


			//hkQsTransformf[] deforms = this.GetGameTransforms();

			//for (int i = 0; i < deforms.Length; ++i)
			//{
			//	hkQsTransformf tNew = this.PluginTransform.ModifyExistingTransformation(deforms[i]);
			//	this.SetGameTransforms(tNew);
			//}
		}

		public string ToTreeString()
		{
			StringBuilder sb = new StringBuilder();

			if (this.Parent == null)
			{
				sb.AppendLine("*");
			}
			else
			{
				sb.AppendLine(this.Parent.BoneName);
			}

			sb.Append($"└{this.BoneName}");

			if (this.Sibling != null)
			{
				sb.Append($" ─── {this.Sibling.BoneName}");
			}
			sb.AppendLine();

			for (int i = 0; i < this.Children.Count - 1; ++i)
			{
				sb.AppendLine($"  ├{this.Children[i].BoneName}");
			}

			if (this.Children.Any())
			{
				sb.Append($"  └{this.Children.Last().BoneName}");
			}

			return sb.ToString();
		}

		public IEnumerable<ModelBone> GetLineage(bool includeSelf = true)
		{
			if (includeSelf)
			{
				yield return this;
			}

			ModelBone? ancestor = this.Parent;

			while (ancestor != null)
			{
				yield return ancestor;

				ancestor = ancestor.Parent;
			}
		}
	}
}
