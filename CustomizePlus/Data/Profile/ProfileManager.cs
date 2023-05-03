// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Data.Profile
{
	/// <summary>
	/// Container class for administrating <see cref="CharacterProfile"/>s during runtime.
	/// </summary>
	public static class ProfileManager
	{
		public static readonly HashSet<CharacterProfile> Profiles = new(new ProfileEquality());

		public static void LoadProfiles()
		{
			foreach(string path in ProfileReaderWriter.GetProfilePaths())
			{
				if (ProfileReaderWriter.TryLoadProfile(path, out var prof) && prof != null)
				{
					Profiles.Add(prof);
				}
			}
		}

		public static void SaveProfile(CharacterProfile prof)
		{
			//if the profile is already in the list, simply replace it
			if (Profiles.Remove(prof))
			{
				Profiles.Add(prof);
				ProfileReaderWriter.SaveProfile(prof);
			}
			else
			{
				//otherwise it must be a new profile, obviously
				//in which case we update its creation date
				//(which incidentally prevents it from inheriting an old hash code)

				prof.CreationDate = DateTime.Now;
				ProfileReaderWriter.SaveProfile(prof);
			}
		}

		public static void SaveAllProfiles()
		{
			foreach(var prof in Profiles)
			{
				ProfileReaderWriter.SaveProfile(prof);
			}
		}

	}
}
