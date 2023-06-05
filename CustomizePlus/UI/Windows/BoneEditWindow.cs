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
using ImGuiNET;

namespace CustomizePlus.UI.Windows
{
    public class BoneEditWindow : WindowBase
    {
        protected override string Title => $"Edit Profile: {profileInProgress.ProfileName}";
        protected override bool SingleInstance => true;
        protected override string DrawTitle => $"{Title}###customize_plus_scale_edit_window{Index}"; //keep the same ID for all scale editor windows

        protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoScrollbar |
            (dirty ? ImGuiWindowFlags.UnsavedDocument : ImGuiWindowFlags.None);
        private int precision = 3;

        protected override bool LockCloseButton => true;

        public EditorSessionSettings Settings;

        private CharacterProfile? profileInProgress;
        private string? originalCharName;
        private string? originalProfName;
        private Armature? SkeletonInProgress => profileInProgress.Armature;

        private bool dirty = false;

        private float windowHorz; //for formatting :V

        public static void Show(CharacterProfile prof)
        {
            var editWnd = Plugin.InterfaceManager.Show<BoneEditWindow>();

            editWnd.profileInProgress = prof;
            editWnd.originalCharName = prof.CharacterName;
            editWnd.originalProfName = prof.ProfileName;

            //By having the armature manager to do its checks on this profile,
            //	we force it to generate and track a new armature for it
            Plugin.ArmatureManager.RenderCharacterProfiles(prof);

            editWnd.Settings = new EditorSessionSettings(prof.Armature);

            editWnd.ConfirmSkeletonConnection();
        }

        protected override void DrawContents()
        {
            windowHorz = ImGui.GetWindowWidth();

            if (profileInProgress.CharacterName != originalCharName
                || profileInProgress.ProfileName != originalProfName)
            {
                dirty = true;
            }

            ImGui.SetNextItemWidth(windowHorz / 4);
            CtrlHelper.TextPropertyBox("Character Name",
                () => profileInProgress.CharacterName,
                (s) => profileInProgress.CharacterName = s);

            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();

            ImGui.SetNextItemWidth(windowHorz / 4);
            CtrlHelper.TextPropertyBox("Profile Name",
                () => profileInProgress.ProfileName,
                (s) => profileInProgress.ProfileName = s);

            var tempEnabled = profileInProgress.Enabled;
            if (CtrlHelper.Checkbox("Enable Preview", ref tempEnabled))
            {
                profileInProgress.Enabled = tempEnabled;
                ConfirmSkeletonConnection();
            }
            CtrlHelper.AddHoverText($"Hook the editor into the game to edit and preview live bone data");

            ImGui.SameLine();

            if (!profileInProgress.Enabled) ImGui.BeginDisabled();

            if (CtrlHelper.Checkbox("Show Live Bones", ref Settings.ShowLiveBones))
            {
                ConfirmSkeletonConnection();
            }
            CtrlHelper.AddHoverText($"If selected, present for editing all bones found in the game data,\nelse show only bones for which the profile already contains edits.");

            ImGui.SameLine();

            ImGui.BeginDisabled();
            var tempRefSnap = profileInProgress.Armature.GetReferenceSnap();
            if (CtrlHelper.Checkbox("A-Pose", ref tempRefSnap))
            {
                ConfirmSkeletonConnection();
                profileInProgress.Armature.SetReferenceSnap(tempRefSnap);
            }
            CtrlHelper.AddHoverText($"Force character into their default reference pose");
            ImGui.EndDisabled();

            ImGui.SameLine();

            if (CtrlHelper.Checkbox("Mirror Mode", ref Settings.MirrorModeEnabled))
            {
                ConfirmSkeletonConnection();
            }
            CtrlHelper.AddHoverText($"Bone changes will be reflected from left to right and vice versa");

            ImGui.SameLine();

            if (CtrlHelper.Checkbox("Parenting Mode", ref Settings.ParentingEnabled))
            {
                ConfirmSkeletonConnection();
            }
            CtrlHelper.AddHoverText($"Changes will propagate \"outward\" from edited bones");

            if (!profileInProgress.Enabled) ImGui.EndDisabled();

            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();

            ImGui.Separator();

            if (ImGui.RadioButton("Position", Settings.EditingAttribute == BoneAttribute.Position))
            {
                Settings.EditingAttribute = BoneAttribute.Position;
            }
            CtrlHelper.AddHoverText($"May have unintended effects. Edit at your own risk!");

            ImGui.SameLine();
            if (ImGui.RadioButton("Rotation", Settings.EditingAttribute == BoneAttribute.Rotation))
            {
                Settings.EditingAttribute = BoneAttribute.Rotation;
            }
            CtrlHelper.AddHoverText($"May have unintended effects. Edit at your own risk!");

            ImGui.SameLine();
            if (ImGui.RadioButton("Scale", Settings.EditingAttribute == BoneAttribute.Scale))
            {
                Settings.EditingAttribute = BoneAttribute.Scale;
            }

            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();

            ImGui.TextUnformatted("|");

            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();

            if (!Settings.EditStack.UndoPossible()) ImGui.BeginDisabled();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
            {
                Settings.EditStack.Undo();
            }
            CtrlHelper.AddHoverText("Undo last edit");
            if (!Settings.EditStack.UndoPossible()) ImGui.EndDisabled();

            ImGui.SameLine();

            if (!Settings.EditStack.RedoPossible()) ImGui.BeginDisabled();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.RedoAlt))
            {
                Settings.EditStack.Redo();
            }
            CtrlHelper.AddHoverText("Redo next edit");
            if (!Settings.EditStack.RedoPossible()) ImGui.EndDisabled();

            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();

