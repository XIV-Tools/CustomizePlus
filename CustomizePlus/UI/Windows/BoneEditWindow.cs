// © Customize+.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Common.Math;
using CustomizePlus.Data;
using CustomizePlus.Data.Armature;
using CustomizePlus.Data.Profile;
using CustomizePlus.Helpers;
using CustomizePlus.UI.Dialogs;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using ImGuiNET;

namespace CustomizePlus.UI.Windows
{
    /// <summary>
    /// Menu for editing skeleton information of an ingame model.
    /// </summary>
    public class BoneEditWindow : WindowBase
    {
        private bool _dirty;
        private int _precision = 3;

        /// <summary>
        /// User-selected settings for this instance of the bone edit window.
        /// </summary>
        private EditorSessionSettings _settings;
        /// <inheritdoc/>
        protected override string Title => $"Edit Profile: {_settings.ProfileInProgress.ProfileName}";
        /// <inheritdoc/>
        protected override bool SingleInstance => true;

        /// <inheritdoc/>
        protected override string DrawTitle => $"{Title}###customize_plus_scale_edit_window{Index}";

        /// <inheritdoc/>
        protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoScrollbar |
                                                           (_dirty
                                                               ? ImGuiWindowFlags.UnsavedDocument
                                                               : ImGuiWindowFlags.None);

        /// <inheritdoc/>
        protected override bool LockCloseButton => _dirty;

        /// <summary>
        /// Show the editing menu for the given character profile.
        /// </summary>
        public static void Show(CharacterProfile prof)
        {
            var editWnd = Plugin.InterfaceManager.Show<BoneEditWindow>();

            editWnd._settings = new EditorSessionSettings(prof);
        }

