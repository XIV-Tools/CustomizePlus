using System;
using System.Collections.Generic;
using System.IO;
using Anamnesis.Files;
using Anamnesis.Serialization;

namespace ConsoleApp1
{
	public static class CustomizeService
	{
		private static Dictionary<string, PoseFile> poseFiles = new Dictionary<string, PoseFile>();

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
			if (poseFiles.ContainsKey(actorName))
				return poseFiles[actorName];

			return null;
		}

		private static void Load(string path)
		{
			PoseFile template = SerializerService.DeserializeFile<PoseFile>(path);
			string name = Path.GetFileNameWithoutExtension(path);
			poseFiles.Add(name, template);
		}
	}
}