            ImGui.TextUnformatted("|");

            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();

            if (!profileInProgress.Enabled) ImGui.BeginDisabled();
            if (ImGui.Button("Reload Bone Data"))
            {
                SkeletonInProgress.RebuildSkeleton();
            }
            CtrlHelper.AddHoverText("Refresh the skeleton data obtained from in-game");
            if (!profileInProgress.Enabled) ImGui.EndDisabled();

            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();

            ImGui.TextUnformatted("|");

            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();

            ImGui.SetNextItemWidth(150);
            //ImGui.SliderInt("Precision", ref precision, 1, 6);
            ImGui.SliderInt(string.Empty, ref precision, 1, 6, $"Precision = {precision}");
            CtrlHelper.AddHoverText("Level of precision to display while editing values");

            ImGui.Separator();

            //CompleteBoneEditor("n_root");

            var col1Label = Settings.EditingAttribute == BoneAttribute.Rotation
                ? $"Roll"
                : $"X";
            var col2Label = Settings.EditingAttribute == BoneAttribute.Rotation
                ? $"Pitch"
                : $"Y";
            var col3Label = Settings.EditingAttribute == BoneAttribute.Rotation
                ? $"Yaw"
                : $"Z";
            var col4Label = Settings.EditingAttribute == BoneAttribute.Scale
                ? $"All"
                : $"N/A";