        /// <inheritdoc/>
        protected unsafe override void DrawContents()
        {
            CharacterBase* targetObject = _settings.ArmatureInProgress.TryLinkSkeleton();

            if (targetObject == null && _settings.ShowLiveBones)
            {
                _settings.ToggleLiveBones(false);
                DisplayNoLinkMsg();
            }

            if (_settings.ProfileRenamed())
            {
                _dirty = true;
            }

            if (ImGui.BeginTable("##Save/Close", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoClip))
            {
                ImGui.TableSetupColumn("##CharName", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##ProfName", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##Precision", ImGuiTableColumnFlags.WidthFixed);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                CtrlHelper.StaticLabel("Character Name", CtrlHelper.TextAlignment.Center);
                CtrlHelper.TextPropertyBox("##Character Name",
                    () => _settings.ProfileInProgress.CharacterName,
                    (s) => _settings.ProfileInProgress.CharacterName = s);

                ImGui.TableNextColumn();

                CtrlHelper.StaticLabel("Profile Name", CtrlHelper.TextAlignment.Center);
                CtrlHelper.TextPropertyBox("##Profile Name",
                    () => _settings.ProfileInProgress.ProfileName,
                    (s) => _settings.ProfileInProgress.ProfileName = s);

                ImGui.TableNextColumn();

                CtrlHelper.StaticLabel("Decimal Precision", CtrlHelper.TextAlignment.Left);
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.SliderInt("##Precision", ref _precision, 0, 6, $"{_precision} Place{(_precision == 1 ? "" : "s")}");
                CtrlHelper.AddHoverText("Level of precision to display while editing values");

                //I don't know why it does what it does
                //but if this column index reset is removed it messes up all the separator lines in the window?
                ImGui.TableSetColumnIndex(0);
                ImGui.EndTable();
            }

            ImGui.Separator();

            int numColumns = Plugin.ConfigurationManager.Configuration.DebuggingModeEnabled ? 3 : 2;

            if (ImGui.BeginTable("Checkboxes", numColumns, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoClip))
            {
                ImGui.TableSetupColumn("CheckEnabled", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("CheckLive", ImGuiTableColumnFlags.WidthStretch);
                if (Plugin.ConfigurationManager.Configuration.DebuggingModeEnabled)
                    ImGui.TableSetupColumn("CheckAPose", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("CheckMirrored", ImGuiTableColumnFlags.WidthStretch);
                if (Plugin.ConfigurationManager.Configuration.DebuggingModeEnabled)
                    ImGui.TableSetupColumn("CheckParented", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                if (CtrlHelper.Checkbox("Show Live Bones", ref _settings.ShowLiveBones))
                {
                    _settings.ToggleLiveBones(_settings.ShowLiveBones);
                    ConfirmSkeletonConnection();
                }
                CtrlHelper.AddHoverText($"If selected, present for editing all bones found in the game data,\nelse show only bones for which the profile already contains edits.");

                //if (Plugin.ConfigurationManager.Configuration.DebuggingModeEnabled)
                //{
                //    ImGui.TableNextColumn();

                //    var tempRefSnap = _targetArmature?.SnapToReferencePose ?? false;
                //    if (_targetArmature != null && CtrlHelper.Checkbox("A-Pose", ref tempRefSnap))
                //    {
                //        ConfirmSkeletonConnection();
                //        _targetArmature.SnapToReferencePose = tempRefSnap;
                //    }
                //    CtrlHelper.AddHoverText($"D: Force character into their default reference pose");
                //}

                ImGui.TableNextColumn();

                if (!_settings.ShowLiveBones) ImGui.BeginDisabled();

                if (CtrlHelper.Checkbox("Mirror Mode", ref _settings.MirrorModeEnabled))
                {
                    ConfirmSkeletonConnection();
                }
                CtrlHelper.AddHoverText($"Bone changes will be reflected from left to right and vice versa");

                if (Plugin.ConfigurationManager.Configuration.DebuggingModeEnabled)
                {
                    ImGui.TableNextColumn();

                    if (CtrlHelper.Checkbox("Parenting Mode", ref _settings.ParentingEnabled))
                    {
                        ConfirmSkeletonConnection();
                    }
                    CtrlHelper.AddHoverText($"D: Changes will propagate \"outward\" from edited bones");
                }

                if (!_settings.ShowLiveBones) ImGui.EndDisabled();
                ImGui.EndTable();
            }

            ImGui.Separator();

            if (ImGui.BeginTable("Misc", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoClip))
            {
                ImGui.TableSetupColumn("Attributes", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Space", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("ReloadButton", ImGuiTableColumnFlags.WidthFixed);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                if (GameStateHelper.GameInPosingMode()) ImGui.BeginDisabled();
                if (ImGui.RadioButton("Position", _settings.EditingAttribute == BoneAttribute.Position))
                {
                    _settings.EditingAttribute = BoneAttribute.Position;
                }
                CtrlHelper.AddHoverText($"May have unintended effects. Edit at your own risk!");

                ImGui.SameLine();

                if (ImGui.RadioButton("Rotation", _settings.EditingAttribute == BoneAttribute.Rotation))
                {
                    _settings.EditingAttribute = BoneAttribute.Rotation;
                }
                CtrlHelper.AddHoverText($"May have unintended effects. Edit at your own risk!");
                if (GameStateHelper.GameInPosingMode()) ImGui.EndDisabled();

                ImGui.SameLine();
                if (ImGui.RadioButton("Scale", _settings.EditingAttribute == BoneAttribute.Scale))
                {
                    _settings.EditingAttribute = BoneAttribute.Scale;
                }

                ImGui.TableNextColumn();
                ImGui.TableNextColumn();

                if (!_settings.ShowLiveBones || targetObject == null) ImGui.BeginDisabled();
                if (ImGui.Button("Reload Bone Data"))
                {
                    _settings.ArmatureInProgress.RebuildSkeleton(targetObject);
                }
                CtrlHelper.AddHoverText("Refresh the skeleton data obtained from in-game");
                if (!_settings.ShowLiveBones || targetObject == null) ImGui.EndDisabled();

                ImGui.EndTable();
            }

            //if (!Settings.EditStack.UndoPossible()) ImGui.BeginDisabled();
            //if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
            //{
            //    Settings.EditStack.Undo();
            //}
            //CtrlHelper.AddHoverText("Undo last edit");
            //if (!Settings.EditStack.UndoPossible()) ImGui.EndDisabled();

            //ImGui.SameLine();

            //if (!Settings.EditStack.RedoPossible()) ImGui.BeginDisabled();
            //if (ImGuiComponents.IconButton(FontAwesomeIcon.RedoAlt))
            //{
            //    Settings.EditStack.Redo();
            //}
            //CtrlHelper.AddHoverText("Redo next edit");
            //if (!Settings.EditStack.RedoPossible()) ImGui.EndDisabled();


            ImGui.Separator();

            //CompleteBoneEditor("n_root");

            var col1Label = _settings.EditingAttribute == BoneAttribute.Rotation
                ? "Roll"
                : "X";
            var col2Label = _settings.EditingAttribute == BoneAttribute.Rotation
                ? "Pitch"
                : "Y";
            var col3Label = _settings.EditingAttribute == BoneAttribute.Rotation
                ? "Yaw"
                : "Z";
            var col4Label = _settings.EditingAttribute == BoneAttribute.Scale
                ? "All"
                : "N/A";

            if (ImGui.BeginTable("Bones", 6, ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.BordersV | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollY,
                new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 56)))
            {
                ImGui.TableSetupColumn("\tBones", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthFixed, 3 * CtrlHelper.IconButtonWidth);

                ImGui.TableSetupColumn($"\t{col1Label}", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn($"\t{col2Label}", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn($"\t{col3Label}", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn($"\t{col4Label}", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetColumnEnabled(4, _settings.EditingAttribute == BoneAttribute.Scale);

                ImGui.TableSetupColumn("\tName", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableSetupScrollFreeze(0, 1);

                ImGui.TableHeadersRow();

                if (_settings.ArmatureInProgress != null || _settings.ProfileInProgress != null)
                {
                    IBoneContainer container = _settings.ShowLiveBones && _settings.ArmatureInProgress != null
                        ? _settings.ArmatureInProgress
                        : _settings.ProfileInProgress;

                    var groupedBones = container.GetBoneTransformValues(_settings.EditingAttribute, _settings.ReferenceFrame)
                        .GroupBy(x => BoneData.GetBoneFamily(x.BoneCodeName)).ToList();

                    foreach (var boneGroup in groupedBones.OrderBy(x => (int)x.Key))
                    {
                        //Hide root bone group if it's not enabled in settings
                        if (boneGroup.Key == BoneData.BoneFamily.Root &&
                            !Plugin.ConfigurationManager.Configuration.RootPositionEditingEnabled)
                            continue;

                        //create a dropdown entry for the family if one doesn't already exist
                        //mind that it'll only be rendered if bones exist to fill it
                        if (!_settings.GroupExpandedState.TryGetValue(boneGroup.Key, out var expanded))
                        {
                            _settings.GroupExpandedState[boneGroup.Key] = false;
                            expanded = false;
                        }

                        if (expanded)
                        {
                            //paint the row in header colors if it's expanded
                            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                        }
                        else
                        {
                            ImGui.TableNextRow();
                        }
                        ImGui.TableSetColumnIndex(0);

                        CtrlHelper.ArrowToggle($"##{boneGroup.Key}", ref expanded);
                        ImGui.SameLine();
                        CtrlHelper.StaticLabel(boneGroup.Key.ToString());
                        if (BoneData.DisplayableFamilies.TryGetValue(boneGroup.Key, out var tip) && tip != null)
                            CtrlHelper.AddHoverText(tip);

                        if (expanded)
                        {
                            foreach (TransformInfo trInfo in boneGroup.OrderBy(x => BoneData.GetBoneRanking(x.BoneCodeName)))
                            {
                                CompleteBoneEditor(trInfo);
                            }
                        }

                        _settings.GroupExpandedState[boneGroup.Key] = expanded;
                    }
                }


                ImGui.EndTable();
            }

            ImGui.Separator();

            //----------------------------------

            if (ImGui.BeginTable("Save/Close", 3, ImGuiTableFlags.SizingFixedFit, new Vector2(ImGui.GetWindowWidth() - CtrlHelper.IconButtonWidth, 0)))
            {
                ImGui.TableSetupColumn("Save", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Space", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Close", ImGuiTableColumnFlags.WidthFixed);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                if (ImGui.Button("Save") && _dirty)
                {
                    if (_dirty)
                    {
                        Plugin.ProfileManager.SaveWorkingCopy(false);
                        _dirty = false;
                    }
                }
                CtrlHelper.AddHoverText("Save changes and continue editing");

                ImGui.SameLine();

                if (ImGui.Button("Save and Close"))
                {
                    if (_dirty)
                    {
                        Plugin.ProfileManager.SaveWorkingCopy(true);
                        _dirty = false;
                    }

                    Close();
                }

                CtrlHelper.AddHoverText("Save changes and stop editing");

                    ImGui.TableNextColumn();
                    ImGui.Spacing();
                    ImGui.TableNextColumn();

                if (ImGui.Button("Revert") && _dirty)
                {
                    ConfirmationDialog.Show("Revert all unsaved work?",
                        () =>
                        {
                            Plugin.ProfileManager.RevertWorkingCopy();
                            _dirty = false;
                        });
                }

                CtrlHelper.AddHoverText("Remove all pending changes, reverting to last save");

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    if (_dirty)
                    {
                        ConfirmationDialog.Show("Close editor and abandon all unsaved work?",
                            () =>
                            {
                                Plugin.ProfileManager.StopEditing();
                                Close();
                            });
                    }
                    else
                    {
                        //convenient data handling means we just drop it
                        Plugin.ProfileManager.StopEditing();
                        Close();
                    }
                }
                CtrlHelper.AddHoverText("Close the editor without saving\n(reverting all unsaved changes)");

                ImGui.Dummy(new Vector2(CtrlHelper.IconButtonWidth));

                ImGui.EndTable();
            }
        }

        /// <summary>
        /// Check that the target skeleton is present in game memory and that its profile is enabled.
        /// If necessary, disable editing features that rely on either of these things.
        /// </summary>
        public unsafe void ConfirmSkeletonConnection()
        {
            if (_settings.ArmatureInProgress == null || _settings.ArmatureInProgress.TryLinkSkeleton() == null)
            {
                if (_settings.ShowLiveBones)
                {
                    _settings.ToggleLiveBones(false);
                    _settings.MirrorModeEnabled = false;
                    _settings.ParentingEnabled = false;
                    DisplayNoLinkMsg();
                }
            }
        }

        public void DisplayNoLinkMsg()
        {
            var msg =
                $"The editor can't find {_settings.ProfileInProgress.CharacterName} or their bone data in the game's memory.\nAs a result, certain editing features will be unavailable.";
            MessageDialog.Show(msg);
        }

        #region ImGui helper functions

        public bool ResetBoneButton(string codename, ref Vector3 value)
        {
            var output = ImGuiComponents.IconButton(codename, FontAwesomeIcon.Recycle);
            CtrlHelper.AddHoverText(
                $"Reset '{BoneData.GetBoneDisplayName(codename)}' to default {_settings.EditingAttribute} values");

            if (output)
            {
                value = _settings.EditingAttribute switch
                {
                    BoneAttribute.Scale => Vector3.One,
                    _ => Vector3.Zero
                };
            }

            return output;
        }

        private bool RevertBoneButton(string codename, ref Vector3 value)
        {
            var output = ImGuiComponents.IconButton(codename, FontAwesomeIcon.ArrowCircleLeft);
            CtrlHelper.AddHoverText(
                $"Revert '{BoneData.GetBoneDisplayName(codename)}' to last saved {_settings.EditingAttribute} values");

            if (output)
            {
                //if the backup scale doesn't contain bone values to revert TO, then just reset it

                if (Plugin.ProfileManager.GetProfileByUniqueId(_settings.ProfileInProgress.UniqueId) is CharacterProfile oldProf
                        && oldProf != null
                        && oldProf.Bones.TryGetValue(codename, out var bec)
                        && bec != null)
                {
                    value = _settings.EditingAttribute switch
                    {
                        BoneAttribute.Position => bec.Translation,
                        BoneAttribute.Rotation => bec.Rotation,
                        _ => bec.Scaling
                    };
                }
                else
                {
                    value = _settings.EditingAttribute switch
                    {
                        BoneAttribute.Scale => Vector3.One,
                        _ => Vector3.Zero
                    };
                }
            }

            return output;
        }

        private bool FullBoneSlider(string label, ref Vector3 value)
        {
            float velocity = _settings.EditingAttribute == BoneAttribute.Rotation ? 0.1f : 0.001f;
            float minValue = _settings.EditingAttribute == BoneAttribute.Rotation ? -360.0f : -10.0f;
            float maxValue = _settings.EditingAttribute == BoneAttribute.Rotation ? 360.0f : 10.0f;

            float temp = _settings.EditingAttribute switch
            {
                BoneAttribute.Position => 0.0f,
                BoneAttribute.Rotation => 0.0f,
                _ => value.X == value.Y && value.Y == value.Z ? value.X : 1.0f
            };


            ImGui.PushItemWidth(ImGui.GetColumnWidth());
            if (ImGui.DragFloat(label, ref temp, velocity, minValue, maxValue, $"%.{_precision}f"))
            {
                value = new Vector3(temp, temp, temp);
                return true;
            }

            return false;
        }

        private bool SingleValueSlider(string label, ref float value)
        {
            var velocity = _settings.EditingAttribute == BoneAttribute.Rotation ? 0.1f : 0.001f;
            var minValue = _settings.EditingAttribute == BoneAttribute.Rotation ? -360.0f : -10.0f;
            var maxValue = _settings.EditingAttribute == BoneAttribute.Rotation ? 360.0f : 10.0f;

            ImGui.PushItemWidth(ImGui.GetColumnWidth());
            var temp = value;
            if (ImGui.DragFloat(label, ref temp, velocity, minValue, maxValue, $"%.{_precision}f"))
            {
                value = temp;
                return true;
            }

            return false;
        }

        private void CompleteBoneEditor(TransformInfo trInfo)
        {
            string codename = trInfo.BoneCodeName;
            string displayName = trInfo.BoneDisplayName;

            bool flagUpdate = false;

            Vector3 newVector = trInfo.TransformationValue;

            ImGui.PushID(codename);

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);

            //----------------------------------
            ImGui.Dummy(new Vector2(CtrlHelper.IconButtonWidth * 0.75f, 0));
            ImGui.SameLine();
            flagUpdate |= ResetBoneButton(codename, ref newVector);
            ImGui.SameLine();
            flagUpdate |= RevertBoneButton(codename, ref newVector);

            //TODO the sliders need to cache their value at the instant they're clicked into
            //then transforms can be adjusted using the delta in relation to that cached value

            //----------------------------------
            ImGui.TableNextColumn();
            flagUpdate |= SingleValueSlider($"##{displayName}-X", ref newVector.X);

            //----------------------------------
            ImGui.TableNextColumn();
            flagUpdate |= SingleValueSlider($"##{displayName}-Y", ref newVector.Y);

            //-----------------------------------
            ImGui.TableNextColumn();
            flagUpdate |= SingleValueSlider($"##{displayName}-Z", ref newVector.Z);

            //----------------------------------
            if (_settings.EditingAttribute != BoneAttribute.Scale)
                ImGui.BeginDisabled();

            ImGui.TableNextColumn();
            Vector3 tempVec = new Vector3(newVector.X, newVector.Y, newVector.Z);
            flagUpdate |= FullBoneSlider($"##{displayName}-All", ref newVector);

            if (_settings.EditingAttribute != BoneAttribute.Scale)
                ImGui.EndDisabled();

            //----------------------------------
            ImGui.TableNextColumn();
            CtrlHelper.StaticLabel(displayName, CtrlHelper.TextAlignment.Left, BoneData.IsIVCSBone(codename) ? $"(IVCS) {codename}" : codename);

            ImGui.PopID();

            if (flagUpdate)
            {
                _dirty = true;

                trInfo.TransformationValue = newVector;

                BoneUpdateMode mode = _settings.EditingAttribute switch
                {
                    BoneAttribute.Position => BoneUpdateMode.Position,
                    BoneAttribute.Rotation => BoneUpdateMode.Rotation,
                    _ => BoneUpdateMode.Scale
                };

                trInfo.PushChanges(mode, _settings.MirrorModeEnabled, _settings.ParentingEnabled);
            }
        }

        #endregion
    }

    public struct EditorSessionSettings
    {
        public readonly CharacterProfile ProfileInProgress;
        private string? _originalCharName;
        private string? _originalProfName;

        public Armature ArmatureInProgress => ProfileInProgress.Armature;

        public bool ShowLiveBones = false;
        public bool MirrorModeEnabled = false;
        public bool ParentingEnabled = false;
        public BoneAttribute EditingAttribute = BoneAttribute.Scale;
        public PosingSpace ReferenceFrame = PosingSpace.Self;

        public Dictionary<BoneData.BoneFamily, bool> GroupExpandedState = new();

        public void ToggleLiveBones(bool setTo)
        {
            ShowLiveBones = setTo;
            ProfileInProgress.Enabled = setTo;
        }

        public bool ProfileRenamed() => _originalCharName != ProfileInProgress.CharacterName
            || _originalProfName != ProfileInProgress.ProfileName;

        public EditorSessionSettings(CharacterProfile prof)
        {
            ProfileInProgress = prof;
            Plugin.ArmatureManager.ConstructArmatureForProfile(prof);

            _originalCharName = prof.CharacterName;
            _originalProfName = prof.ProfileName;

            ProfileInProgress.Enabled = true;
            ShowLiveBones = true;
        }
    }
}