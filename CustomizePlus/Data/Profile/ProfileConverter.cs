// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CustomizePlus.Anamnesis;
using CustomizePlus.Data.Configuration.Version0;
using CustomizePlus.Data.Configuration.Version2;
using CustomizePlus.Extensions;
using Newtonsoft.Json;

namespace CustomizePlus.Data.Profile
{
    public static class ProfileConverter
    {
        public static CharacterProfile? ConvertFromAnamnesis(string json, string? profileName = null)
        {
            JsonSerializerSettings settings = new()
            {
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter> { new PoseFile.VectorConverter() }
            };

            var pose = JsonConvert.DeserializeObject<PoseFile>(json, settings);

            if (pose == null)
            {
                throw new Exception("Failed to deserialize pose file");
            }

            if (pose.Bones == null)
            {
                return null;
            }

            var output = new CharacterProfile();

            if (profileName != null)
            {
                output.CharacterName = string.Empty;
                output.ProfileName = profileName;
            }

            //load up all the valid bones, but skip root

            var validBones = pose.Bones.Where(x =>
                x.Key != Constants.RootBoneName
                && x.Value != null
                && x.Value.Scale != null);

            foreach (var kvp in validBones)
            {
                var bt = new BoneTransform
                {
                    Scaling = kvp.Value.Scale!.GetAsNumericsVector()
                };

                output.Bones[kvp.Key] = bt;
            }

            //load up root, but check it more rigorously

            var validRoot = pose.Bones.TryGetValue(Constants.RootBoneName, out var root)
                            && root != null
                            && root.Scale != null
                            && root.Scale.GetAsNumericsVector() != Vector3.Zero
                            && root.Scale.GetAsNumericsVector() != Vector3.One;

            if (validRoot)
            {
                output.Bones[Constants.RootBoneName] = new BoneTransform
                {
                    Scaling = root.Scale!.GetAsNumericsVector()
                };
            }

            return output;
        }

        public static CharacterProfile? ConvertFromConfigV2(string json)
        {
            var oldVer = JsonConvert.DeserializeObject<Version2BodyScale>(json);
            return oldVer != null ? ConvertFromConfigV2(oldVer) : null;
        }

        public static CharacterProfile ConvertFromConfigV2(Version2BodyScale oldVer)
        {
            CharacterProfile newProfile = new()
            {
                CharacterName = oldVer.CharacterName,
                ProfileName = oldVer.ScaleName,
                Enabled = oldVer.BodyScaleEnabled
            };

            foreach (var kvp in oldVer.Bones)
            {
                var novelValues = kvp.Value.Position != Vector3.Zero
                                  || kvp.Value.Rotation != Vector3.Zero
                                  || kvp.Value.Scale != Vector3.One;

                if (novelValues || kvp.Key == Constants.RootBoneName)
                {
                    newProfile.Bones[kvp.Key] = new BoneTransform
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
            var oldVer = JsonConvert.DeserializeObject<Version0BodyScale>(json);
            return oldVer != null ? ConvertFromConfigV0(oldVer) : null;
        }

        public static CharacterProfile ConvertFromConfigV0(Version0BodyScale oldVer)
        {
            var newProfile = new CharacterProfile
            {
                CharacterName = oldVer.CharacterName,
                ProfileName = oldVer.ScaleName,
                Enabled = oldVer.BodyScaleEnabled
            };

            foreach (var kvp in oldVer.Bones)
            {
                var newScaling = Vector3.One;

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
                    newProfile.Bones[kvp.Key] = new BoneTransform
                    {
                        Scaling = newScaling
                    };
                }
            }

            return newProfile;
        }
    }
}