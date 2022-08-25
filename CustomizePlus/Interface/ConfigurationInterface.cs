// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Numerics;
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

		protected override string Title => "Customize+ Configuration";
		protected override bool SingleInstance => true;

		public static void Show()
		{
			Plugin.InterfaceManager.Show<ConfigurationInterface>();
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
					Plugin.LoadConfig();
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
				ImGui.SetTooltip($"Apply scales to NPCs in cutscenes.\nSpecify a scale with the name 'CutsceneDefault' to apply it to all generic characters.");
			
			ImGui.Separator();
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
					scale = this.BuildDefault(scale);
					Plugin.Configuration.BodyScales.Add(scale);
					Plugin.Configuration.ToggleOffAllOtherMatching(characterName, scale.ScaleName);
					if (config.AutomaticEditMode)
					{
						config.Save();
						Plugin.LoadConfig();
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

			ImGui.BeginChild("scrolling", new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 56), false);

			for (int i = 0; i < config.BodyScales.Count; i++)
			{
				BodyScale bodyScale = config.BodyScales[i];
				bool bodyScaleEnabled = bodyScale.BodyScaleEnabled;

				ImGui.PushID(i);

				ImGui.SetNextItemWidth((ImGui.GetWindowSize().X - 350) / 2);
				string characterName = bodyScale.CharacterName ?? string.Empty;
				if (ImGui.InputText("Character", ref characterName, 512, ImGuiInputTextFlags.NoHorizontalScroll))
				{
					bodyScale.CharacterName = characterName;
				}

				if (ImGui.IsItemHovered())
					ImGui.SetTooltip($"The name of the character this body scale should apply to.");

				ImGui.SameLine();

				ImGui.SetNextItemWidth((ImGui.GetWindowSize().X - 350) / 2);
				string scaleName = bodyScale.ScaleName ?? string.Empty;
				if (ImGui.InputText("Scale Name", ref scaleName, 512, ImGuiInputTextFlags.NoHorizontalScroll))
				{
					bodyScale.ScaleName = scaleName;
				}

				if (ImGui.IsItemHovered())
					ImGui.SetTooltip($"A description of the scale.");

				ImGui.SameLine();
				if (ImGui.Checkbox("Enable", ref bodyScaleEnabled))
				{
					if (bodyScale.CharacterName != null)
						config.ToggleOffAllOtherMatching(bodyScale.CharacterName, bodyScale.ScaleName);
					bodyScale.BodyScaleEnabled = bodyScaleEnabled;
					config.Save();
					if (config.AutomaticEditMode)
					{
						Plugin.LoadConfig();
					}
				}

				if (ImGui.IsItemHovered())
					ImGui.SetTooltip($"Enable and disable scale.\nWill disable all other scales for the same character.");

				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.Pen))
				{
					EditInterface editWindow = new EditInterface();
					editWindow.Show(bodyScale);
				}

				if (ImGui.IsItemHovered())
					ImGui.SetTooltip($"Edit body scale (WIP)");

				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.FileImport))
				{
					this.Import(bodyScale);
				}

				if (ImGui.IsItemHovered())
					ImGui.SetTooltip($"Import scale from Anamnesis");

				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
				{
					config.BodyScales.Remove(bodyScale);
				}

				if (ImGui.IsItemHovered())
					ImGui.SetTooltip($"Remove");

				ImGui.PopID();
			}

			ImGui.EndChild();
			ImGui.Separator();

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
				scale.RootScale = new HkVector4(file.Scale.X, file.Scale.Y, file.Scale.Z, 1);
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

				if (!scale.Bones.ContainsKey(modernName))
					scale.Bones.Add(modernName, boneScale);

				scale.Bones[modernName] = boneScale;
			}
		}

		// TODO: Finish feature. May require additional skeleton code from Anamnesis
		// Process only works properly in that when in GPose as it is.
		private unsafe BodyScale BuildFromName(BodyScale scale, string characterName)
		{
			if (characterName == null)
			{
				return this.BuildDefault(scale);
			}
			else
			{
				GameObject? obj = Plugin.FindModelByName(characterName);
				if (obj == null)
					return this.BuildDefault(scale);
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
			return this.BuildDefault(scale);
		}

		// TODO: Change to using real bone dict and not existing JSON logic.
		private BodyScale BuildDefault(BodyScale scale)
		{
			string json = this.defaultFile;

			scale = this.BuildFromJSON(scale, json);

			scale.ScaleName = "Default";

			return scale;
		}

		private BodyScale BuildFromJSON(BodyScale scale, string json)
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
				scale.RootScale = new HkVector4(file.Scale.X, file.Scale.Y, file.Scale.Z, 1);
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

				if (!scale.Bones.ContainsKey(modernName))
					scale.Bones.Add(modernName, boneScale);

				scale.Bones[modernName] = boneScale;
			}

			return scale;
		}

		private readonly string defaultFile = @"{""FileExtension"": "".pose"", ""TypeName"": ""Default"", ""Position"": ""0, 0, 0"", ""Rotation"": ""0, 0, 0, 0"", ""Scale"": ""0, 0, 0"", ""Bones"": {
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
