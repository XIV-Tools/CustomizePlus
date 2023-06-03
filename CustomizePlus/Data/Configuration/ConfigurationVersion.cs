// © Customize+.
// Licensed under the MIT license.

using Dalamud.Configuration;

namespace CustomizePlus.Data.Configuration
{
    /// <summary>
    ///     Wrapper object which allows us to read configuration version no matter the actual configuration file contents
    /// </summary>
    public class ConfigurationVersion : IPluginConfiguration
    {
        public int Version { get; set; }
    }
}