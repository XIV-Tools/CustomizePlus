// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using CustomizePlus.Data.Configuration.Interfaces;
using CustomizePlus.Data.Profile;
using Dalamud.Configuration;
using Newtonsoft.Json;

namespace CustomizePlus.Data.Configuration.Version2
{
    internal class Version2Configuration : IPluginConfiguration, ILegacyConfiguration
    {
        public List<Version2BodyScale> BodyScales { get; set; } = new();
        public bool Enable { get; set; } = true;
        public bool AutomaticEditMode { get; set; } = false;
        public bool MirrorMode { get; set; } = false;
        public BoneAttribute EditingAttribute { get; set; } = 0;

        public bool ApplyToNpcs { get; set; } = true;
        public bool ApplyToNpcsInCutscenes { get; set; } = true;
        public bool DebuggingMode { get; set; } = false;

        public ILegacyConfiguration? LoadFromFile(string path)
        {
            return !Path.Exists(path)
                ? throw new ArgumentException("Specified config path is invalid")
                : (ILegacyConfiguration?)JsonConvert.DeserializeObject<Version2Configuration>(File.ReadAllText(path));
        }

        public PluginConfiguration ConvertToLatestVersion()
        {
            var config = new PluginConfiguration
            {
                Version = PluginConfiguration.CurrentVersion,
                PluginEnabled = Enable,
                DebuggingModeEnabled = DebuggingMode,
                ApplyToNPCs = ApplyToNpcs,
                ApplyToNPCsInCutscenes = ApplyToNpcsInCutscenes
            };

            foreach (var bodyScale in BodyScales)
            {
                var newProf = ProfileConverter.ConvertFromConfigV2(bodyScale);

                ProfileManager.ConvertedProfiles.Add(newProf);
            }

            return config;
        }

        public int Version { get; set; } = 2;
    }
}