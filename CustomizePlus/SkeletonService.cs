// © Anamnesis.
// Developed by W and A Walsh.
// Licensed under the MIT license.

namespace Anamnesis.PoseModule
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Anamnesis.Memory;
	using Anamnesis.Posing.Templates;
	using Anamnesis.Serialization;

	public static class SkeletonService
	{
		private static List<SkeletonFile> skeletonFiles = new List<SkeletonFile>();

		public static void LoadSkeletons()
		{
			string[] templates = Directory.GetFiles("Data/Skeletons/", "*.json");
			foreach (string templatePath in templates)
			{
				Load(templatePath);
			}
		}

		public static SkeletonFile? GetSkeletonFile(Appearance customize)
		{
			int maxDepth = int.MinValue;
			SkeletonFile? maxSkel = null;

			foreach (SkeletonFile template in skeletonFiles)
			{
				if (template.IsValid(customize))
				{
					if (template.Depth > maxDepth)
					{
						maxDepth = template.Depth;
						maxSkel = template;
					}
				}
			}

			if (maxSkel != null)
				return maxSkel;

			return null;
		}

		private static SkeletonFile Load(string path)
		{
			SkeletonFile template = SerializerService.DeserializeFile<SkeletonFile>(path);
			skeletonFiles.Add(template);

			if (template.BasedOn != null)
			{
				SkeletonFile baseTemplate = Load("Data/Skeletons/" + template.BasedOn);
				template.CopyBaseValues(baseTemplate);
			}

			// Validate that all bone names are unique
			if (template.BoneNames != null)
			{
				HashSet<string> boneNames = new HashSet<string>();

				foreach ((string orignal, string name) in template.BoneNames)
				{
					if (boneNames.Contains(name))
						throw new Exception($"Duplicate bone name: {name} in skeleton file: {path}");

					boneNames.Add(name);
				}
			}

			return template;
		}
	}
}
