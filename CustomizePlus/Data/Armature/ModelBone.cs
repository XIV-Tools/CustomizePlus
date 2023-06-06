// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using CustomizePlus.Extensions;
using FFXIVClientStructs.Havok;

//using CustomizePlus.Memory;

namespace CustomizePlus.Data.Armature
{
    /// <summary>
    ///     Represents a single bone of an ingame character's skeleton.
    /// </summary>
    public unsafe class ModelBone
    {
        public enum PoseType
        {
            Local,
            Model,
            Reference /*, World*/
        }

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

        public ModelBone(Armature arm, string name, string parentName, int skeleIndex, int poseIndex, int boneIndex)
        {
            Armature = arm;

            //this.SkeletonIndex = skeleIndex;
            //this.PoseIndex = poseIndex;
            //this.BoneIndex = boneIndex;

            TripleIndices.Add(new Tuple<int, int, int>(skeleIndex, poseIndex, boneIndex));

            BoneName = name;
            ParentBoneName = parentName;

            PluginTransform = arm.Profile.Bones.TryGetValue(name, out var bec) && bec != null
                ? bec
                : new BoneTransform();

            Parent = null;
            Sibling = null;
            Children = new List<ModelBone>();
        }

        public string GetDisplayName()
        {
            var partials = TripleIndices.Select(x => x.Item1).Distinct();
            var poses = TripleIndices.Select(x => x.Item2).Distinct();
            var bones = TripleIndices.Select(x => x.Item3).Distinct();

            var t1 = partials.Count() > 1 ? $"[{partials.Count()}]" : partials.FirstOrDefault().ToString() ?? "?";
            var t2 = poses.Count() > 1 ? $"[{poses.Count()}]" : poses.FirstOrDefault().ToString() ?? "?";
            var t3 = bones.Count() > 1 ? $"[{bones.Count()}]" : bones.FirstOrDefault().ToString() ?? "?";

            return $"{BoneName} @ <{t1}, {t2}, {t3}>";
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
            if (mirror && Sibling != null)
            {
                var mirroredTransform = BoneData.IsIVCSBone(BoneName)
                    ? newTransform.GetSpecialReflection()
                    : newTransform.GetStandardReflection();

                Sibling.UpdateModel(mirroredTransform, false, propagate);
            }

            UpdateTransformation(newTransform);
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

        public bool TryGetGameTransform(int triplexNo, PoseType refFrame, out hkQsTransformf output)
        {
            if (TripleIndices.ElementAtOrDefault(triplexNo) is var triplex && triplex != null)
            {
                var pSkele = Armature.CharacterBaseRef->Skeleton->PartialSkeletons[triplex.Item1];
                var currentPose = pSkele.GetHavokPose(triplex.Item2);

                if (currentPose != null)
                {
                    output = refFrame switch
                    {
                        PoseType.Local => currentPose->LocalPose[triplex.Item3],
                        PoseType.Model => currentPose->ModelPose[triplex.Item3],
                        PoseType.Reference => currentPose->Skeleton->ReferencePose[triplex.Item3],
                        _ => throw new NotImplementedException()
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

            foreach (var x in TripleIndices)
            {
                var currentPose = Armature.CharacterBaseRef->Skeleton->PartialSkeletons[x.Item1].GetHavokPose(x.Item2);

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
            foreach (var triplex in TripleIndices)
            {
                //if (triplex.Item1 == 1 && triplex.Item2 == 3 && triplex.Item3 == 0)
                //{
                //	Dalamud.Logging.PluginLog.LogDebug("Something's fishy");
                //	//what's going on here?
                //}

                var currentPose = Armature.CharacterBaseRef->Skeleton->PartialSkeletons[triplex.Item1]
                    .GetHavokPose(triplex.Item2);

                if (currentPose == null)
                {
                    return;
                }

                if (Armature.GetReferenceSnap())
                {
                    //Referencing from Ktisis, Function 'SetFromLocalPose' @ Line 39 in 'Havok.cs'

                    //hkQsTransformf tRef = currentPose->Skeleton->ReferencePose.Data[triplex.Item3];
                    var tRef = currentPose->LocalPose[triplex.Item3];

                    var tParent = currentPose->ModelPose[currentPose->Skeleton->ParentIndices[triplex.Item3]];
                    var tModel =
                        *currentPose->AccessBoneModelSpace(triplex.Item3, hkaPose.PropagateOrNot.DontPropagate);

                    tModel.Translation =
                    (
                        tParent.Translation.GetAsNumericsVector().RemoveWTerm()
                        + Vector3.Transform(tRef.Translation.GetAsNumericsVector().RemoveWTerm(),
                            tParent.Rotation.ToQuaternion())
                    ).ToHavokTranslation();

                    tModel.Rotation =
                        (tParent.Rotation.ToQuaternion() * tRef.Rotation.ToQuaternion()).ToHavokRotation();
                    tModel.Scale = tRef.Scale;

                    var t = tModel;
                    var tNew = PluginTransform.ModifyExistingTransformation(t);
                    currentPose->ModelPose.Data[triplex.Item3] = tNew;
                }
                else
                {
                    var t = currentPose->ModelPose.Data[triplex.Item3];
                    var tNew = PluginTransform.ModifyExistingTransformation(t);
                    currentPose->ModelPose.Data[triplex.Item3] = tNew;
                }
            }
        }

        public string ToTreeString()
        {
            var sb = new StringBuilder();

            if (Parent == null)
            {
                sb.AppendLine("*");
            }
            else
            {
                sb.AppendLine(Parent.BoneName);
            }

            sb.Append($"└{BoneName}");

            if (Sibling != null)
            {
                sb.Append($" ─── {Sibling.BoneName}");
            }

            sb.AppendLine();

            for (var i = 0; i < Children.Count - 1; ++i)
            {
                sb.AppendLine($"  ├{Children[i].BoneName}");
            }

            if (Children.Any())
            {
                sb.Append($"  └{Children.Last().BoneName}");
            }

            return sb.ToString();
        }

        public IEnumerable<ModelBone> GetLineage(bool includeSelf = true)
        {
            if (includeSelf)
            {
                yield return this;
            }

            var ancestor = Parent;

            while (ancestor != null)
            {
                yield return ancestor;

                ancestor = ancestor.Parent;
            }
        }
    }
}