            if (ImGui.BeginTable("Bones", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollY,
                new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 56)))
            {
                ImGui.TableSetupColumn("Bones", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthFixed);

                ImGui.TableSetupColumn(col1Label, ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(col2Label, ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(col3Label, ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(col4Label, ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableSetColumnEnabled(4, Settings.EditingAttribute == BoneAttribute.Scale);

                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableSetupScrollFreeze(0, 1);

                ImGui.TableHeadersRow();

                var relevantBoneNames = Settings.ShowLiveBones
                    ? SkeletonInProgress.GetExtantBoneNames()
                    : profileInProgress.Bones.Keys;

                var groupedBones = relevantBoneNames.GroupBy(BoneData.GetBoneFamily);

                foreach (var boneGroup in groupedBones.OrderBy(x => (int)x.Key))
                {
	                //Hide root bone if it's not enabled in settings
	                if (boneGroup.Key == BoneData.BoneFamily.Root &&
	                    !Plugin.ConfigurationManager.Configuration.RootPositionEditingEnabled)
	                    continue;
                                    
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);

                    //create a dropdown entry for the family if one doesn't already exist
                    //mind that it'll only be rendered if bones exist to fill it
                    if (!Settings.GroupExpandedState.TryGetValue(boneGroup.Key, out var expanded))
                    {
                        Settings.GroupExpandedState[boneGroup.Key] = false;
                        expanded = false;
                    }

                    CtrlHelper.ArrowToggle($"##{boneGroup.Key}", ref expanded);
                    ImGui.SameLine();
                    CtrlHelper.StaticLabel(boneGroup.Key.ToString());
                    if (BoneData.DisplayableFamilies.TryGetValue(boneGroup.Key, out var tip) && tip != null)
                    {
                        CtrlHelper.AddHoverText(tip);
                    }

                    if (expanded)
                    {
                        foreach (var codename in boneGroup.OrderBy(BoneData.GetBoneIndex))
                        {
                            CompleteBoneEditor(codename);
                        }
                    }

                    Settings.GroupExpandedState[boneGroup.Key] = expanded;
                }

                ImGui.EndTable();
            }

            ImGui.Separator();

            //----------------------------------

            if (ImGui.Button("Save") && dirty)
            {
                if (dirty)
                {
                    Plugin.ProfileManager.SaveWorkingCopy(profileInProgress, false);
                    SkeletonInProgress.RebuildSkeleton();
                }
            }
            CtrlHelper.AddHoverText("Save changes and continue editing");

            ImGui.SameLine();

            if (ImGui.Button("Save and Close"))
            {
                if (dirty)
                {
                    Plugin.ProfileManager.SaveWorkingCopy(profileInProgress, true);
                    //Plugin.RefreshPlugin();
                }

                Close();
            }
            CtrlHelper.AddHoverText("Save changes and stop editing");

            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();

            ImGui.Spacing();
            ImGui.SameLine();

            if (ImGui.Button("Revert") && dirty)
            {
                ConfirmationDialog.Show("Revert all unsaved work?",
                    () =>
                    {
                        Plugin.ProfileManager.RevertWorkingCopy(profileInProgress);
                        SkeletonInProgress.RebuildSkeleton();
                    });

            }
            CtrlHelper.AddHoverText("Remove all pending changes, reverting to last save");

            ImGui.SameLine();

            if (ImGui.Button("Cancel"))
            {
                //convenient data handling means we just drop it
                Plugin.ProfileManager.StopEditing(profileInProgress);
                Close();
            }
            CtrlHelper.AddHoverText("Close the editor without saving\n(reverting all unsaved changes)");
        }

        public void ConfirmSkeletonConnection()
        {
            if (!SkeletonInProgress.TryLinkSkeleton())
            {
                profileInProgress.Enabled = false;

                Settings.ShowLiveBones = false;
                Settings.MirrorModeEnabled = false;
                Settings.ParentingEnabled = false;
                DisplayNoLinkMsg();
            }
            else if (!profileInProgress.Enabled)
            {
                Settings.ShowLiveBones = false;
                Settings.MirrorModeEnabled = false;
                Settings.ParentingEnabled = false;
            }
        }

        public void DisplayNoLinkMsg()
        {
            var msg = $"The editor can't find {profileInProgress.CharacterName} or their bone data in the game's memory.\nCertain editing features will be unavailable.";
            MessageDialog.Show(msg);
        }

        #region ImGui helper functions

        public bool ResetBoneButton(string codename, ref Vector3 value)
        {
            var output = ImGuiComponents.IconButton(codename, FontAwesomeIcon.Recycle);
            CtrlHelper.AddHoverText($"Reset '{BoneData.GetBoneDisplayName(codename)}' to default {Settings.EditingAttribute} values");

            if (output)
            {
                value = Settings.EditingAttribute switch
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
            CtrlHelper.AddHoverText($"Revert '{BoneData.GetBoneDisplayName(codename)}' to last saved {Settings.EditingAttribute} values");

            if (output)
            {
                //if the backup scale doesn't contain bone values to revert TO, then just reset it

                value = Plugin.ProfileManager.Profiles.TryGetValue(profileInProgress, out var oldProf)
                    && oldProf != null
                    && oldProf.Bones.TryGetValue(codename, out var bec)
                    && bec != null
                    ? Settings.EditingAttribute switch
                    {
                        BoneAttribute.Position => bec.Translation,
                        BoneAttribute.Rotation => bec.Rotation,
                        _ => bec.Scaling
                    }
                    : Settings.EditingAttribute switch
                    {
                        BoneAttribute.Scale => Vector3.One,
                        _ => Vector3.Zero
                    };
            }

            return output;
        }

        private bool FullBoneSlider(string label, ref Vector3 value)
        {
            var velocity = Settings.EditingAttribute == BoneAttribute.Rotation ? 1.0f : 0.001f;
            var minValue = Settings.EditingAttribute == BoneAttribute.Rotation ? -360.0f : -10.0f;
            var maxValue = Settings.EditingAttribute == BoneAttribute.Rotation ? 360.0f : 10.0f;

            var temp = Settings.EditingAttribute switch
            {
                BoneAttribute.Position => 0.0f,
                BoneAttribute.Rotation => 0.0f,
                _ => value.X == value.Y && value.Y == value.Z ? value.X : 1.0f
            };


            ImGui.PushItemWidth(ImGui.GetColumnWidth());
            if (ImGui.DragFloat(label, ref temp, velocity, minValue, maxValue, $"%.{precision}f"))
            {
                value = new Vector3(temp, temp, temp);
                return true;
            }
            return false;
        }

        private bool SingleBoneSlider(string label, ref float value)
        {
            var velocity = Settings.EditingAttribute == BoneAttribute.Rotation ? 1.0f : 0.001f;
            var minValue = Settings.EditingAttribute == BoneAttribute.Rotation ? -360.0f : -10.0f;
            var maxValue = Settings.EditingAttribute == BoneAttribute.Rotation ? 360.0f : 10.0f;

            var temp = Settings.EditingAttribute switch
            {
                BoneAttribute.Position => 0.0f,
                BoneAttribute.Rotation => 0.0f,
                _ => 1.0f
            };

            ImGui.PushItemWidth(ImGui.GetColumnWidth());
            if (ImGui.DragFloat(label, ref temp, velocity, minValue, maxValue, $"%.{precision}f"))
            {
                value = temp;
                return true;
            }
            return false;
        }

        private bool TripleBoneSlider(string label, ref Vector3 value)
        {
            var velocity = Settings.EditingAttribute == BoneAttribute.Rotation ? 1.0f : 0.001f;
            var minValue = Settings.EditingAttribute == BoneAttribute.Rotation ? -360.0f : -10.0f;
            var maxValue = Settings.EditingAttribute == BoneAttribute.Rotation ? 360.0f : 10.0f;

            return ImGui.DragFloat3(label, ref value, velocity, minValue, maxValue, $"%.{precision}f");
        }

        private void CompleteBoneRow(string codename, string col1, string col2, string col3)
        {
            var transform = new BoneTransform();

            if (Settings.ShowLiveBones)
            {
                if (SkeletonInProgress.Bones.TryGetValue(codename, out var mb)
                    && mb != null
                    && mb.PluginTransform != null)
                {
                    transform = mb.PluginTransform;
                }
            }
            else if (!profileInProgress.Bones.TryGetValue(codename, out transform) || transform == null)
            {
                return;
            }

            var displayName = BoneData.GetBoneDisplayName(codename) ?? codename;

            var flagUpdate = false;

            var newVector = Settings.EditingAttribute switch
            {
                BoneAttribute.Position => transform.Translation,
                BoneAttribute.Rotation => transform.Rotation,
                _ => transform.Scaling
            };

            var originalVector = newVector;

            ImGui.PushID(codename);

            flagUpdate |= ResetBoneButton(codename, ref newVector);

            flagUpdate |= RevertBoneButton(codename, ref newVector);

            //----------------------------------
            ImGui.TableNextColumn();

            var velocity = Settings.EditingAttribute == BoneAttribute.Rotation ? 1.0f : 0.001f;
            var minValue = Settings.EditingAttribute == BoneAttribute.Rotation ? -360.0f : -10.0f;
            var maxValue = Settings.EditingAttribute == BoneAttribute.Rotation ? 360.0f : 10.0f;

            flagUpdate |= ImGui.DragFloat($"##{displayName}-{col1}", ref newVector.X, velocity, minValue, maxValue, $"%.{precision}f");

            //----------------------------------
            ImGui.TableNextColumn();

            flagUpdate |= ImGui.DragFloat($"##{displayName}-{col1}", ref newVector.Y, velocity, minValue, maxValue, $"%.{precision}f");

            //----------------------------------
            ImGui.TableNextColumn();

            flagUpdate |= ImGui.DragFloat($"##{displayName}-{col1}", ref newVector.Z, velocity, minValue, maxValue, $"%.{precision}f");

            //----------------------------------
            ImGui.TableNextColumn();

            if (Settings.EditingAttribute != BoneAttribute.Scale) ImGui.BeginDisabled();

            flagUpdate |= FullBoneSlider($"##{displayName}AllAxes", ref newVector);

            if (Settings.EditingAttribute != BoneAttribute.Scale) ImGui.EndDisabled();

            //----------------------------------
            ImGui.TableNextColumn();

            CtrlHelper.StaticLabel(displayName, BoneData.IsIVCSBone(codename) ? $"(IVCS) {codename}" : codename);

            ImGui.PopID();

            if (flagUpdate)
            {
                dirty = true;

                var whichValue = FrameStackManager.Axis.X;
                if (originalVector.Y != newVector.Y) whichValue = FrameStackManager.Axis.Y;
                if (originalVector.Z != newVector.Z) whichValue = FrameStackManager.Axis.Z;

                Settings.EditStack.Do(codename, Settings.EditingAttribute, whichValue, originalVector, newVector);

                transform.UpdateAttribute(Settings.EditingAttribute, newVector);

                //if we have access to the armature, then use it to push the values through
                //as the bone information allows us to propagate them to siblings and children
                //otherwise access them through the profile directly

                if (profileInProgress.Enabled && SkeletonInProgress.Bones.Any(x => x.Value.BoneName == codename))
                {
                    SkeletonInProgress.UpdateBoneTransform(codename, transform, Settings.MirrorModeEnabled, Settings.ParentingEnabled);
                }
                else
                {
                    profileInProgress.Bones[codename].UpdateToMatch(transform);
                }
            }
        }

        private void CompleteBoneEditor(string codename)
        {
            var transform = new BoneTransform();

            if (Settings.ShowLiveBones)
            {
                if (SkeletonInProgress.Bones.TryGetValue(codename, out var mb)
                    && mb != null
                    && mb.PluginTransform != null)
                {
                    transform = mb.PluginTransform;
                }
            }
            else if (!profileInProgress.Bones.TryGetValue(codename, out transform) || transform == null)
            {
                return;
            }

            var displayName = BoneData.GetBoneDisplayName(codename) ?? codename;

            var flagUpdate = false;

            var newVector = Settings.EditingAttribute switch
            {
                BoneAttribute.Position => transform.Translation,
                BoneAttribute.Rotation => transform.Rotation,
                _ => transform.Scaling
            };

            var originalVector = newVector;

            ImGui.PushID(codename);

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);

            //----------------------------------
            flagUpdate |= ResetBoneButton(codename, ref newVector);
            ImGui.SameLine();
            flagUpdate |= RevertBoneButton(codename, ref newVector);

            //----------------------------------
            ImGui.TableNextColumn();
            flagUpdate |= SingleBoneSlider($"##{displayName}", ref newVector.X);

            //----------------------------------
            ImGui.TableNextColumn();
            flagUpdate |= SingleBoneSlider($"##{displayName}", ref newVector.Y);

            //-----------------------------------
            ImGui.TableNextColumn();
            flagUpdate |= SingleBoneSlider($"##{displayName}", ref newVector.Z);

            //----------------------------------
            if (Settings.EditingAttribute != BoneAttribute.Scale)
                ImGui.BeginDisabled();

            ImGui.TableNextColumn();
            flagUpdate |= FullBoneSlider($"##{displayName}-All", ref newVector);

            if (Settings.EditingAttribute != BoneAttribute.Scale)
                ImGui.EndDisabled();

            //----------------------------------
            ImGui.TableNextColumn();
            CtrlHelper.StaticLabel(displayName, BoneData.IsIVCSBone(codename) ? $"(IVCS) {codename}" : codename);

            ImGui.PopID();

            if (flagUpdate)
            {
                dirty = true;

                var whichValue = FrameStackManager.Axis.X;
                if (originalVector.Y != newVector.Y) whichValue = FrameStackManager.Axis.Y;
                if (originalVector.Z != newVector.Z) whichValue = FrameStackManager.Axis.Z;

                Settings.EditStack.Do(codename, Settings.EditingAttribute, whichValue, originalVector, newVector);

                transform.UpdateAttribute(Settings.EditingAttribute, newVector);

                //if we have access to the armature, then use it to push the values through
                //as the bone information allows us to propagate them to siblings and children
                //otherwise access them through the profile directly

                if (profileInProgress.Enabled && SkeletonInProgress.Bones.Any(x => x.Value.BoneName == codename))
                {
                    SkeletonInProgress.UpdateBoneTransform(codename, transform, Settings.MirrorModeEnabled, Settings.ParentingEnabled);
                }
                else
                {
                    profileInProgress.Bones[codename].UpdateToMatch(transform);
                }
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
            EditStack = new FrameStackManager(armRef);
        }
    }


}