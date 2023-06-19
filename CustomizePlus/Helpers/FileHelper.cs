// © Customize+.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Dalamud.Logging;

namespace CustomizePlus.Helpers
{
    internal static class FileHelper
    {
        public static string? ReadFileAtPath(string path)
        {
            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                return text;
            }

            PluginLog.LogError($"Tried to read file from path that doesn't exist: '{path}'");

            return null;
        }
    }
}