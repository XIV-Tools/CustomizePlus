// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
    using System;
    using System.Collections.Generic;
	using System.IO;
	using System.IO.Compression;
	using System.Numerics;
	using System.Text;
	using System.Windows.Forms;
    using Anamnesis.Files;
    using Anamnesis.Posing;
    using CustomizePlus.Memory;
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

		// Change Version when updating the way scales are saved. Import from base64 will then auto fail.
		private byte scaleVersion = 1;

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
			Configuration config = Plugin.Configuration;

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
					config.Save();
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
					BodyScale scale = new();
					scale.CharacterName = characterName;

					// TODO: Build scales from only present bones
					// scale = this.BuildFromName(scale, characterName);
					scale = BuildDefault(scale);
					Plugin.Configuration.BodyScales.Add(scale);
					Plugin.Configuration.ToggleOffAllOtherMatching(characterName, scale.ScaleName);
					if (config.AutomaticEditMode)
					{
						config.Save();
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

				try {
					importVer = ImportFromBase64(Clipboard.GetText(),out json);
					importScale = BuildFromCustomizeJSON(json) ;
				} catch (Exception e) {
					PluginLog.Error(e, "An error occured during import conversion. Please check you coppied the right thing!");
				}

				if (importVer == scaleVersion && importScale != null) {
					Plugin.Configuration.BodyScales.Add(importScale);
					Plugin.Configuration.ToggleOffAllOtherMatching(importScale.CharacterName, importScale.ScaleName);
					if (config.AutomaticEditMode) {
						config.Save();
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

			// IPC Testing Window - Hidden unless enabled in json.
			if (config.DebuggingMode)
			{
				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.Pen))
				{
					IPCTestInterface ipcWindow = new IPCTestInterface();
					ipcWindow.Show(Plugin.PluginInterface);
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

				for (int i = 0;i < config.BodyScales.Count;i++) {
					BodyScale bodyScale = config.BodyScales[i];
					bool bodyScaleEnabled = bodyScale.BodyScaleEnabled;

					ImGui.PushID(i);

					ImGui.TableNextRow();
					ImGui.TableNextColumn();

					// Enable
					ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (12 * fontScale));
					if (ImGui.Checkbox("##Enable", ref bodyScaleEnabled)) {
						if (bodyScale.CharacterName != null)
							config.ToggleOffAllOtherMatching(bodyScale.CharacterName, bodyScale.ScaleName == null ? "" : bodyScale.ScaleName);
						bodyScale.BodyScaleEnabled = bodyScaleEnabled;
						config.Save();
						if (config.AutomaticEditMode) {
							Plugin.LoadConfig(true);
						}
					}

					if (ImGui.IsItemHovered())
						ImGui.SetTooltip($"Enable and disable scale.\nWill disable all other scales for the same character.");

					// Character Name
					ImGui.TableNextColumn();
					string characterName = bodyScale.CharacterName ?? string.Empty;
					ImGui.PushItemWidth(-1);
					if (ImGui.InputText("##Character", ref characterName, 64, ImGuiInputTextFlags.NoHorizontalScroll)) {
						bodyScale.CharacterName = characterName;
					}

					if (ImGui.IsItemHovered())
						ImGui.SetTooltip($"The name of the character this body scale should apply to.");

					// Scale Name
					ImGui.TableNextColumn();
					ImGui.PushItemWidth(-1);
					string scaleName = bodyScale.ScaleName ?? string.Empty;
					if (ImGui.InputText("##Scale Name", ref scaleName, 64, ImGuiInputTextFlags.NoHorizontalScroll)) {
						bodyScale.ScaleName = scaleName;
					}

					if (ImGui.IsItemHovered())
						ImGui.SetTooltip($"A description of the scale.");

					// Edit
					ImGui.TableNextColumn();
					if (ImGuiComponents.IconButton(FontAwesomeIcon.Pen)) {
						EditInterface editWindow = new EditInterface();
						editWindow.Show(bodyScale);
					}

					if (ImGui.IsItemHovered())
						ImGui.SetTooltip($"Edit body scale (WIP)");

					// Import Ana
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(FontAwesomeIcon.FileImport)) {
						this.Import(bodyScale);
					}

					if (ImGui.IsItemHovered())
						ImGui.SetTooltip($"Import scale from Anamnesis");

					// Import Clipboard
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(FontAwesomeIcon.FileExport)) {
						Clipboard.SetText(this.ExportToBase64(bodyScale, scaleVersion));
					}

					if (ImGui.IsItemHovered())
						ImGui.SetTooltip($"Export scale to Clipboard.");

					// Remove
					ImGui.SameLine();
					if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash)) {
						config.BodyScales.Remove(bodyScale);
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
				config.Save();
				Plugin.LoadConfig();
			}

			ImGui.SameLine();

			if (ImGui.Button("Save and Close"))
			{
				config.Save();
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
			OpenFileDialog picker = new();
			picker.Filter = "Anamnesis Pose (*.pose)|*.pose";
			picker.CheckFileExists = true;
			picker.Title = "Customize+ - Import Anamnesis Pose";

			DialogResult result = picker.ShowDialog();
			if (result != DialogResult.OK)
				return;

			string json = File.ReadAllText(picker.FileName);

			JsonSerializerSettings settings = new();
			settings.NullValueHandling = NullValueHandling.Ignore;
			settings.Converters.Add(new PoseFile.VectorConverter());

			PoseFile? file = JsonConvert.DeserializeObject<PoseFile>(json, settings);

			if (file == null)
				throw new Exception("Failed to deserialize pose file");

			// Load scale if it it not null, not 0 and not 1.
			if (file.Scale != null &&
				file.Scale.X != 0 &&
				file.Scale.Y != 0 &&
				file.Scale.Z != 0 &&
				file.Scale.X != 1 &&
				file.Scale.Y != 1 &&
				file.Scale.Z != 1)
			{
				scale.Bones["n_root"].Scale = new HkVector4(file.Scale.X, file.Scale.Y, file.Scale.Z, 1); //todo: might crash
				//scale.RootScale = new HkVector4(file.Scale.X, file.Scale.Y, file.Scale.Z, 1);
			}

			if (file.Bones == null)
				return;

			string name = Path.GetFileNameWithoutExtension(picker.FileName);

			scale.ScaleName = name;

			scale.Bones.Clear();

			foreach ((string boneName, PoseFile.Bone? bone) in file.Bones)
			{
				if (bone == null)
					continue;

				if (bone.Scale == null)
					continue;

				string? modernName = LegacyBoneNameConverter.GetModernName(boneName);
				if (modernName == null)
					modernName = boneName;

				HkVector4 boneScale = new();
				boneScale.X = bone.Scale.X;
				boneScale.Y = bone.Scale.Y;
				boneScale.Z = bone.Scale.Z;

				var editsContainer = new BoneEditsContainer { Position = HkVector4.Zero, Rotation = HkVector4.Zero, Scale = HkVector4.One };

				if (!scale.Bones.ContainsKey(modernName))
					scale.Bones.Add(modernName, editsContainer);

				scale.Bones[modernName] = editsContainer;
			}
		}

		// TODO: Finish feature. May require additional skeleton code from Anamnesis
		// Process only works properly in that when in GPose as it is.
		private unsafe BodyScale BuildFromName(BodyScale scale, string characterName)
		{
			if (characterName == null)
			{
				return BuildDefault(scale);
			}
			else
			{
				GameObject? obj = Plugin.FindModelByName(characterName);
				if (obj == null)
					return BuildDefault(scale);
				try
				{
					List<string> boneNameList = new();

					RenderSkeleton* skele = RenderSkeleton.FromActor(obj);

					// IEnumerator<HkaBone> realBones = skele->PartialSkeletons->Pose1->Skeleton->Bones.GetEnumerator();
					// HkaPose* pose = skele->PartialSkeletons->Pose1;
					// skele

					// PluginLog.Information(skele->ToString());
					/*
					while (realBones.MoveNext())
					{
						string? boneName = realBones.Current.GetName();
						if (boneName == null)
						{
							PluginLog.Error($"Null bone found: {realBones.ToString()}");
						}
						else
						{
							boneNameList.Add(boneName);
						}
					}
					*/
					scale.ScaleName = $"Built from real bones of {scale.CharacterName}";
				}
				catch (Exception ex)
				{
					PluginLog.Error($"Failed to get bones from skelton by name:{ex}");
				}
			}
			scale.ScaleName = $"Default";
			return BuildDefault(scale);
		}

		// Scale returns as null if it fails.
		public static BodyScale? BuildFromCustomizeJSON(string json) {
			BodyScale scale = null;

			JsonSerializerSettings settings = new();
			settings.NullValueHandling = NullValueHandling.Ignore;
			settings.Converters.Add(new PoseFile.VectorConverter());
			scale = JsonConvert.DeserializeObject<BodyScale>(json, settings);
			return scale;
		}

		// TODO: Change to using real bone dict and not existing JSON logic.
		public static BodyScale BuildDefault(BodyScale scale)
		{
			string json = DefaultFile;

			scale = BuildFromJSON(scale, json);

			scale.ScaleName = "Default";

			return scale;
		}

		private static BodyScale? BuildFromJSON(BodyScale scale, string json)
		{
			if (json == null)
				return null;

			JsonSerializerSettings settings = new();
			settings.NullValueHandling = NullValueHandling.Ignore;
			settings.Converters.Add(new PoseFile.VectorConverter());

			// PluginLog.Debug(json.ToString());

			PoseFile? file = JsonConvert.DeserializeObject<PoseFile>(json, settings);

			if (file == null)
				throw new Exception("Failed to deserialize pose file");

			// Load scale if it it not null, not 0 and not 1.
			if (file.Scale != null &&
				file.Scale.X != 0 &&
				file.Scale.Y != 0 &&
				file.Scale.Z != 0 &&
				file.Scale.X != 1 &&
				file.Scale.Y != 1 &&
				file.Scale.Z != 1)
			{
				//scale.RootScale = new HkVector4(file.Scale.X, file.Scale.Y, file.Scale.Z, 1);
				scale.Bones["n_root"].Scale = new HkVector4(file.Scale.X, file.Scale.Y, file.Scale.Z, 1); //todo: might crash
			}

			if (file.Bones == null)
				return null;

			// this.ScaleName = "Default (Failed to get real bones from model)";
			scale.Bones.Clear();

			foreach ((string boneName, PoseFile.Bone? bone) in file.Bones)
			{
				if (bone == null)
					continue;

				if (bone.Scale == null)
					continue;

				string? modernName = LegacyBoneNameConverter.GetModernName(boneName);
				if (modernName == null)
					modernName = boneName;

				HkVector4 boneScale = new();
				boneScale.X = bone.Scale.X;
				boneScale.Y = bone.Scale.Y;
				boneScale.Z = bone.Scale.Z;

				var editsContainer = new BoneEditsContainer {Position = HkVector4.Zero, Rotation = HkVector4.Zero, Scale = HkVector4.One };

				if (!scale.Bones.ContainsKey(modernName))
					scale.Bones.Add(modernName, editsContainer);

				scale.Bones[modernName] = editsContainer;
			}

			return scale;
		}

		private static readonly string DefaultFile = @"{""FileExtension"": "".pose"", ""TypeName"": ""Default"", ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""0, 0, 0"", ""Bones"": {
			""Root"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 1"", ""Scale"": ""1, 1, 1""},
			""Abdomen"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 1"", ""Scale"": ""1, 1, 1"" },
			""Throw"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 1"", ""Scale"": ""1, 1, 1"" },
			""Waist"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""SpineA"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""LegLeft"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""LegRight"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""HolsterLeft"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""HolsterRight"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""SheatheLeft"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""SheatheRight"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""SpineB"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""ClothBackALeft"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_b_a_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_f_a_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_f_a_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_s_a_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_s_a_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_asi_b_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_asi_b_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_mune_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_mune_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sebo_c"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_b_b_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_b_b_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_f_b_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_f_b_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_s_b_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_s_b_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_asi_c_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_asi_c_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_buki_sebo_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_buki_sebo_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_kubi"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sako_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sako_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_b_c_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_b_c_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_f_c_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_f_c_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_s_c_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_sk_s_c_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_hizasoubi_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_hizasoubi_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_asi_d_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_asi_d_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_kao"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_ude_a_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_ude_a_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_kataarmor_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_kataarmor_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_ago"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_asi_e_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_asi_e_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_kami_a"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_kami_f_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_kami_f_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_mimi_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_mimi_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_ude_b_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_ude_b_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_hkata_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_hkata_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_kami_b"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_te_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_te_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_buki_tate_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_buki_tate_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_ear_a_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_ear_a_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_hhiji_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_hhiji_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_hijisoubi_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_hijisoubi_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_hte_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_hte_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_hito_a_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_hito_a_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_ko_a_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_ko_a_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_kusu_a_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_kusu_a_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_naka_a_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_naka_a_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_oya_a_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_oya_a_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_buki_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_buki_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_ear_b_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""n_ear_b_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_hito_b_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_hito_b_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_ko_b_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_ko_b_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_kusu_b_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_kusu_b_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_naka_b_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_naka_b_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_oya_b_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_oya_b_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_dmab_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_dmab_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_eye_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_eye_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_hana"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_hoho_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_hoho_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_lip_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_lip_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_mayu_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_mayu_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_memoto"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_miken_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_miken_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_ulip_a"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_umab_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_umab_r"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_dlip_a"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_ulip_b"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_f_dlip_b"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_ex_h0106_ke_f_a"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_ex_h0106_ke_l"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""1, 1, 1"" },
			""j_ex_h0106_ke_f_b"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"",""Scale"": ""1, 1, 1""},
			""mh_n_root"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 1"",""Scale"": ""1, 1, 1""},
			""mh_n_hara"": { ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 1"",""Scale"": ""1, 1, 1""} } }";
	}
}
