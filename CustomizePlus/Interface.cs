// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.IO;
	using System.Numerics;
	using System.Threading.Tasks;
	using System.Windows.Forms;
	using Anamnesis.Files;
	using CustomizePlus.Memory;
	using Dalamud.Logging;
	using ImGuiNET;
	using Newtonsoft.Json;

	public class Interface
	{
		public bool Visible = true;

		public void Show()
		{
			this.Visible = true;
		}

		public void Close()
		{
			this.Visible = false;
		}

		public void Draw()
		{
			if (!this.Visible)
				return;

			Configuration config = Plugin.Configuration;

			if (ImGui.Begin(
				"Customize+",
				ref this.Visible,
				ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
			{
				foreach(BodyScale bodyScale in config.BodyScales)
				{
					string name = bodyScale.CharacterName ?? string.Empty;
					ImGui.InputText(string.Empty, ref name, 1024);
					bodyScale.CharacterName = name;

					ImGui.SameLine();
					if (ImGui.Button("-", new(22, 22)))
					{
						config.BodyScales.Remove(bodyScale);
						break;
					}
				}

				if (ImGui.Button("Import Anamnesis Pose"))
				{
					try
					{
						this.Import();
					}
					catch (Exception ex)
					{
						PluginLog.Error(ex, "Failed to import Anamnesis pose");
						Plugin.ChatGui.PrintError(ex.Message);
					}
				}

				ImGui.SameLine();
				if (ImGui.Button("Save"))
				{
					config.Save();
					Plugin.LoadConfig();
				}
			}

			ImGui.End();
		}

		private void Import()
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

			if (file.Bones == null)
				return;

			string name = Path.GetFileNameWithoutExtension(picker.FileName);

			BodyScale scale = new();
			scale.CharacterName = name;

			foreach ((string boneName, PoseFile.Bone? bone) in file.Bones)
			{
				if (bone == null)
					continue;

				if (bone.Scale == null)
					continue;

				HkVector4 boneScale = new();
				boneScale.X = bone.Scale.X;
				boneScale.Y = bone.Scale.Y;
				boneScale.Z = bone.Scale.Z;
				scale.Bones.Add(boneName, boneScale);
			}

			Plugin.Configuration.BodyScales.Add(scale);
		}
	}
}
