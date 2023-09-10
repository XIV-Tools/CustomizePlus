// © Customize+.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using CustomizePlus.Data;
using CustomizePlus.Data.Profile;
using CustomizePlus.Helpers;
using CustomizePlus.UI.Dialogs;
using CustomizePlus.UI.Windows.Debug;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Logging;
using ImGuiNET;
using Newtonsoft.Json;

namespace CustomizePlus.UI.Windows
{
    public class MainWindow : WindowBase
    {
        private static string _newCharacterName = GameDataHelper.GetPlayerName() ?? string.Empty;
        private static string _newProfileName = "New Profile";
        private readonly FileDialogManager _importFilePicker = new();
        private static string? PlayerCharacterName => GameDataHelper.GetPlayerName();

        protected override string Title => "Customize+";
        protected override bool SingleInstance => true;

        public static void Show()
        {
            Plugin.InterfaceManager.Show<MainWindow>();
        }

        public static void Toggle()
        {
            Plugin.InterfaceManager.Toggle<MainWindow>();
        }

        protected override void DrawContents()
        {
            // Draw the File Picker
            _importFilePicker.Draw();

            if (ImGui.BeginTable("##NewProfiles", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoClip))
            {
                ImGui.TableSetupColumn("##NewProf", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##NewImport", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##Space", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##Settings", ImGuiTableColumnFlags.WidthFixed);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                if (ImGui.BeginPopup("Add"))
                {
                    ImGui.Text("Character Name:");
                    ImGui.InputText("##newProfCharName", ref _newCharacterName, 1024);
                    ImGui.Text("Profile Name:");
                    ImGui.InputText("##newProfName", ref _newProfileName, 1024);

                    if (ImGui.Button("OK") && _newCharacterName != string.Empty)
                    {
                        CharacterProfile newProf = new()
                        {
                            CharacterName = _newCharacterName,
                            ProfileName = _newProfileName,
                            Enabled = false
                        };

                        Plugin.ProfileManager.AddAndSaveProfile(newProf);

                        ImGui.CloseCurrentPopup();
                        _newCharacterName = GameDataHelper.GetPlayerName() ?? string.Empty;
                        _newProfileName = "Default";
                    }

                    ImGui.SameLine();
                    ImGui.Spacing();
                    ImGui.SameLine();

                    if (ImGui.Button("Cancel"))
                    {
                        ImGui.CloseCurrentPopup();
                        _newCharacterName = GameDataHelper.GetPlayerName() ?? string.Empty;
                        _newProfileName = "Default";
                    }

                    ImGui.EndPopup();
                }

                if (ImGui.Button("New Profile"))
                {
                    _newCharacterName = GameDataHelper.GetPlayerName() ?? string.Empty;
                    _newProfileName = "New Profile";
                    ImGui.OpenPopup("Add");
                }
                CtrlHelper.AddHoverText("Create a new character profile");

                ImGui.SameLine();

                if (ImGui.Button("New on Target"))
                {
                    _newCharacterName = GameDataHelper.GetPlayerTargetName() ?? string.Empty;
                    _newProfileName = "New Profile";
                    ImGui.OpenPopup("Add");
                }
                CtrlHelper.AddHoverText("Create a new character profile for your current target");

                ImGui.TableNextColumn();

                if (ImGui.Button("Add from Clipboard"))
                {
                    ImportFromClipboard();
                }
                CtrlHelper.AddHoverText("Add a character from your Clipboard");

                ImGui.SameLine();

                if (ImGui.Button("Add from Pose File"))
                {
                    ImportWithImgui();
                }
                CtrlHelper.AddHoverText("Import one or more profiles from Anamnesis*.pose files");

                ImGui.TableNextColumn();

                ImGui.Dummy(new(ImGui.GetColumnWidth() - CtrlHelper.IconButtonWidth, 0));

                ImGui.SameLine();

                //Settings
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
                {
                    SettingsWindow.Show();
                }
                CtrlHelper.AddHoverText("Customize+ Settings");

                ImGui.EndTable();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(5f, 6f));

            //TODO there's probably some imgui functionality to sort the table when you click on the headers

            var fontScale = ImGui.GetIO().FontGlobalScale;
            if (ImGui.BeginTable("Config", 6,
                    ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY |
                    ImGuiTableFlags.Sortable,
                    new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 70 * fontScale)))
            {
                ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
                ImGui.TableSetupColumn("Only Owned", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
                ImGui.TableSetupColumn("Character",
                    ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultSort);
                ImGui.TableSetupColumn("Profile Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Info",
                    ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.NoSort);
                ImGui.TableSetupColumn("Options",
                    ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.NoSort);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                var sortSpecs = ImGui.TableGetSortSpecs().Specs;
                Func<CharacterProfile, IComparable> sortByAttribute = sortSpecs.ColumnIndex switch
                {
                    0 => x => x.Enabled ? 0 : 1,
                    1 => x => x.OwnedOnly ? 0 : 1,
                    2 => x => x.CharacterName,
                    3 => x => x.ProfileName,
                    _ => x => x.CharacterName
                };

                var profileList = sortSpecs.SortDirection == ImGuiSortDirection.Ascending
                    ? Plugin.ProfileManager.Profiles.OrderBy(sortByAttribute).ToList()
                    : sortSpecs.SortDirection == ImGuiSortDirection.Descending
                        ? Plugin.ProfileManager.Profiles.OrderByDescending(sortByAttribute).ToList()
                        : Plugin.ProfileManager.Profiles.ToList();

                foreach (var prof in profileList)
                {
                    ImGui.PushID(prof.GetHashCode());

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    // Enable
                    var tempEnabled = prof.Enabled;
                    ImGui.Dummy(new Vector2((ImGui.GetContentRegionAvail().X - (ImGui.GetStyle().CellPadding.X * 2) - CtrlHelper.IconButtonWidth) / 2, 0));
                    ImGui.SameLine();
                    if (ImGui.Checkbox("##Enable", ref tempEnabled))
                    {
                        if (tempEnabled)
                        {
                            Plugin.ProfileManager.AssertEnabledProfile(prof);
                        }

                        prof.Enabled = tempEnabled;

                        //Send OnProfileUpdate if this is profile of the current player
                        Plugin.IPCManager.OnProfileUpdate(prof);
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Enable and disable profile.\nOnly one profile can be active per character.");
                    }

                    // Owned only
                    ImGui.TableNextColumn();
                    ImGui.Dummy(new Vector2((ImGui.GetContentRegionAvail().X - (ImGui.GetStyle().CellPadding.X * 2) - CtrlHelper.IconButtonWidth) / 2, 0));
                    ImGui.SameLine();
                    var ownedOnly = prof.OwnedOnly;
                    if (ImGui.Checkbox("##OwnedOnly", ref ownedOnly))
                    {
                        prof.OwnedOnly = ownedOnly;
                    }


                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Apply only to player owned objects, like Yourself, Retainers, Pets, Minions, etc.");
                    }

                    // ---

                    // Character Name
                    ImGui.TableNextColumn();
                    var characterName = prof.CharacterName ?? string.Empty;
                    ImGui.PushItemWidth(-1);
                    if (ImGui.InputText("##Character", ref characterName, 64, ImGuiInputTextFlags.NoHorizontalScroll))
                    {
                        if (ImGui.IsItemDeactivatedAfterEdit())
                        {
                            prof.CharacterName = characterName;
                            Plugin.ProfileManager.AddAndSaveProfile(prof);
                        }
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("The name of the character that will use this profile.");
                    }

                    // ---

                    // Profile Name
                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(-1);
                    var inputProfName = prof.ProfileName ?? string.Empty;
                    if (ImGui.InputText("##Profile Name", ref inputProfName, 64,
                            ImGuiInputTextFlags.NoHorizontalScroll))
                    {
                        if (ImGui.IsItemDeactivatedAfterEdit())
                        {
                            var newProfileName = ValidateProfileName(characterName, inputProfName);
                            if (newProfileName != inputProfName)
                            {
                                MessageDialog.Show(
                                    $"Profile '{inputProfName}' already exists for {characterName}. Renamed to '{newProfileName}'.");
                            }

                            prof.ProfileName = newProfileName;
                            Plugin.ProfileManager.AddAndSaveProfile(prof);
                        }
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("A description of the scale.");
                    }

                    // ---

                    ImGui.TableNextColumn();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.InfoCircle) && Plugin.ConfigurationManager.Configuration.DebuggingModeEnabled)
                    {
                        BoneMonitorWindow.Show(prof);
                    }

                    CtrlHelper.AddHoverText(string.Join('\n',
                        $"Profile '{prof.ProfileName}'",
                        $"for {prof.CharacterName}",
                        $"with {prof.Bones.Count} modified bones",
                        $"Created: {prof.CreationDate:yyyy MMM dd, HH:mm}",
                        $"Updated: {prof.CreationDate:yyyy MMM dd, HH:mm}"));

                    // ---

                    // Edit
                    ImGui.TableNextColumn();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Pen)
                        && Plugin.ProfileManager.GetWorkingCopy(prof, out var profCopy)
                        && profCopy != null)
                    {
                        BoneEditWindow.Show(profCopy);
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Edit Profile");
                    }

