// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Data.Configuration.Interfaces
{
    internal interface ILegacyConfiguration
    {
        ILegacyConfiguration? LoadFromFile(string path);
        PluginConfiguration ConvertToLatestVersion();
    }
}