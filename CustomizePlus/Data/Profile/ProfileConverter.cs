// © Customize+.
// Licensed under the MIT license.

using Anamnesis.Files;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomizePlus.Extensions;
using System.Numerics;

namespace CustomizePlus.Data.Profile
{
	public static class ProfileConverter
	{
		public static CharacterProfile? ConvertFromAnamnesis(string json, string? profileName = null)
		{
			JsonSerializerSettings settings = new()
			{
				NullValueHandling = NullValueHandling.Ignore,
				Converters = new List<JsonConverter>() { new PoseFile.VectorConverter() }
			};

			PoseFile? pose = JsonConvert.DeserializeObject<PoseFile>(json, settings);

			if (pose == null)
			{
				throw new Exception("Failed to deserialize pose file");
			}
			else if (pose.Bones == null)
			{
				return null;
			}

			CharacterProfile output = new CharacterProfile();

			if (profileName != null)
			{
				output.CharName = String.Empty;
				output.ProfName = profileName;
			}

			//load up all the valid bones, but skip root

			var validBones = pose.Bones.Where(x =>
				   x.Key != Constants.RootBoneName
				&& x.Value != null
				&& x.Value.Scale != null);

			foreach (var kvp in validBones)
			{
				var bt = new BoneTransform()
				{
					Scaling = kvp.Value.Scale.GetAsNumericsVector()
				};

				output.Bones[kvp.Key] = bt;
			}

			//load up root, but check it more rigorously

			bool validRoot = pose.Bones.TryGetValue(Constants.RootBoneName, out var root)
				&& root != null
				&& root.Scale != null
				&& root.Scale.GetAsNumericsVector() != System.Numerics.Vector3.Zero
				&& root.Scale.GetAsNumericsVector() != System.Numerics.Vector3.One;

			if (validRoot)
			{
				output.Bones[Constants.RootBoneName] = new BoneTransform()
				{
					Scaling = root.Scale.GetAsNumericsVector()
				};
			}

			return output;
		}

		public static CharacterProfile? ConvertFromConfigV2(string json)
		{
			var oldVer = JsonConvert.DeserializeObject<Configuration.Version2.Version2BodyScale>(json);
			if (oldVer != null)
			{
				return ConvertFromConfigV2(oldVer);
			}
			return null;
		}
		public static CharacterProfile ConvertFromConfigV2(Configuration.Version2.Version2BodyScale oldVer)
		{
			CharacterProfile newProfile = new()
			{
				CharName = oldVer.CharacterName,
				ProfName = oldVer.ScaleName,
				Enabled = oldVer.BodyScaleEnabled
			};

			foreach (var kvp in oldVer.Bones)
			{
				bool novelValues = kvp.Value.Position != Vector3.Zero
					|| kvp.Value.Rotation != Vector3.Zero
					|| kvp.Value.Scale != Vector3.One;

				if (novelValues || kvp.Key == Constants.RootBoneName)
				{
					newProfile.Bones[kvp.Key] = new BoneTransform()
					{
						Translation = kvp.Value.Position,
						Rotation = kvp.Value.Rotation,
						Scaling = kvp.Value.Scale
					};
				}
			}

			return newProfile;
		}

		public static CharacterProfile? ConvertFromConfigV0(string json)
		{
			var oldVer = JsonConvert.DeserializeObject<Configuration.Version0.Version0BodyScale>(json);
			if (oldVer != null)
			{
				return ConvertFromConfigV0(oldVer);
			}
			return null;
		}
		public static CharacterProfile ConvertFromConfigV0(Configuration.Version0.Version0BodyScale oldVer)
		{
			CharacterProfile newProfile = new CharacterProfile()
			{
				CharName = oldVer.CharacterName,
				ProfName = oldVer.ScaleName,
				Enabled = oldVer.BodyScaleEnabled
			};

			foreach (var kvp in oldVer.Bones)
			{
				Vector3 newScaling = Vector3.One;

				if (kvp.Value.W != 0)
				{
					newScaling *= kvp.Value.W;
				}
				else
				{
					newScaling = new Vector3(kvp.Value.X, kvp.Value.Y, kvp.Value.Z);
				}

				if (newScaling != Vector3.One || kvp.Key == Constants.RootBoneName)
				{
					newProfile.Bones[kvp.Key] = new BoneTransform()
					{
						Scaling = newScaling
					};
				}
			}

			return newProfile;
		}
	}
}
