// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Data.Profile;
using System.IO;

namespace CustomizePlus.Anamnesis
{
	public static class Importer
	{
		private const string PoseFileFilter = "Anamnesis Pose (*.pose)|*.pose";
		private const string FilePickerTitle = "Customize+ - Import Anamnesis Pose";

		//TODO double-check this is the actual default
		private const string DefaultDirectory = @"%USERPROFILE%\Documents\Anamnesis\Poses";

		private static readonly string FullPath = System.Environment.ExpandEnvironmentVariables(DefaultDirectory);

		public static void ImportFiles(ProfileManager destination)
		{
			string[] selectedPaths = Helpers.FileHelper.PromptUserForPath(FilePickerTitle, PoseFileFilter, FullPath);

			foreach (var path in selectedPaths)
			{
				string? json = Helpers.FileHelper.ReadFileAtPath(path);

				if (json != null)
				{
					string profileName = Path.GetFileNameWithoutExtension(path);
					CharacterProfile? import = ProfileConverter.ConvertFromAnamnesis(json, profileName);

					if (import != null)
					{
						destination.AddAndSaveProfile(import);
					}
					else
					{
						Dalamud.Logging.PluginLog.LogError($"Error parsing character profile from anamnesis pose file at '{path}'");
					}
				}
			}
		}
	}
}
