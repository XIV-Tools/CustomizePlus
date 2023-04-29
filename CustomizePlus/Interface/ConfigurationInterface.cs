// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
    using System;
    using System.Collections.Generic;
    using System.IO;
	using System.IO.Compression;
	using System.Linq;
	using System.Numerics;
	using System.Text;
	using System.Windows.Forms;
    using Anamnesis.Files;
    using Anamnesis.Posing;
	using CustomizePlus.Data;
	using CustomizePlus.Data.Configuration;
	using CustomizePlus.Memory;
	using Dalamud.Game.ClientState.Objects.SubKinds;
	using Dalamud.Game.ClientState.Objects.Types;
    using Dalamud.Interface;
    using Dalamud.Interface.Components;
    using Dalamud.Logging;
    using ImGuiNET;
    using Newtonsoft.Json;

	public class ConfigurationInterface : WindowBase
	{
		private string newScaleName = string.Empty;
		private string newScaleCharacter = string.Empty;

		private string? playerCharacterName;

		protected override string Title => "Customize+ Configuration";
		protected override bool SingleInstance => true;

		public static void Show()
		{
			Plugin.InterfaceManager.Show<ConfigurationInterface>();
		}

		public static void Toggle() {
			Plugin.InterfaceManager.Toggle<ConfigurationInterface>();
		}

		protected override void DrawContents()
		{
			PluginConfiguration config = Plugin.ConfigurationManager.Configuration;

			playerCharacterName = DalamudServices.ObjectTable[0]?.Name.ToString();

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

			bool enable = config.Enable;
			if (ImGui.Checkbox("Enable", ref enable))
			{
				config.Enable = enable;
				if (config.AutomaticEditMode)
				{
					Plugin.ConfigurationManager.SaveConfiguration();
					Plugin.LoadConfig(true);
				}
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Toggles Customize+ functionality.\nWhen off, Customize+ will not affect any characters.");

			ImGui.SameLine();

			bool autoModeEnable = config.AutomaticEditMode;
			if (ImGui.Checkbox("Automatic Mode", ref autoModeEnable))
			{
				config.AutomaticEditMode = autoModeEnable;
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Applies changes automatically without saving.");

			ImGui.SameLine();

			bool applyToNpcs = config.ApplyToNpcs;
			if (ImGui.Checkbox("Apply to NPCS", ref applyToNpcs))
			{
				config.ApplyToNpcs = applyToNpcs;
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Apply scales to NPCs.\nSpecify a scale with the name 'Default' for it to apply to all NPCs and non-specified players.");

			ImGui.SameLine();
			/*
			 * May not be needed, was intended for possible FPS fixes
			bool applyToNpcsInBusyAreas = config.ApplyToNpcsInBusyAreas;
			if (ImGui.Checkbox("Apply to NPCS in Busy Areas", ref applyToNpcsInBusyAreas))
			{
				config.ApplyToNpcsInBusyAreas = applyToNpcsInBusyAreas;
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Applies to NPCs in busy areas (when NPCs are in index > 200, which occurs when up to 100 characters are rendered.");

			ImGui.SameLine();
			*/
			bool applyToNpcsInCutscenes = config.ApplyToNpcsInCutscenes;
			if (ImGui.Checkbox("Apply to NPCs in Cutscenes", ref applyToNpcsInCutscenes))
			{
				config.ApplyToNpcsInCutscenes = applyToNpcsInCutscenes;
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Apply scales to NPCs in cutscenes.\nSpecify a scale with the name 'DefaultCutscene' to apply it to all generic characters while in a cutscene.");

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();

			ImGui.Text("Characters:");

			ImGui.SameLine();

			if (ImGui.BeginPopup("Add"))
			{
				ImGui.Text("Character Name:");
				ImGui.InputText(string.Empty, ref this.newScaleCharacter, 1024);

				if (ImGui.Button("OK"))
				{
					string characterName = this.newScaleCharacter;
					BodyScale scale = BodyScale.BuildDefault();
					scale.CharacterName = characterName;

					// TODO: Build scales from only present bones
					// scale = this.BuildFromName(scale, characterName);

					Plugin.ConfigurationManager.Configuration.BodyScales.Add(scale);
					Plugin.ConfigurationManager.ToggleOffAllOtherMatching(scale);
					if (config.AutomaticEditMode)
					{
						Plugin.ConfigurationManager.SaveConfiguration();
						Plugin.LoadConfig(true);
					}
					ImGui.CloseCurrentPopup();
				}

				ImGui.EndPopup();
			}

			// if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
			// ImGui.SetNextItemWidth(ImGui.GetWindowSize().X - 623);
			if (ImGui.Button("Add Character"))
			{
				ImGui.OpenPopup("Add");
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip("Add a character");

			ImGui.SameLine();
			if (ImGui.Button("Add Character from Clipboard")) {
				Byte importVer = 0;
				BodyScale importScale = null;
				string json = null;

				try
				{
					importVer = ImportFromBase64(Clipboard.GetText(),out json);
					importScale = BuildFromCustomizeJSON(json) ;
				}
				catch (Exception e)
				{
					PluginLog.Error(e, "An error occured during import conversion. Please check you coppied the right thing!");
				}

				if (importVer == (byte)Constants.ImportExportVersion && importScale != null) {
					Plugin.ConfigurationManager.Configuration.BodyScales.Add(importScale);
					Plugin.ConfigurationManager.ToggleOffAllOtherMatching(importScale);
					if (config.AutomaticEditMode) {
						Plugin.ConfigurationManager.SaveConfiguration();
						Plugin.LoadConfig(true);
					}
				} else if (importVer == 0 || importScale is null) {
					PluginLog.Error("An error occured during import conversion, but neither ImportFromBase64 nor BuildFromCustomizeJSON threw an Exception. Please report this to the developers.");
				} else {
					PluginLog.Information("You are trying to import an Outdated scale, these are not supported anymore. Sorry.");
				}
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip("Add a character from your Clipboard");

			if (playerCharacterName != null)
			{
				ImGui.SameLine();
				if (ImGui.Button($"Add Scale for {playerCharacterName}"))
				{
					BodyScale newBS = new BodyScale()
					{
						CharacterName = playerCharacterName,
						ScaleName = "Default",
						BodyScaleEnabled = false
					};

					int tryIndex = 1;

					while(config.BodyScales.Contains(newBS))
					{
						newBS.ScaleName = $"Default-{tryIndex}";
						tryIndex++;
					}

					config.BodyScales.Add(newBS);
				}
			}


			// IPC Testing Window - Hidden unless enabled in json.
			if (config.DebuggingMode)
			{
				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.Pen))
				{
					IPCTestInterface.Show(DalamudServices.PluginInterface);
				}
			}

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();

			ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(5f, 6f));

			var fontScale = ImGui.GetIO().FontGlobalScale;
			if (ImGui.BeginTable("Config", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY, new Vector2(0, ImGui.GetFrameHeightWithSpacing() - (70 * fontScale)))) {
				ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
				ImGui.TableSetupColumn("Character");
				ImGui.TableSetupColumn("Name");
				ImGui.TableSetupColumn("Options", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
				ImGui.TableHeadersRow();

				foreach(BodyScale extBS in config.BodyScales.OrderBy(x => x.CharacterName).ThenBy(x => x.ScaleName))
				{
					bool bodyScaleEnabled = extBS.BodyScaleEnabled;

					ImGui.PushID(extBS.GetHashCode());

					ImGui.TableNextRow();
					ImGui.TableNextColumn();

					// Enable
					ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (12 * fontScale));
					if (ImGui.Checkbox("##Enable", ref bodyScaleEnabled)) {
						if (extBS.CharacterName != null)
							Plugin.ConfigurationManager.ToggleOffAllOtherMatching(extBS);
						extBS.BodyScaleEnabled = bodyScaleEnabled;
						Plugin.ConfigurationManager.SaveConfiguration();
						if (config.AutomaticEditMode) {
							Plugin.LoadConfig(true);
						}
					}

					if (ImGui.IsItemHovered())
						ImGui.SetTooltip($"Enable and disable scale.\nWill disable all other scales for the same character.");

					// Character Name
					ImGui.TableNextColumn();
					string characterName = extBS.CharacterName ?? string.Empty;
					ImGui.PushItemWidth(-1);
					if (ImGui.InputText("##Character", ref characterName, 64, ImGuiInputTextFlags.NoHorizontalScroll)) {
						
						if (ImGui.IsItemDeactivatedAfterEdit())
						{
							extBS.CharacterName = characterName;
						}
					}

					if (ImGui.IsItemHovered())
						ImGui.SetTooltip($"The name of the character this body scale should apply to.");

					// Scale Name
					ImGui.TableNextColumn();
					ImGui.PushItemWidth(-1);
					string scaleName = extBS.ScaleName ?? string.Empty;
					if (ImGui.InputText("##Scale Name", ref scaleName, 64, ImGuiInputTextFlags.NoHorizontalScroll)) {
						
						if (ImGui.IsItemDeactivatedAfterEdit() && config.BodyScales.Remove(extBS))
						{
							extBS.ScaleName = scaleName;

							if (config.BodyScales.Contains(extBS))
							{
								int tryIndex = 1;

								do
								{
									extBS.ScaleName = $"{scaleName}-{tryIndex}";
									tryIndex++;
								}
								while (config.BodyScales.Contains(extBS));

								MessageWindow.Show($"Scaling '{scaleName}' already exists for {extBS.CharacterName}. Renamed to '{extBS.ScaleName}'.");
							}

							config.BodyScales.Add(extBS);
						}
					}

					if (ImGui.IsItemHovered())
						ImGui.SetTooltip($"A description of the scale.");

					// Edit
					ImGui.TableNextColumn();
					if (ImGuiComponents.IconButton(FontAwesomeIcon.Pen)) {
						EditInterface.Show(extBS);
					}

					if (ImGui.IsItemHovered())
						ImGui.SetTooltip($"Edit body scale (WIP)");

					// Import Ana
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(FontAwesomeIcon.FileImport)) {
						this.Import(extBS);
						Plugin.ConfigurationManager.SaveConfiguration();
						Plugin.LoadConfig(true);
					}

					if (ImGui.IsItemHovered())
						ImGui.SetTooltip($"Import scale from Anamnesis");

					// Import Clipboard
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(FontAwesomeIcon.FileExport)) {
						Clipboard.SetText(this.ExportToBase64(extBS, Constants.ImportExportVersion));
					}

					if (ImGui.IsItemHovered())
						ImGui.SetTooltip($"Export scale to Clipboard.");

					// Remove
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash)) {
						string msg = $"Are you sure you want to permanently delete '{extBS.ScaleName}' scaling for {extBS.CharacterName}?";
						ConfirmationDialog.Show(msg, () => config.BodyScales.Remove(extBS), "Delete Scaling?");
					}

					if (ImGui.IsItemHovered())
						ImGui.SetTooltip($"Remove");

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
				Plugin.ConfigurationManager.SaveConfiguration();
				Plugin.LoadConfig();
			}

			ImGui.SameLine();

			if (ImGui.Button("Save and Close"))
			{
				Plugin.ConfigurationManager.SaveConfiguration();
				Plugin.LoadConfig();
				this.Close();
			}
		}

		// Compress any type to a base64 encoding of its compressed json representation, prepended with a version byte.
		// Returns an empty string on failure.
		// Original by Ottermandias: OtterGui <3
		private unsafe string ExportToBase64<T>(T bodyScale, byte version) {
			try {
				var json = JsonConvert.SerializeObject(bodyScale, Formatting.None);
				var bytes = Encoding.UTF8.GetBytes(json);
				using var compressedStream = new MemoryStream();
				using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress)) {
					zipStream.Write(new ReadOnlySpan<byte>(&version, 1));
					zipStream.Write(bytes, 0, bytes.Length);
				}

				return Convert.ToBase64String(compressedStream.ToArray());
			} catch {
				return string.Empty;
			}
		}

		// Decompress a base64 encoded string to the given type and a prepended version byte if possible.
		// On failure, data will be String error and version will be byte.MaxValue.
		// Original by Ottermandias: OtterGui <3
		public static byte ImportFromBase64(string base64, out string data) {
			var version = byte.MaxValue;
			try {
				var bytes = Convert.FromBase64String(base64);
				using var compressedStream = new MemoryStream(bytes);
				using var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
				using var resultStream = new MemoryStream();
				zipStream.CopyTo(resultStream);
				bytes = resultStream.ToArray();
				version = bytes[0];
				var json = Encoding.UTF8.GetString(bytes, 1, bytes.Length - 1);
				data = json;
			} catch {
				data = "error";
			}

			return version;
		}

		private void Import(BodyScale scale)
		{
			Action importAction = () =>
			{
				OpenFileDialog picker = new();
				picker.Filter = "Anamnesis Pose (*.pose)|*.pose";
				picker.CheckFileExists = true;
				picker.Title = "Customize+ - Import Anamnesis Pose";

				DialogResult result = picker.ShowDialog();
				if (result != DialogResult.OK)
					return;

				string json = File.ReadAllText(picker.FileName);

				BuildFromJSON(scale, json);

				string name = Path.GetFileNameWithoutExtension(picker.FileName);

				scale.ScaleName = name;
			};

			MessageWindow.Show("Customize+ is only able to import scale from the *.pose files. Position and rotation will be ignored.", new Vector2(570, 100), importAction, "ana_import_pos_rot_warning");
		}

		// TODO: Finish feature. May require additional skeleton code from Anamnesis
		// Process only works properly in that when in GPose as it is.

		private unsafe BodyScale BuildFromName(BodyScale scale, string characterName)
		{
			if (characterName == null)
			{
				scale = BodyScale.BuildDefault();
				return scale;
			}
			else
			{
				GameObject? obj = Plugin.FindModelByName(characterName);
				if (obj == null)
				{
					scale = BodyScale.BuildDefault();
					return scale;
				}

				try
				{
					List<string> boneNameList = new();

					RenderSkeleton* skele = RenderSkeleton.FromActor(obj);

					// IEnumerator<HkaBone> realBones = skele->PartialSkeletons->Pose1->Skeleton->Bones.GetEnumerator();
					// HkaPose* pose = skele->PartialSkeletons->Pose1;
					// skele

					// PluginLog.Information(skele->ToString());
					
					//while (realBones.MoveNext())
					//{
					//	string? boneName = realBones.Current.GetName();
					//	if (boneName == null)
					//	{
					//		PluginLog.Error($"Null bone found: {realBones.ToString()}");
					//	}
					//	else
					//	{
					//		boneNameList.Add(boneName);
					//	}
					//}
					
					scale.ScaleName = $"Built from real bones of {scale.CharacterName}";
				}
				catch (Exception ex)
				{
					PluginLog.Error($"Failed to get bones from skeleton by name: {ex}");
				}
			}
			scale.ScaleName = $"Default";
			scale = BodyScale.BuildDefault();
			return scale;
		}

		// Scale returns as null if it fails.
		public static BodyScale BuildFromCustomizeJSON(string json) {
			BodyScale scale = null;

			JsonSerializerSettings settings = new();
			settings.NullValueHandling = NullValueHandling.Ignore;
			settings.Converters.Add(new PoseFile.VectorConverter());
			scale = JsonConvert.DeserializeObject<BodyScale>(json, settings);
			return scale;
		}

		// TODO: Change to using real bone dict and not existing JSON logic.
		/*
		public static BodyScale BuildDefault(BodyScale scale)
		{
			string json = DefaultFile;

			scale = BuildFromJSON(scale, json);

			scale.ScaleName = "Default";

			return scale;
		}
		*/

		//todo: further refactoring
		private static BodyScale? BuildFromJSON(BodyScale scale, string json)
		{
			if (json == null)
				return null;

			JsonSerializerSettings settings = new();
			settings.NullValueHandling = NullValueHandling.Ignore;
			settings.Converters.Add(new PoseFile.VectorConverter());

			PoseFile? file = JsonConvert.DeserializeObject<PoseFile>(json, settings);

			if (file == null)
				throw new Exception("Failed to deserialize pose file");

			if (file.Bones == null)
				return null;

			scale.Bones.Clear();

			foreach ((string boneName, PoseFile.Bone? bone) in file.Bones)
			{
				if (bone == null)
					continue;

				if (bone.Scale == null)
					continue;

				string? codename = BoneData.GetBoneCodename(boneName);
				if (codename == null)
					codename = boneName;

				if (codename == "n_root")
					continue;

				var editsContainer = new BoneEditsContainer { Position = Constants.ZeroVector, Rotation = Constants.ZeroVector, Scale = new Vector3(bone.Scale.X, bone.Scale.Y, bone.Scale.Z) };

				if (!scale.Bones.ContainsKey(codename))
					scale.Bones.Add(codename, editsContainer);

				scale.Bones[codename] = editsContainer;
			}

			scale.Bones["n_root"] = new BoneEditsContainer { Position = Constants.ZeroVector, Rotation = Constants.ZeroVector, Scale = Constants.OneVector };

			// Load scale if it it not null, not 0 and not 1.
			if (file.Scale != null &&
				file.Scale.X != 0 &&
				file.Scale.Y != 0 &&
				file.Scale.Z != 0 &&
				file.Scale.X != 1 &&
				file.Scale.Y != 1 &&
				file.Scale.Z != 1)
			{
				scale.Bones["n_root"].Scale = new Vector3(file.Scale.X, file.Scale.Y, file.Scale.Z);
			}

			return scale;
		}
	}
}
