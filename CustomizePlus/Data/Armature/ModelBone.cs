// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace CustomizePlus.Data.Armature
{
    /// <summary>
    ///     Represents a single bone of an ingame character's skeleton.
    /// </summary>
    public unsafe class ModelBone
    {
        public readonly Armature Armature;

        public readonly string BoneName;
        public readonly string? ParentBoneName;

        //public readonly int SkeletonIndex;
        //public readonly int PoseIndex;
        //public readonly int BoneIndex;

        public readonly List<Tuple<int, int, int>> TripleIndices = new();
        public List<ModelBone> Children = new();

        public ModelBone? Parent;

        public BoneTransform PluginTransform;
        public ModelBone? Sibling;

        public ModelBone(Armature arm, string name, string? parentName, int skeleIndex, int poseIndex, int boneIndex)
        {
            Armature = arm;

            //this.SkeletonIndex = skeleIndex;
            //this.PoseIndex = poseIndex;
            //this.BoneIndex = boneIndex;

            TripleIndices.Add(new Tuple<int, int, int>(skeleIndex, poseIndex, boneIndex));

            BoneName = name;
            ParentBoneName = parentName;

            if (arm.Profile.Bones.TryGetValue(name, out var bec) && bec != null)
            {
                PluginTransform = bec;
            }
            else
            {
                PluginTransform = new BoneTransform();
            }

            Parent = null;
            Sibling = null;
            Children = new List<ModelBone>();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (Parent != null)
            {
                sb.Append($"[{Parent.BoneName}]-> ");
            }

            sb.Append(BoneName);
            if (Sibling != null)
            {
                sb.Append($" <->[{Sibling.BoneName}]");
            }

            if (Children.Any())
            {
                sb.Append($" ({Children.Count} children)");
            }
            else
            {
                sb.Append(" (no children)");
            }

            return sb.ToString();
        }

        public void UpdateModel(BoneTransform newTransform, bool mirror = false, bool propagate = false)
        {
            UpdateTransformation(newTransform);

            if (mirror && Sibling != null)
            {
                Sibling.UpdateModel(newTransform, false, propagate);
            }

            if (propagate)
            {
                foreach (var child in Children)
                {
                    CascadeTransformation(newTransform,
                        PluginTransform.Translation,
                        PluginTransform.Rotation,
                        Vector3.One);
                }
            }
        }

        private void UpdateTransformation(BoneTransform newTransform)
        {
            //update the transform locally
            PluginTransform.UpdateToMatch(newTransform);

            //these should be connected by reference already, I think?
            //but I suppose it doesn't hurt...?
            if (newTransform.IsEdited())
            {
                Armature.Profile.Bones[BoneName] = PluginTransform;
            }
            else
            {
                Armature.Profile.Bones.Remove(BoneName);
            }
        }

        private void CascadeTransformation(BoneTransform aggregateTransform, Vector3 pointPos, Vector3 pointRot,
            Vector3 priorScaling)
        {
            var newAggregate =
                PluginTransform.ReorientKinematically(aggregateTransform, pointPos, pointRot, priorScaling);

            foreach (var child in Children)
            {
                child.CascadeTransformation(newAggregate,
                    pointPos,
                    pointRot,
                    PluginTransform.Scaling);
            }
        }

        /// <summary>
        ///     Updates the ingame transformation values associated with this model bone.
        /// </summary>
        public void ApplyModelTransform()
        {
            foreach (var triplex in TripleIndices)
            {
                var currentPose = triplex.Item2 switch
                {
                    0 => Armature.InGameSkeleton->PartialSkeletons[triplex.Item1].Pose1,
                    1 => Armature.InGameSkeleton->PartialSkeletons[triplex.Item1].Pose2,
                    2 => Armature.InGameSkeleton->PartialSkeletons[triplex.Item1].Pose3,
                    3 => Armature.InGameSkeleton->PartialSkeletons[triplex.Item1].Pose4,
                    _ => null
                };

                if (currentPose == null)
                {
                    return;
                }

                var t = currentPose->Transforms[triplex.Item3];
                var tNew = PluginTransform.ModifyExistingTransformation(t);
                currentPose->Transforms[triplex.Item3] = tNew;
            }


            //if (this.GameTransform != null)
            //{
            //	this.GameTransform = this.PluginTransform.ModifyExistingTransformation((Transform)this.GameTransform);
            //}
        }
    }
}