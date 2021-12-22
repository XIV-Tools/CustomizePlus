// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
    using System;
    using Dalamud.Configuration;
    using Dalamud.Plugin;

    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }
    }
}
