// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Data.Configuration.Interfaces;
using CustomizePlus.Helpers;
using Dalamud.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace CustomizePlus.Data.Configuration.Version0
{
	internal class Version0Configuration : IPluginConfiguration, ILegacyConfiguration
	{
		public int Version { get; set; } = 0;
		public List<Version0BodyScale> BodyScales { get; set; } = new();
		public bool Enable { get; set; } = true;
		public bool AutomaticEditMode { get; set; } = false;

		public bool ApplyToNpcs { get; set; } = true;

		public bool ApplyToNpcsInCutscenes { get; set; } = true;

		public bool DebuggingMode { get; set; } = false;

		public static ILegacyConfiguration LoadFromFile(string path)
		{
			if (!Path.Exists(path))
				throw new ArgumentException("Specified config path is invalid");

			return JsonConvert.DeserializeObject<Version0Configuration>(File.ReadAllText(path));
		}

		public PluginConfiguration ConvertToLatestVersion()
		{
			PluginConfiguration configuration = new PluginConfiguration
			{
				Version = PluginConfiguration.CurrentVersion,
				Enable = Enable,
				AutomaticEditMode = AutomaticEditMode,
				ApplyToNpcs = ApplyToNpcs,
				ApplyToNpcsInCutscenes = ApplyToNpcsInCutscenes,
				DebuggingMode = DebuggingMode,
				BodyScales = new(BodyScales.Count)
			};

			foreach (var bodyScale in BodyScales)
			{
				BodyScale newBodyScale = new BodyScale
				{
					ScaleName = bodyScale.ScaleName,
					BodyScaleEnabled = bodyScale.BodyScaleEnabled,
					CharacterName = bodyScale.CharacterName
				};

				foreach (var kvPair in bodyScale.Bones)
				{
					BoneEditsContainer boneEditsContainer = new BoneEditsContainer
					{
						Position = Constants.ZeroVector,
						Rotation = Constants.ZeroVector,
						Scale = Constants.OneVector
					};

					if (kvPair.Value.W != 0)
						boneEditsContainer.Scale = new Vector3(kvPair.Value.W, kvPair.Value.W, kvPair.Value.W);
					else if (kvPair.Value.X != 0 || kvPair.Value.Y != 0 || kvPair.Value.Z != 0)
						boneEditsContainer.Scale = new Vector3(kvPair.Value.X, kvPair.Value.Y, kvPair.Value.Z);

					newBodyScale.Bones.Add(kvPair.Key, boneEditsContainer);
				}

				newBodyScale.Bones["n_root"] = new BoneEditsContainer
				{
					Position = Constants.ZeroVector,
					Rotation = Constants.ZeroVector
				};

				if (bodyScale.RootScale.W != 0)
					newBodyScale.Bones["n_root"].Scale = new Vector3(bodyScale.RootScale.W, bodyScale.RootScale.W, bodyScale.RootScale.W);
				else if (bodyScale.RootScale.X != 0 || bodyScale.RootScale.Y != 0 || bodyScale.RootScale.Z != 0)
					newBodyScale.Bones["n_root"].Scale = new Vector3(bodyScale.RootScale.X, bodyScale.RootScale.Y, bodyScale.RootScale.Z);

				configuration.BodyScales.Add(newBodyScale);
			}

			return configuration;
		}
	}
}
