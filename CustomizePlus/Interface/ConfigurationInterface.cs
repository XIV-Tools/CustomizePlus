// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
	using System;
	using System.IO;
	using System.Numerics;
	using System.Windows.Forms;
	using Anamnesis.Files;
	using Anamnesis.Posing;
	using CustomizePlus.Memory;
	using Dalamud.Interface;
	using Dalamud.Interface.Components;
	using Dalamud.Logging;
	using ImGuiNET;
	using Newtonsoft.Json;

	public class ConfigurationInterface : WindowBase
	{
		private string newScaleName = string.Empty;

		protected override string Title => "Customize+ Configuration";
		protected override bool SingleInstance => true;

		public static void Show()
		{
			Plugin.InterfaceManager.Show<ConfigurationInterface>();
		}

		protected override void DrawContents()
		{
			Configuration config = Plugin.Configuration;

			bool enable = config.Enable;
			if (ImGui.Checkbox("Enable", ref enable))
			{
				config.Enable = enable;
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Toggles Customize+ functionality.\nWhen off, Customize+ will not affect any characters.");

			ImGui.Separator();
			ImGui.Text("Characters:");

			ImGui.SameLine();
			ImGui.SetNextItemWidth(ImGui.GetWindowSize().X - 123);
			ImGui.LabelText(string.Empty, string.Empty);

			ImGui.SameLine();

			if (ImGui.BeginPopup("Add"))
			{
				ImGui.Text("Character Name:");
				ImGui.InputText(string.Empty, ref this.newScaleName, 1024);

				if (ImGui.Button("OK"))
				{
					BodyScale scale = new();
					scale.CharacterName = this.newScaleName;
					Plugin.Configuration.BodyScales.Add(scale);
					ImGui.CloseCurrentPopup();
				}

				ImGui.EndPopup();
			}

			if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
			{
				ImGui.OpenPopup("Add");
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip("Add a character");

			ImGui.BeginChild("scrolling", new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 56), false);

			for (int i = 0; i < config.BodyScales.Count; i++)
			{
				BodyScale bodyScale = config.BodyScales[i];

				ImGui.PushID(i);

				ImGui.SetNextItemWidth(ImGui.GetWindowSize().X - 94);
				string name = bodyScale.CharacterName ?? string.Empty;
				if (ImGui.InputText(string.Empty, ref name, 1024, ImGuiInputTextFlags.NoHorizontalScroll))
				{
					bodyScale.CharacterName = name;
				}

				if (ImGui.IsItemHovered())
					ImGui.SetTooltip($"The name of the character this body scale should apply to.");

				ImGui.SameLine();
				if (ImGuiComponents.IconButton(FontAwesomeIcon.Pen))
				{
					EditInterface.Show(bodyScale);
				}

				if (ImGui.IsItemHovered())
					ImGui.SetTooltip($"Edit body scale");

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

			if (file.Scale != null && file.Scale.X != 0 && file.Scale.Y != 0 && file.Scale.Z != 0)
			{
				scale.RootScale = new HkVector4(file.Scale.X, file.Scale.Y, file.Scale.Z, 1);
			}

			if (file.Bones == null)
				return;

			string name = Path.GetFileNameWithoutExtension(picker.FileName);

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
	}
}
