// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ImGuiScene;

namespace CustomizePlus.Data
{
    public static class Files
    {
        private static readonly List<IDisposable> LoadedResources = new();
        private static readonly Dictionary<string, TextureWrap> TextureCache = new();

        public static TextureWrap Icon => LoadImage("icon.png");

        public static void Dispose()
        {
            foreach (var resource in LoadedResources)
            {
                resource.Dispose();
            }

            LoadedResources.Clear();
        }

        private static TextureWrap LoadImage(string file)
        {
            if (TextureCache.ContainsKey(file))
            {
                return TextureCache[file];
            }

            var tex = DalamudServices.PluginInterface.UiBuilder.LoadImage(file);
            LoadedResources.Add(tex);
            TextureCache.Add(file, tex);
            return tex;
        }

        private static string GetFullPath(string file)
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;

            if (assemblyLocation == null)
            {
                throw new Exception("Failed to get executing assembly location");
            }

            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

            if (assemblyDirectory == null)
            {
                throw new Exception("Failed to get executing assembly location");
            }

            return Path.Combine(assemblyDirectory, file);
        }
    }
}