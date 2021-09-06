// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusWpfUi
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using CustomizePlusWpfUi.Serialization;
	using Serilog;
	using XivToolsWpf.DependencyInjection;

	public class LocalizationProvider : ILocaleProvider
	{
		private const string LocaleFolder = "./Languages/";

		private static LocalizationProvider? instance;

		private Dictionary<string, string> strings = new Dictionary<string, string>();

		private LocalizationProvider()
		{
			DependencyFactory.RegisterDependency<ILocaleProvider>(this);
		}

		public event LocalizationEvent? LocaleChanged;

		public bool Loaded => this.strings.Count > 0;

		public static void Load()
		{
			instance = new LocalizationProvider();

			string localeFile = "en.json";
			string json = File.ReadAllText(LocaleFolder + localeFile);

			try
			{
				instance.strings = SerializerService.Deserialize<Dictionary<string, string>>(json);
			}
			catch (Exception ex)
			{
				Log.Error(ex, $"Failed to load language: {localeFile}");
			}
		}

		public bool HasString(string key)
		{
			return this.strings.ContainsKey(key);
		}

		public string GetStringFormatted(string key, params string[] param)
		{
			string str = this.GetString(key);
			return string.Format(str, param);
		}

		public string GetStringAllLanguages(string key)
		{
			throw new NotImplementedException();
		}

		public string GetString(string key, bool silent = false)
		{
			string? val;
			if (!this.strings.TryGetValue(key, out val) || val == null)
			{
				if (!silent)
					Log.Error("Missing Localized string: \"" + key + "\"");

				return key;
			}

			return val;
		}
	}
}
