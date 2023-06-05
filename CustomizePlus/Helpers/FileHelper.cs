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
        public static string[] PromptUserForPath(string title, string? filter = null, string? defaultDir = null)
        {
            OpenFileDialog picker = new()
            {
                CheckFileExists = true,
                Title = title,
                Multiselect = true
            };

            if (filter != null)
            {
                picker.Filter = filter;
            }

            if (defaultDir != null && Directory.Exists(defaultDir))
            {
                picker.InitialDirectory = defaultDir;
            }

            var result = picker.ShowDialog();

            return result == DialogResult.OK
                ? picker.FileNames.Select(Path.GetFullPath).ToArray()
                : Array.Empty<string>();
        }

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