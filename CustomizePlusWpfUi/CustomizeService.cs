// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusWpfUi
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using CustomizePlusLib.Anamnesis;
	using CustomizePlusWpfUi.Serialization;

	public static class CustomizeService
	{
		private static readonly Dictionary<string, PoseFile> PoseFiles = new();

		public static void LoadCustomizers()
		{
			string[] templates = Directory.GetFiles("Poses/", "*.pose");
			foreach (string posePath in templates)
			{
				Load(posePath);
			}
		}

		public static PoseFile? GetPose(string actorName)
		{
			if (PoseFiles.ContainsKey(actorName))
				return PoseFiles[actorName];

			return null;
		}

		private static void Load(string path)
		{
			PoseFile template = SerializerService.DeserializeFile<PoseFile>(path);
			string name = Path.GetFileNameWithoutExtension(path);
			PoseFiles.Add(name, template);
		}
	}
}
