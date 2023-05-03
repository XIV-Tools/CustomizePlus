// © Customize+.
// Licensed under the MIT license.

using Dalamud.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Numerics;

namespace CustomizePlus.Data.Profile
{
	/// <summary>
	/// Contains utilities for saving and loading <see cref="CharacterProfile"/>s to disk,
	/// as well as parsing profiles from previous plugin versions.
	/// </summary>
	public static class ProfileReaderWriter
	{
		#region Save/Load
		private static string CreatePath(string fileName)
		{
			System.IO.Directory.CreateDirectory(Plugin.Config.ProfileDirectory);

			return $"{Plugin.Config.ProfileDirectory}\\{fileName}";
		}

		public static void SaveProfile(CharacterProfile prof)
		{
			try
			{
				string json = JsonConvert.SerializeObject(prof, Formatting.Indented);

				var invalidCharacters = System.IO.Path.GetInvalidFileNameChars();
				string fileName = $"{prof.CharacterName}--{prof.ProfileName}.profile";
				fileName = String.Join(String.Empty, fileName.Split(invalidCharacters, StringSplitOptions.RemoveEmptyEntries));

				string filePath = CreatePath(fileName);

				File.WriteAllText(filePath, json);
			}
			catch (Exception ex)
			{
				PluginLog.LogError($"Error saving {prof}: {ex}");
			}
		}

		public static string[] GetProfilePaths()
		{
			return Directory.GetFiles(Plugin.Config.ProfileDirectory, "*.profile");
		}

		public static bool TryLoadProfile(string path, out CharacterProfile? prof)
		{
			try
			{
				if (System.IO.Path.Exists(path))
				{
					var file = JsonConvert.DeserializeObject<CharacterProfile>(File.ReadAllText(path));

					if (file != null)
					{
						prof = file;
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				PluginLog.LogError($"Error loading character profile (from '{path}'): {ex}");
			}

			prof = null;
			return false;
		}

		#endregion


		#region Converters

		public static void SaveConvertedProfiles(params CharacterProfile[] profs)
		{
			foreach (var prof in profs)
			{
				SaveProfile(prof);
			}
		}

		private static bool BackupOldConfig(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					string fileName = System.IO.Path.GetFileName(path);
					string backupPath = CreatePath(fileName);

					System.IO.File.Copy(path, backupPath);
					return true;
				}
			}
			catch (Exception ex)
			{
				PluginLog.LogError($"Error backing up file at '{path}': {ex}");
			}
			return false;
		}

		public static bool ConvertConfigVersion2()
		{
			string path = DalamudServices.PluginInterface.ConfigFile.FullName;
			if (!BackupOldConfig(path))
			{
				PluginLog.LogError("Error backing up config file. Aborting conversion...");
				return false;
			}

			string text = File.ReadAllText(path);

			var blob = Newtonsoft.Json.Linq.JObject.Parse(text);

			foreach (var bs in blob["BodyScales"])
			{
				CharacterProfile newProfile = new CharacterProfile()
				{
					CharacterName = bs.Value<string>("CharacterName") ?? String.Empty,
					ProfileName = bs.Value<string>("ScaleName") ?? String.Empty,
					Enabled = bs.Value<bool>("BodyScaleEnabled")
				};

				foreach (var bone in bs["Bones"])
				{
					string boneName = bone.ToString();
					Vector3 pos = new(
						bone["Position"].Value<float>("X"),
						bone["Position"].Value<float>("Y"),
						bone["Position"].Value<float>("Z"));
					Vector3 rot = new(
						bone["Rotation"].Value<float>("X"),
						bone["Rotation"].Value<float>("Y"),
						bone["Rotation"].Value<float>("Z"));
					Vector3 scale = new(
						bone["Scale"].Value<float>("X"),
						bone["Scale"].Value<float>("Y"),
						bone["Scale"].Value<float>("Z"));

					BoneTransform bec = new BoneTransform()
					{
						Translation = pos,
						EulerRotation = rot,
						Scaling = scale
					};

					newProfile.Bones[boneName] = bec;
				}

				SaveProfile(newProfile);
			}

			return true;
		}

		#endregion
	}
}
