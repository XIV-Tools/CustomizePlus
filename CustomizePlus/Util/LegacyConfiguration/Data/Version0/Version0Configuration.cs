// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Helpers;
using CustomizePlus.Interface.LegacyConfiguration.Data;
using Dalamud.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Util.LegacyConfiguration.Data.Version0
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

		public Configuration ConvertToLatestVersion()
		{
			Configuration configuration = new Configuration
			{
				Version = Configuration.CurrentVersion,
				Enable = Enable,
				AutomaticEditMode = AutomaticEditMode,
				ApplyToNpcs = ApplyToNpcs,
				ApplyToNpcsInCutscenes = ApplyToNpcsInCutscenes,
				DebuggingMode = DebuggingMode,
				BodyScales = new List<BodyScale>(BodyScales.Count)
			};

			foreach(var bodyScale in BodyScales)
			{
				BodyScale newBodyScale = new BodyScale
				{
					ScaleName = bodyScale.ScaleName,
					BodyScaleEnabled = bodyScale.BodyScaleEnabled,
					CharacterName = bodyScale.CharacterName
				};

				foreach(var kvPair in bodyScale.Bones)
				{
					BoneEditsContainer boneEditsContainer = new BoneEditsContainer
					{
						Position = MathHelpers.ZeroVector,
						Rotation = MathHelpers.ZeroVector,
						Scale = kvPair.Value.W != 0 ? new Vector3(kvPair.Value.W, kvPair.Value.W, kvPair.Value.W) : new Vector3(kvPair.Value.X, kvPair.Value.Y, kvPair.Value.Z)
					};

					newBodyScale.Bones.Add(kvPair.Key, boneEditsContainer);
				}

				newBodyScale.Bones["n_root"] = new BoneEditsContainer
				{
					Position = MathHelpers.ZeroVector,
					Rotation = MathHelpers.ZeroVector,
					Scale = bodyScale.RootScale.W != 0 ? new Vector3(bodyScale.RootScale.W, bodyScale.RootScale.W, bodyScale.RootScale.W) : new Vector3(bodyScale.RootScale.X, bodyScale.RootScale.Y, bodyScale.RootScale.Z)
				};

				configuration.BodyScales.Add(newBodyScale);
			}

			return configuration;
		}
	}
}
