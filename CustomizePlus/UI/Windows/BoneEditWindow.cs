// © Customize+.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        private string? _originalCharName;
        private string? _originalProfName;
        private int _precision = 3;

        private Armature _targetArmature => _profileInProgress.Armature;

        /// <summary>
        /// The character profile being edited.
        /// </summary>
        private CharacterProfile _profileInProgress = null!;

        /// <summary>
        /// User-selected settings for this instance of the bone edit window.
        /// </summary>
        private EditorSessionSettings _settings;
        /// <inheritdoc/>
        protected override string Title => $"Edit Profile: {_profileInProgress.ProfileName}";
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

            editWnd._profileInProgress = prof;
            editWnd._originalCharName = prof.CharacterName;
            editWnd._originalProfName = prof.ProfileName;

            //By having the armature manager to do its checks on this profile,
            //	we force it to generate and track a new armature for it
            Plugin.ArmatureManager.ConstructArmatureForProfile(prof);

            editWnd._settings = new EditorSessionSettings(prof.Armature);

            //editWnd.ConfirmSkeletonConnection();
        }

        /// <inheritdoc/>
        protected unsafe override void DrawContents()
        {
            CharacterBase* targetObject = null;
            if (_profileInProgress.Enabled
                && !GameDataHelper.TryLookupCharacterBase(_profileInProgress.CharacterName, out targetObject))
            {
                _profileInProgress.Enabled = false;
                DisplayNoLinkMsg();
            }

            if (_profileInProgress.CharacterName != _originalCharName
                || _profileInProgress.ProfileName != _originalProfName)
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
                    () => _profileInProgress.CharacterName,
                    (s) => _profileInProgress.CharacterName = s);

                ImGui.TableNextColumn();

                CtrlHelper.StaticLabel("Profile Name", CtrlHelper.TextAlignment.Center);
                CtrlHelper.TextPropertyBox("##Profile Name",
                    () => _profileInProgress.ProfileName,
                    (s) => _profileInProgress.ProfileName = s);

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

            int numColumns = Plugin.ConfigurationManager.Configuration.DebuggingModeEnabled ? 5 : 3;

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

                var tempEnabled = _profileInProgress.Enabled;
                if (CtrlHelper.Checkbox("Enable Preview", ref tempEnabled))
                {
                    _profileInProgress.Enabled = tempEnabled;
                    ConfirmSkeletonConnection();
                }
                CtrlHelper.AddHoverText($"Hook the editor into the game to edit and preview live bone data");

                ImGui.TableNextColumn();

                if (!_profileInProgress.Enabled) ImGui.BeginDisabled();

                if (CtrlHelper.Checkbox("Show Live Bones", ref _settings.ShowLiveBones))
                {
                    ConfirmSkeletonConnection();
                }
                CtrlHelper.AddHoverText($"If selected, present for editing all bones found in the game data,\nelse show only bones for which the profile already contains edits.");

                if (Plugin.ConfigurationManager.Configuration.DebuggingModeEnabled)
                {
                    ImGui.TableNextColumn();

                    var tempRefSnap = _targetArmature?.SnapToReferencePose ?? false;
                    if (_targetArmature != null && CtrlHelper.Checkbox("A-Pose", ref tempRefSnap))
                    {
                        ConfirmSkeletonConnection();
                        _targetArmature.SnapToReferencePose = tempRefSnap;
                    }
                    CtrlHelper.AddHoverText($"Force character into their default reference pose");
                }

                ImGui.TableNextColumn();

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
                    CtrlHelper.AddHoverText($"Changes will propagate \"outward\" from edited bones");
                }

                if (!_profileInProgress.Enabled) ImGui.EndDisabled();
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

                if (!_profileInProgress.Enabled || targetObject == null) ImGui.BeginDisabled();
                if (ImGui.Button("Reload Bone Data"))
                {
                    _targetArmature.RebuildSkeleton(targetObject);
                }
                CtrlHelper.AddHoverText("Refresh the skeleton data obtained from in-game");
                if (!_profileInProgress.Enabled || targetObject == null) ImGui.EndDisabled();

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

                if (_profileInProgress != null || _targetArmature != null)
                {
                    IEnumerable<EditRowParams> relevantModelBones = _settings.ShowLiveBones && _targetArmature != null
                        ? _targetArmature.GetBones().Select(x => new EditRowParams(x))
                        : _profileInProgress.Bones.Select(x => new EditRowParams(x.Key, x.Value));

                    var groupedBones = relevantModelBones.GroupBy(x => BoneData.GetBoneFamily(x.BoneCodeName)).ToList();

                    //TODO implement the method to pull out the bone parameters if not integrated naturally
                    //with the rest of the bones

                    //IEnumerable<EditRowParams> mhGroup = _settings.ShowLiveBones && _targetArmature != null
                    //    ? _targetArmature.GetMHBones().Select(x => new EditRowParams(x))
                    //    : _profileInProgress.Bones_MH.Select(x => new EditRowParams(x.Key, x.Value));

                    //if (mhGroup.GroupBy(x => BoneData.BoneFamily.MainHand).FirstOrDefault() is var mhg
                    //    && mhg != null
                    //    && mhg.Any())
                    //{
                    //    groupedBones.Add(mhg);
                    //}

                    //IEnumerable<EditRowParams> ohGroup = _settings.ShowLiveBones && _targetArmature != null
                    //    ? _targetArmature.GetOHBones().Select(x => new EditRowParams(x))
                    //    : _profileInProgress.Bones_OH.Select(x => new EditRowParams(x.Key, x.Value));

                    //if (ohGroup.GroupBy(x => BoneData.BoneFamily.OffHand).FirstOrDefault() is var ohg
                    //    && ohg != null
                    //    && ohg.Any())
                    //{
                    //    groupedBones.Add(ohg);
                    //}

                    foreach (var boneGroup in groupedBones.OrderBy(x => (int)x.Key))
                    {
                        //Hide root bone if it's not enabled in settings
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
                            foreach (EditRowParams erp in boneGroup.OrderBy(x => BoneData.GetBoneRanking(x.BoneCodeName)))
                            {
                                CompleteBoneEditor(erp);
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
                        Plugin.ProfileManager.SaveWorkingCopy(_profileInProgress, false);
                        _dirty = false;
                    }
                }
                CtrlHelper.AddHoverText("Save changes and continue editing");

                ImGui.SameLine();

                if (ImGui.Button("Save and Close"))
                {
                    if (_dirty)
                    {
                        Plugin.ProfileManager.SaveWorkingCopy(_profileInProgress, true);
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
                            Plugin.ProfileManager.RevertWorkingCopy(_profileInProgress);
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
                                Plugin.ProfileManager.RevertWorkingCopy(_profileInProgress);
                                Plugin.ProfileManager.StopEditing(_profileInProgress);
                                _dirty = false;
                                Close();
                            });
                    }
                    else
                    {
                        //convenient data handling means we just drop it
                        Plugin.ProfileManager.StopEditing(_profileInProgress);
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
            if (_targetArmature == null || !_targetArmature.TryLinkSkeleton())
            {
                _profileInProgress.Enabled = false;

                _settings.ShowLiveBones = false;
                _settings.MirrorModeEnabled = false;
                _settings.ParentingEnabled = false;
                DisplayNoLinkMsg();
            }
            else if (!_profileInProgress.Enabled)
            {
                _settings.ShowLiveBones = false;
                _settings.MirrorModeEnabled = false;
                _settings.ParentingEnabled = false;
            }
        }

        public void DisplayNoLinkMsg()
        {
            var msg =
                $"The editor can't find {_profileInProgress.CharacterName} or their bone data in the game's memory.\nCertain editing features will be unavailable.";
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

                value = Plugin.ProfileManager.Profiles.TryGetValue(_profileInProgress, out var oldProf)
                        && oldProf != null
                        && oldProf.Bones.TryGetValue(codename, out var bec)
                        && bec != null
                    ? _settings.EditingAttribute switch
                    {
                        BoneAttribute.Position => bec.Translation,
                        BoneAttribute.Rotation => bec.Rotation,
                        _ => bec.Scaling
                    }
                    : _settings.EditingAttribute switch
                    {
                        BoneAttribute.Scale => Vector3.One,
                        _ => Vector3.Zero
                    };
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

        private void CompleteBoneEditor(EditRowParams bone)
        {
            string codename = bone.BoneCodeName;
            string displayName = bone.BoneDisplayName;
            BoneTransform transform = new BoneTransform(bone.Transform);

            bool flagUpdate = false;

            Vector3 newVector = _settings.EditingAttribute switch
            {
                BoneAttribute.Position => transform.Translation,
                BoneAttribute.Rotation => transform.Rotation,
                _ => transform.Scaling
            };

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

                //var whichValue = FrameStackManager.Axis.X;
                //if (originalVector.Y != newVector.Y)
                //{
                //    whichValue = FrameStackManager.Axis.Y;
                //}

                //if (originalVector.Z != newVector.Z)
                //{
                //    whichValue = FrameStackManager.Axis.Z;
                //}

                //_settings.EditStack.Do(codename, Settings.EditingAttribute, whichValue, originalVector, newVector);

                transform.UpdateAttribute(_settings.EditingAttribute, newVector);

                //if we have access to the armature, then use it to push the values through
                //as the bone information allows us to propagate them to siblings and children
                //otherwise access them through the profile directly

                if (_profileInProgress.Enabled && _settings.ShowLiveBones)
                {
                    bone.Basis.UpdateModel(transform, _settings.MirrorModeEnabled, _settings.ParentingEnabled);
                }
                //else
                //{
                //    _profileInProgress.Bones[codename].UpdateToMatch(transform);
                //}
            }
        }

        #endregion
    }

    public struct EditorSessionSettings
    {
        public bool ShowLiveBones = false;
        public bool MirrorModeEnabled = false;
        public bool ParentingEnabled = false;
        public BoneAttribute EditingAttribute = BoneAttribute.Scale;

        public Dictionary<BoneData.BoneFamily, bool> GroupExpandedState = new();
        public FrameStackManager EditStack;

        public EditorSessionSettings(Armature armRef)
        {
            //EditStack = new FrameStackManager(armRef);
            ShowLiveBones = armRef.Profile.Enabled;
        }
    }

    /// <summary>
    /// Simple structure for representing arguments to the editor table.
    /// Can be constructed with or without access to a live armature.
    /// </summary>
    internal struct EditRowParams
    {
        public string BoneCodeName;
        public string BoneDisplayName => BoneData.GetBoneDisplayName(BoneCodeName);
        public BoneTransform Transform;
        public ModelBone? Basis = null;

        public float CachedX;
        public float CachedY;
        public float CachedZ;

        public EditRowParams(ModelBone mb)
        {
            BoneCodeName = mb.BoneName;
            Transform = mb.CustomizedTransform;
            Basis = mb;
        }

        public EditRowParams(string codename, BoneTransform tr)
        {
            BoneCodeName = codename;
            Transform = tr;
            Basis = null;
        }
    }
}