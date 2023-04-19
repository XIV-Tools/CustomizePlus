// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Util
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using ImGuiScene;

	public static class Files
	{
		private static readonly List<IDisposable> LoadedResources = new List<IDisposable>();
		private static readonly Dictionary<string, TextureWrap> TextureCache = new Dictionary<string, TextureWrap>();

		public static TextureWrap Icon => LoadImage("icon.png");

		public static void Dispose()
		{
			foreach (IDisposable? resource in LoadedResources)
			{
				resource.Dispose();
			}

			LoadedResources.Clear();
		}

		private static TextureWrap LoadImage(string file)
		{
			if (TextureCache.ContainsKey(file))
				return TextureCache[file];

			TextureWrap tex = DalamudServices.PluginInterface.UiBuilder.LoadImage(file);
			LoadedResources.Add(tex);
			TextureCache.Add(file, tex);
			return tex;
		}

		private static string GetFullPath(string file)
		{
			string? assemblyLocation = Assembly.GetExecutingAssembly().Location;

			if (assemblyLocation == null)
				throw new Exception("Failed to get executing assembly location");

			string? assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

			if (assemblyDirectory == null)
				throw new Exception("Failed to get executing assembly location");

			return Path.Combine(assemblyDirectory, file);
		}
	}
}
