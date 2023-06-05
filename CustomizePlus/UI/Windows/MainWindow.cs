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
using ImGuizmoNET;
using Newtonsoft.Json;

namespace CustomizePlus.UI.Windows
{
    public class MainWindow : WindowBase
    {
        private static string _newCharacterName = GameDataHelper.GetPlayerName() ?? string.Empty;
        private static string _newProfileName = "Default";
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
            /* Upcoming feature to group by either scale name or character name
            List<string> uniqueCharacters = new();
            List<string> uniqueScales = new();

            for (int i = 0; i < config.BodyScales.Count; i++)
            {
                if (!uniqueCharacters.Contains(config.BodyScales[i].CharacterName))
                    uniqueCharacters.Add(config.BodyScales[i].CharacterName);
                if (!uniqueScales.Contains(config.BodyScales[i].ScaleName))
                    uniqueScales.Add(config.BodyScales[i].ScaleName);
            }
            */

            // Draw the File Picker
            _importFilePicker.Draw();

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
                    Plugin.RefreshPlugin(true);

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

            // if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
            // ImGui.SetNextItemWidth(ImGui.GetWindowSize().X - 623);
            if (ImGui.Button("New Profile"))
            {
                ImGui.OpenPopup("Add");
            }

            CtrlHelper.AddHoverText("Create a new character profile");

            ImGui.SameLine();
            if (ImGui.Button("Add from Clipboard"))
            {
                CharacterProfile importedProfile = null;

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
                        Plugin.RefreshPlugin();
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

            CtrlHelper.AddHoverText("Add a character from your Clipboard");

            ImGui.SameLine();
            if (ImGui.Button("Add from Pose"))
            {
                ImportWithImgui();
            }

            CtrlHelper.AddHoverText("Import one or more profiles from Anamnesis*.pose files");

            // IPC Testing Window - Hidden unless enabled in json.
            if (Plugin.ConfigurationManager.Configuration.DebuggingModeEnabled)
            {
                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Pen))
                {
                    IPCTestWindow.Show(DalamudServices.PluginInterface);
                }
            }

            //Settings
            ImGui.SameLine(ImGui.GetWindowWidth() - 30);
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
            {
                SettingsWindow.Show();
            }

            CtrlHelper.AddHoverText("Customize+ Settings");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(5f, 6f));

            //TODO there's probably some imgui functionality to sort the table when you click on the headers

            var fontScale = ImGui.GetIO().FontGlobalScale;
            if (ImGui.BeginTable("Config", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable,
                new Vector2(0, ImGui.GetFrameHeightWithSpacing() - (70 * fontScale))))
            {
                ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
                ImGui.TableSetupColumn("Character", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultSort);
                ImGui.TableSetupColumn("Profile Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Info", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.NoSort);
                ImGui.TableSetupColumn("Options", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.NoSort);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                var sortSpecs = ImGui.TableGetSortSpecs().Specs;
                Func<CharacterProfile, IComparable> sortByAttribute = sortSpecs.ColumnIndex switch
                {
                    0 => x => x.Enabled ? 0 : 1,
                    1 => x => x.CharacterName,
                    2 => x => x.ProfileName,
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
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (12 * fontScale));
                    if (ImGui.Checkbox("##Enable", ref tempEnabled))
                    {
                        if (tempEnabled)
                        {
                            Plugin.ProfileManager.AssertEnabledProfile(prof);
                        }

                        Plugin.RefreshPlugin(true);
                        prof.Enabled = tempEnabled;
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Enable and disable profile.\nOnly one profile can be active per character.");
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
                    if (ImGui.InputText("##Profile Name", ref inputProfName, 64, ImGuiInputTextFlags.NoHorizontalScroll))
                    {
                        if (ImGui.IsItemDeactivatedAfterEdit())
                        {
                            var newProfileName = ValidateProfileName(characterName, inputProfName);
                            if (newProfileName != inputProfName)
                            {
                                MessageDialog.Show($"Profile '{inputProfName}' already exists for {characterName}. Renamed to '{newProfileName}'.");
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
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.InfoCircle))
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

        /// <summary>
        ///     Imports a BodyScale using Dalamuds Imgui FileDialog.
        /// </summary>
        private void ImportWithImgui()
        {
            /// <summary>
            /// Action performed when the file is imported.
            /// </summary>
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

        // TODO: Finish feature. May require additional skeleton code from Anamnesis
        // Process only works properly in that when in GPose as it is.

        //private unsafe BodyScale BuildFromName(BodyScale scale, string characterName)
        //{
        //	if (characterName == null)
        //	{
        //		scale = BodyScale.BuildDefault();
        //		return scale;
        //	}
        //	else
        //	{
        //		GameObject? obj = Plugin.FindModelByName(characterName);
        //		if (obj == null)
        //		{
        //			scale = BodyScale.BuildDefault();
        //			return scale;
        //		}

        //		try
        //		{
        //			List<string> boneNameList = new();

        //			RenderSkeleton* skele = RenderSkeleton.FromActor(obj);

        //			// IEnumerator<HkaBone> realBones = skele->PartialSkeletons->Pose1->Skeleton->Bones.GetEnumerator();
        //			// HkaPose* pose = skele->PartialSkeletons->Pose1;
        //			// skele

        //			// PluginLog.Information(skele->ToString());

        //			//while (realBones.MoveNext())
        //			//{
        //			//	string? boneName = realBones.Current.GetName();
        //			//	if (boneName == null)
        //			//	{
        //			//		PluginLog.Error($"Null bone found: {realBones.ToString()}");
        //			//	}
        //			//	else
        //			//	{
        //			//		boneNameList.Add(boneName);
        //			//	}
        //			//}

        //			scale.ScaleName = $"Built from real bones of {scale.CharacterName}";
        //		}
        //		catch (Exception ex)
        //		{
        //			PluginLog.Error($"Failed to get bones from skeleton by name: {ex}");
        //		}
        //	}
        //	scale.ScaleName = $"Default";
        //	scale = BodyScale.BuildDefault();
        //	return scale;
        //}

        // Scale returns as null if it fails.
        //public static BodyScale BuildFromCustomizeJSON(string json)
        //{
        //	BodyScale scale = null;

        //	JsonSerializerSettings settings = new();
        //	settings.NullValueHandling = NullValueHandling.Ignore;
        //	settings.Converters.Add(new PoseFile.VectorConverter());
        //	scale = JsonConvert.DeserializeObject<BodyScale>(json, settings);
        //	return scale;
        //}
    }
}