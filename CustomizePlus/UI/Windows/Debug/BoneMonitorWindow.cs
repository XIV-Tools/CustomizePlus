// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Common.Math;
using CustomizePlus.Data;
using CustomizePlus.Data.Armature;
using CustomizePlus.Data.Profile;
using CustomizePlus.Extensions;
using CustomizePlus.Helpers;
using CustomizePlus.UI.Dialogs;
using Dalamud.Interface;
using Dalamud.Interface.Components;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using ImGuiNET;

namespace CustomizePlus.UI.Windows.Debug
{
    internal unsafe class BoneMonitorWindow : WindowBase
    {
        private readonly Dictionary<BoneData.BoneFamily, bool> _groupExpandedState = new();
        private PosingSpace _targetPose;

        private BoneAttribute _targetAttribute;


        private CharacterProfile _targetProfile;
        protected override string Title => $"Bone Monitor: {_targetProfile.ToDebugString()}";
        protected override string DrawTitle => $"{Title}###bone_monitor_window{Index}";

        protected override bool LockCloseButton => false;
        protected override bool SingleInstance => true;
        protected override ImGuiWindowFlags WindowFlags =>
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        private Armature _targetArmature;
        //private CharacterBase* _targetObject;

        public static void Show(CharacterProfile prof)
        {
            var editWnd = Plugin.InterfaceManager.Show<BoneMonitorWindow>();

            editWnd._targetProfile = prof;

            //Plugin.ArmatureManager.RenderCharacterProfiles(prof);
            editWnd._targetArmature = prof.Armature;
        }

        protected override void DrawContents()
        {
            if (!GameDataHelper.TryLookupCharacterBase(_targetProfile.CharacterName, out CharacterBase* targetObject)
                && _targetProfile.Enabled)
            {
                _targetProfile.Enabled = false;
                DisplayNoLinkMsg();
            }

            if (ImGui.BeginTable("Header", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoClip | ImGuiTableFlags.BordersInnerV))
            {
                ImGui.TableSetupColumn("AttributeType", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("ReferenceFrame", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Space", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Reload", ImGuiTableColumnFlags.WidthFixed);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                if (ImGui.RadioButton("Position", _targetAttribute == BoneAttribute.Position))
                    _targetAttribute = BoneAttribute.Position;

                ImGui.SameLine();
                if (ImGui.RadioButton("Rotation", _targetAttribute == BoneAttribute.Rotation))
                    _targetAttribute = BoneAttribute.Rotation;

                ImGui.SameLine();
                if (ImGui.RadioButton("Scale", _targetAttribute == BoneAttribute.Scale))
                    _targetAttribute = BoneAttribute.Scale;

                ImGui.TableNextColumn();

                if (ImGui.RadioButton("Local", _targetPose == PosingSpace.Self))
                    _targetPose = PosingSpace.Self;

                ImGui.SameLine();
                if (ImGui.RadioButton("Model", _targetPose == PosingSpace.Parent))
                    _targetPose = PosingSpace.Parent;

                ImGui.TableNextColumn();
                ImGui.TableNextColumn();

                if (!_targetProfile.Enabled) ImGui.BeginDisabled();

                if (ImGui.Button("Reload Bone Data"))
                    _targetArmature.RebuildSkeleton(targetObject);

                if (!_targetProfile.Enabled) ImGui.EndDisabled();

                ImGui.SameLine();
                var tempEnabled = _targetProfile.Enabled;
                if (CtrlHelper.Checkbox("Live", ref tempEnabled))
                {
                    _targetProfile.Enabled = tempEnabled;
                }

                ImGui.EndTable();
            }

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
                ImGui.TableSetupColumn("Bone", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthFixed);

                ImGui.TableSetupColumn("Bone Code",
                    ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableSetupColumn("Bone Name",
                    ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableSetupColumn("Parent Bone",
                    ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableSetupScrollFreeze(0, 1);

                ImGui.TableHeadersRow();

                if (_targetArmature != null && targetObject != null)
                {
                    IEnumerable<ModelBone> relevantModelBones = _targetArmature.GetLocalAndDownstreamBones();

                    var groupedBones = relevantModelBones.GroupBy(x => x.FamilyName);

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
                            foreach (ModelBone mb in boneGroup
                                .OrderBy(x => x.PartialSkeletonIndex)
                                .ThenBy(x => x.BoneIndex))
                            {
                                RenderTransformationInfo(mb, targetObject);
                            }
                        }

                        _groupExpandedState[boneGroup.Key] = expanded;
                    }
                }

                ImGui.EndTable();
            }

            ImGui.Separator();

            //----------------------------------
            if (ImGui.BeginTable("Footer", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoClip))
            {
                ImGui.TableSetupColumn("HeightInfo", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("VarSpace", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Close", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Bumper", ImGuiTableColumnFlags.WidthFixed);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                if (targetObject != null)
                {
                    CtrlHelper.StaticLabel($"Character Height: {targetObject->Height()->ToString()}");
                }

                ImGui.TableNextColumn();
                ImGui.TableNextColumn();

                if (ImGui.Button("Close"))
                    Close();

                ImGui.TableNextColumn();

                ImGui.Dummy(new(CtrlHelper.IconButtonWidth, 0));

                ImGui.EndTable();
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

        private void RenderTransformationInfo(ModelBone bone, CharacterBase* cBase)
        {
            if (bone.GetGameTransform(cBase, _targetPose == PosingSpace.Parent) is FFXIVClientStructs.Havok.hkQsTransformf deform)
            {
                var displayName = bone.ToString();

                Vector4 rowVector = deform.GetAttribute(_targetAttribute);

                ImGui.PushID(bone.BoneName.GetHashCode());

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                //MysteryButton(bone.BoneName, ref rowVector);

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
                ImGui.TextDisabled($"{bone.PartialSkeletonIndex,3}");

                ImGui.TableNextColumn();
                ImGui.TextDisabled($"{bone.BoneIndex,3}");

                //----------------------------------

                ImGui.TableNextColumn();
                CtrlHelper.StaticLabel(bone.BoneName, CtrlHelper.TextAlignment.Left, displayName);

                ImGui.TableNextColumn();
                CtrlHelper.StaticLabel(BoneData.GetBoneDisplayName(bone.BoneName));

                ImGui.TableNextColumn();
                CtrlHelper.StaticLabel(BoneData.GetBoneDisplayName(bone.ParentBone?.BoneName ?? "N/A"),
                    CtrlHelper.TextAlignment.Left,
                    bone.ParentBone?.ToString() ?? "N/A");

                ImGui.PopFont();

                ImGui.PopID();
            }
        }

        #endregion
    }
}