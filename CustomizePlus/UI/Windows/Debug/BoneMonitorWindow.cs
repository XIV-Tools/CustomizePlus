// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CustomizePlus.Data;
using CustomizePlus.Data.Armature;
using CustomizePlus.Data.Profile;
using CustomizePlus.Extensions;
using CustomizePlus.Helpers;
using CustomizePlus.UI.Dialogs;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using ImGuiNET;

namespace CustomizePlus.UI.Windows.Debug
{
    internal class BoneMonitorWindow : WindowBase
    {
        private readonly Dictionary<BoneData.BoneFamily, bool> _groupExpandedState = new();
        private readonly bool _modelFrozen = false;
        private ModelBone.PoseType _targetPose;
        private bool _aggregateDeforms;

        private BoneAttribute _targetAttribute;


        private CharacterProfile? _targetProfile;
        protected override string Title => $"Bone Monitor: {_targetProfile.ToDebugString()}";
        protected override string DrawTitle => $"{Title}###bone_monitor_window{Index}";

        protected override ImGuiWindowFlags WindowFlags =>
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        private Armature? TargetSkeleton => _targetProfile.Armature;

        public static void Show(CharacterProfile prof)
        {
            var editWnd = Plugin.InterfaceManager.Show<BoneMonitorWindow>();

            editWnd._targetProfile = prof;

            Plugin.ArmatureManager.RenderCharacterProfiles(prof);

            editWnd.ConfirmSkeletonConnection();
        }