                    // Dupe
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Copy)
                        && Plugin.ProfileManager.GetWorkingCopy(prof, out var dupe)
                        && dupe != null)
                    {
                        var newProfileName = ValidateProfileName(characterName, inputProfName);
                        dupe.ProfileName = newProfileName;

                        Plugin.ProfileManager.StopEditing(dupe);
                        Plugin.ProfileManager.AddAndSaveProfile(dupe, true);
                    }

                    CtrlHelper.AddHoverText("Duplicate Profile");

                    // Export to Clipboard
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.ClipboardUser))
                    {
                        Clipboard.SetText(Base64Helper.ExportToBase64(prof, Constants.ConfigurationVersion));
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Copy Profile to Clipboard.");
                    }

                    // Remove
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
                    {
                        var msg =
                            $"Are you sure you want to permanently delete profile '{prof.ProfileName}' for {prof.CharacterName}?";
                        ConfirmationDialog.Show(msg, () => Plugin.ProfileManager.DeleteProfile(prof),
                            "Delete Scaling?");
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Permanently Delete Profile");
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            ImGui.PopStyleVar();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("Save"))
            {
                Plugin.ProfileManager.SaveAllProfiles();
            }

            ImGui.SameLine();

            if (ImGui.Button("Save and Close"))
            {
                Plugin.ProfileManager.SaveAllProfiles();
                Close();
            }
        }

        private string ValidateProfileName(string charName, string profName)
        {
            var newProfileName = profName;
            var tryIndex = 2;

            while (Plugin.ProfileManager.Profiles
                   .Where(x => x.CharacterName == charName)
                   .Any(x => x.ProfileName == newProfileName))
            {
                newProfileName = $"{profName}-{tryIndex}";
                tryIndex++;
            }

            return newProfileName;
        }

        private void ImportFromClipboard()
        {
            CharacterProfile? importedProfile = null;

            try
            {
                var importVer = Base64Helper.ImportFromBase64(Clipboard.GetText(), out var json);

                importedProfile = Convert.ToInt32(importVer) switch
                {
                    0 => ProfileConverter.ConvertFromConfigV0(json),
                    2 => ProfileConverter.ConvertFromConfigV2(json),
                    3 => JsonConvert.DeserializeObject<CharacterProfile>(json),
                    _ => null
                };

                void AddNewProfile(CharacterProfile newProf)
                {
                    importedProfile.Enabled = false;
                    Plugin.ProfileManager.AddAndSaveProfile(importedProfile);
                }

                if (importedProfile == null)
                {
                    MessageDialog.Show("Error importing information from clipboard.");
                }
                else if (Plugin.ProfileManager.Profiles.Contains(importedProfile))
                {
                    ConfirmationDialog.Show(
                        $"Customize+ already contains profile '{importedProfile.ProfileName}' for {importedProfile.CharacterName}.\nDo you want to replace it?",
                        () => AddNewProfile(importedProfile),
                        "Overwrite Profile?");
                }
                else
                {
                    AddNewProfile(importedProfile);
                }
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "An error occured during import conversion");
            }
        }

        /// <summary>
        ///     Imports a BodyScale using Dalamuds Imgui FileDialog.
        /// </summary>
        private void ImportWithImgui()
        {
            // Action performed when the file is imported.
            void ImportAction()
            {
                _importFilePicker.OpenFileDialog("Import Pose File", ".pose", (isSuccess, path) =>
                {
                    if (isSuccess)
                    {
                        var selectedFilePath = path.FirstOrDefault();
                        //todo: check for selectedFilePath == null?
                        var json = FileHelper.ReadFileAtPath(selectedFilePath);

                        if (json != null)
                        {
                            var profileName = Path.GetFileNameWithoutExtension(selectedFilePath);
                            var import = ProfileConverter.ConvertFromAnamnesis(json, profileName);

                            if (import != null)
                            {
                                Plugin.ProfileManager.AddAndSaveProfile(import);
                            }
                            else
                            {
                                PluginLog.LogError(
                                    $"Error parsing character profile from anamnesis pose file at '{path}'");
                            }
                        }
                    }
                    else
                    {
                        PluginLog.Information(isSuccess + " NO valid file has been selected. " + path);
                    }
                }, 1, null, true);
            }

            MessageDialog.Show(
                "Due to technical limitations, Customize+ is only able to import scale values from *.pose files.\nPosition and rotation information will be ignored.",
                new Vector2(570, 100), ImportAction, "ana_import_pos_rot_warning");
        }
    }
}