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

        public bool PluginEnabled { get; set; } = true;

        public bool DebuggingModeEnabled { get; set; }

        /// <summary>
        /// Hides root position from the UI. DOES NOT DISABLE LOADING IT FROM THE CONFIG!
        /// </summary>
        public bool RootPositionEditingEnabled { get; set; }

        public HashSet<string> ViewedMessageWindows = new();
    }
}