        protected override void DrawContents()
        {
            ImGui.TextUnformatted($"Character Name: {_targetProfile.CharacterName}");

            ImGui.SameLine();
            ImGui.TextUnformatted("|");

            ImGui.SameLine();
            ImGui.TextUnformatted($"Profile Name: {_targetProfile.ProfileName}");

            ImGui.SameLine();
            ImGui.TextUnformatted("|");

            ImGui.SameLine();
            var tempEnabled = _targetProfile.Enabled;
            if (CtrlHelper.Checkbox("Live", ref tempEnabled))
            {
                _targetProfile.Enabled = tempEnabled;
                ConfirmSkeletonConnection();
            }

            CtrlHelper.AddHoverText("Hook the editor into the game to edit and preview live bone data");

            ImGui.Separator();

            if (ImGui.RadioButton("Position", _targetAttribute == BoneAttribute.Position))
                _targetAttribute = BoneAttribute.Position;

            ImGui.SameLine();
            if (ImGui.RadioButton("Rotation", _targetAttribute == BoneAttribute.Rotation))
                _targetAttribute = BoneAttribute.Rotation;

            ImGui.SameLine();
            if (ImGui.RadioButton("Scale", _targetAttribute == BoneAttribute.Scale))
                _targetAttribute = BoneAttribute.Scale;

            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();

            ImGui.TextUnformatted("|");

            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();

            if (ImGui.RadioButton("Local", _targetPose == ModelBone.PoseType.Local))
                _targetPose = ModelBone.PoseType.Local;

            ImGui.SameLine();
            if (ImGui.RadioButton("Model", _targetPose == ModelBone.PoseType.Model))
                _targetPose = ModelBone.PoseType.Model;

            ImGui.SameLine();
            if (ImGui.RadioButton("Reference", _targetPose == ModelBone.PoseType.Reference))
                _targetPose = ModelBone.PoseType.Reference;

            //-------------

            CtrlHelper.Checkbox("Aggregate Deforms", ref _aggregateDeforms);

            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();

            ImGui.TextUnformatted("|");

            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();

            if (!_targetProfile.Enabled)
                ImGui.BeginDisabled();

            if (ImGui.Button("Reload Bone Data"))
                TargetSkeleton.RebuildSkeleton();

            if (!_targetProfile.Enabled)
                ImGui.EndDisabled();

            ImGui.Separator();

            if (ImGui.BeginTable("Bones", 10,
                    ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollY,
                    new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 56)))
            {
                ImGui.TableSetupColumn("Opt", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthFixed);

                ImGui.TableSetupColumn("\tX", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("\tY", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("\tZ", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("\tW", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableSetupColumn("pSke", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Pose", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Bone", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthFixed);

                ImGui.TableSetupColumn("Bone Code",
                    ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableSetupColumn("Bone Name",
                    ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableSetupScrollFreeze(0, 1);

                ImGui.TableHeadersRow();

                if (TargetSkeleton == null)
                    return;

                var relevantBoneNames = _targetProfile.Enabled
                    ? TargetSkeleton.GetExtantBoneNames()
                    : _targetProfile.Bones.Keys;

                //TODO this shouldn't be necessary, the armature shouldn't cease to exist when the profile is disabled?
                //if it must, then this section should conditionally reroute to show the saved values from within the profile
                //instead of showing nothing
                if (relevantBoneNames.Any(x => !TargetSkeleton.Bones.ContainsKey(x)))
                    return;

                var groupedBones =
                    relevantBoneNames.GroupBy(x => BoneData.GetBoneFamily(TargetSkeleton.Bones[x].BoneName));

                foreach (var boneGroup in groupedBones.OrderBy(x => (int)x.Key))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);

                    //create a dropdown entry for the family if one doesn't already exist
                    //mind that it'll only be rendered if bones exist to fill it
                    if (!_groupExpandedState.TryGetValue(boneGroup.Key, out var expanded))
                    {
                        _groupExpandedState[boneGroup.Key] = false;
                        expanded = false;
                    }

                    CtrlHelper.ArrowToggle($"##{boneGroup.Key}", ref expanded);
                    ImGui.SameLine();
                    CtrlHelper.StaticLabel(boneGroup.Key.ToString());
                    if (BoneData.DisplayableFamilies.TryGetValue(boneGroup.Key, out var tip) && tip != null)
                        CtrlHelper.AddHoverText(tip);

                    if (expanded)
                    {
                        foreach (var codename in boneGroup.OrderBy(x =>
                                     BoneData.GetBoneIndex(TargetSkeleton.Bones[x].BoneName)))
                        {
                            for (var tri = 0; tri < TargetSkeleton.Bones[codename].TripleIndices.Count; ++tri)
                            {
                                TransformationInfo(codename, tri);
                            }
                        }
                    }

                    _groupExpandedState[boneGroup.Key] = expanded;
                }

                ImGui.EndTable();
            }

            ImGui.Separator();

            //----------------------------------

            if (ImGui.Button("Cancel"))
                Close();
        }

        public void ConfirmSkeletonConnection()
        {
            if (!TargetSkeleton.TryLinkSkeleton())
            {
                _targetProfile.Enabled = false;
                DisplayNoLinkMsg();
            }
        }

        public void DisplayNoLinkMsg()
        {
            var msg = $"The editor can't find {_targetProfile.CharacterName} or their bone data in the game's memory.";
            MessageDialog.Show(msg);
        }

        #region ImGui helper functions

        private bool MysteryButton(string codename, ref Vector4 value)
        {
            var output = ImGuiComponents.IconButton(codename, FontAwesomeIcon.ChartLine);
            CtrlHelper.AddHoverText("This will do... something... at some point...?");

            return output;
        }

        //private bool ResetButton(string codename, ref Vector3 value)
        //{
        //	bool output = ImGuiComponents.IconButton(this.TargetSkeleton.Bones[codename].BoneName, FontAwesomeIcon.Recycle);
        //	CtrlHelper.AddHoverText($"Reset values");

        //	if (output)
        //	{
        //		value = this.targetAttribute switch
        //		{
        //			BoneAttribute.Scale => Vector3.One,
        //			_ => Vector3.Zero
        //		};
        //	}

        //	return output;
        //}

        private void TransformationInfo(string codename, int triplexIndex)
        {
            if (_targetProfile.Enabled
                && TargetSkeleton.Bones.TryGetValue(codename, out var mb)
                && mb != null
                && mb.TripleIndices.ElementAtOrDefault(triplexIndex) is var triplex
                && triplex != null
                && mb.TryGetGameTransform(triplexIndex, _targetPose, out var deform))
            {
                var displayName = mb.GetDisplayName();

                var rowVector = deform.GetAttribute(_targetAttribute).GetAsNumericsVector();

                if (_aggregateDeforms)
                {
                    var vectors = mb.GetLineage()
                        .Where(x => x.GetGameTransforms().Count() > triplexIndex)
                        .Select(x => x
                            .GetGameTransforms()[triplexIndex]
                            .GetAttribute(_targetAttribute)
                            .GetAsNumericsVector());

                    if (vectors.Any())
                    {
                        rowVector = vectors.Aggregate((x, y) =>
                        {
                            return _targetAttribute switch
                            {
                                BoneAttribute.Position => x + y,
                                BoneAttribute.Rotation => Quaternion.Multiply(x.ToQuaternion(), y.ToQuaternion())
                                    .GetAsNumericsVector(),
                                BoneAttribute.Scale => x * y,
                                _ => throw new NotImplementedException()
                            };
                        });
                    }
                }

                ImGui.PushID(codename.GetHashCode() + triplexIndex);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                MysteryButton(codename, ref rowVector);

                //----------------------------------
                ImGui.PushFont(UiBuilder.MonoFont);

                ImGui.TableNextColumn();
                ImGui.TextDisabled($"{rowVector.X,8:0.00000}");

                ImGui.TableNextColumn();
                ImGui.TextDisabled($"{rowVector.Y,8:0.00000}");

                ImGui.TableNextColumn();
                ImGui.TextDisabled($"{rowVector.Z,8:0.00000}");

                ImGui.TableNextColumn();
                ImGui.TextDisabled($"{rowVector.W,8:0.00000}");

                //----------------------------------

                ImGui.TableNextColumn();
                ImGui.TextDisabled($"{triplex.Item1,3}");

                ImGui.TableNextColumn();
                ImGui.TextDisabled($"{triplex.Item2,3}");

                ImGui.TableNextColumn();
                ImGui.TextDisabled($"{triplex.Item3,3}");

                //----------------------------------

                ImGui.TableNextColumn();
                CtrlHelper.StaticLabel(mb.BoneName, CtrlHelper.TextAlignment.Left, displayName);

                ImGui.TableNextColumn();
                CtrlHelper.StaticLabel(BoneData.GetBoneDisplayName(mb.BoneName));

                ImGui.PopFont();

                ImGui.PopID();
            }
        }

        #endregion
    }
}