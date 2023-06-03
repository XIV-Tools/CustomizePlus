// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace CustomizePlus.Data.Configuration
{
    [Serializable]
    public class PluginConfiguration : IPluginConfiguration
    {
        public const int CurrentVersion = Constants.ConfigurationVersion;

        public int Version { get; set; } = CurrentVersion;

        //public HashSet<Data.Profile.CharacterProfile> Profiles { get; set; } = new(); //replace with new profile data structure
        public bool PluginEnabled { get; set; } = true;

        public bool ApplytoNPCs { get; set; } = false;
        public bool ApplytoNPCsInCutscenes { get; set; } = false;

        //public RenderRulesManager DefaultRules { get; set; } = new();
        public bool ApplyProfilesInCutscenes { get; set; } = true; //global override? maybe remove
        public int CapSkeletonsRendered { get; set; } = -1; //TODO rename this, please


        //public bool AutomaticEditMode { get; set; } = false; //should belong to editor session
        //public bool MirrorMode { get; set; } = false; //should belong to editor session
        //public bool ParentingMode { get; set; } = false; //should belong to editor session
        //public EditMode EditingAttribute { get; set; } = EditMode.Scale; //should be local to the editor window


        public bool DebuggingMode { get; set; } = false;


        public HashSet<string> ViewedMessageWindows = new();

        // Upcoming feature
        /*
        public bool GroupByScale { get; set; } = false;
        public bool GroupByCharacter { get; set; } = false;
        */
    }
}