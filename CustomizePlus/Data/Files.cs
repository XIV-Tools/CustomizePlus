// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CustomizePlus.Services;
using Dalamud.Interface.Internal;
using ImGuiScene;

namespace CustomizePlus.Data
{
    public static class Files
    {
        private static readonly List<IDisposable> LoadedResources = new();
        private static readonly Dictionary<string, IDalamudTextureWrap> TextureCache = new();

        public static IDalamudTextureWrap Icon => LoadImage("icon.png");

        public static void Dispose()
        {
            foreach (var resource in LoadedResources)
            {
                resource.Dispose();
            }

            LoadedResources.Clear();
        }

        private static IDalamudTextureWrap LoadImage(string file)
        {
            if (TextureCache.TryGetValue(file, out var image))
                return image;

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

            return assemblyDirectory == null
                ? throw new Exception("Failed to get executing assembly location")
                : Path.Combine(assemblyDirectory, file);
        }
    }
}