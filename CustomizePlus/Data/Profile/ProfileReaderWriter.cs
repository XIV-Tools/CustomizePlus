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

		private static string CreateFileName(CharacterProfile prof)
		{
			var invalidCharacters = Path.GetInvalidFileNameChars();
			string fileName = $"{prof.CharName}-{prof.ProfName}-{prof.UniqueID}.profile";
			fileName = String.Join(String.Empty, fileName.Split(invalidCharacters, StringSplitOptions.RemoveEmptyEntries));
			return fileName;
		}

		private static string CreatePath(string fileName)
		{
			Directory.CreateDirectory(Plugin.ConfigurationManager.GetProfileDirectory());

			return Path.GetFullPath($"{Plugin.Config.ProfileDirectory}\\{fileName}");
		}

		public static void SaveProfile(CharacterProfile prof, bool archival = false)
		{
			try
			{
				string oldFilePath = prof.OriginalFilePath ?? string.Empty;
				string newFilePath = CreatePath(CreateFileName(prof));

				if (!archival)
				{
					string json = JsonConvert.SerializeObject(prof, Formatting.Indented);

					File.WriteAllText(newFilePath, json);
				}
				else
				{
					newFilePath += "_arch";
					string text = Helpers.Base64Helper.ExportToBase64(prof, Constants.ConfigurationVersion);

					File.WriteAllText(newFilePath, text);
				}

				if (newFilePath != oldFilePath && oldFilePath != String.Empty)
				{
					File.Delete(oldFilePath);
				}
				
				prof.OriginalFilePath = newFilePath;
			}
			catch (Exception ex)
			{
				PluginLog.LogError($"Error saving {prof}: {ex}");
			}
		}

		public static void DeleteProfile(CharacterProfile prof)
		{
			//SaveProfile(prof, true);
			if (CreatePath(CreateFileName(prof)) is string path && File.Exists(path))
			{
				File.Delete(path);
			}
		}

		public static string[] GetProfilePaths()
		{
			if (Directory.Exists(Plugin.Config.ProfileDirectory))
			{
				return Directory.GetFiles(Plugin.Config.ProfileDirectory, "*.profile");
			}
			else
			{
				Directory.CreateDirectory(Plugin.Config.ProfileDirectory);
				return Array.Empty<string>();
			}
		}

		public static bool TryLoadProfile(string path, out CharacterProfile? prof)
		{
			try
			{
				if (Path.Exists(path))
				{
					var file = JsonConvert.DeserializeObject<CharacterProfile>(File.ReadAllText(path));

					if (file != null)
					{
						prof = file;
						prof.OriginalFilePath = path;
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
	}
}
