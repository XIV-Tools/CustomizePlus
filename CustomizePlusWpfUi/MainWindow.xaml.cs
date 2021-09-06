// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusWpfUi
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using CustomizePlus.GameStructs;
	using CustomizePlusLib.Anamnesis;
	using CustomizePlusLib.Mods;
	using PropertyChanged;
	using XivToolsWpf.Windows;

	/// <summary>
	/// Interaction logic for MainWindow.xaml.
	/// </summary>
	public partial class MainWindow : ChromedWindow
	{
		private bool isEnabled = false;

		public MainWindow()
		{
			this.InitializeComponent();
			this.ContentArea.DataContext = this;

			Entry entry = new Entry();
			this.Entries.Add(entry);
		}

		public ObservableCollection<Entry> Entries { get; set; } = new ObservableCollection<Entry>();

		public bool Enabled
		{
			get => this.isEnabled;
			set
			{
				if (this.isEnabled == value)
					return;

				this.isEnabled = value;

				if (value)
				{
					App.CustomizePlus?.Start();
				}
				else
				{
					App.CustomizePlus?.Stop();
				}
			}
		}

		[AddINotifyPropertyChangedInterface]
		[Serializable]
		public class Entry
		{
			private ModBase? mod;

			public string Name { get; set; } = string.Empty;

			// Anamnesis pose scales
			public PoseFile? Pose { get; set; }

			// custom scale
			public Dictionary<int, Vector>? BodyBoneScales { get; set; }
			public Dictionary<int, Vector>? HeadBoneScales { get; set; }

			public void Apply()
			{
				if (this.mod != null)
					App.CustomizePlus?.RemoveModification(this.mod);

				if (this.Pose != null)
				{
					this.mod = new AnamnesisPoseBoneScaleMod(this.Name, this.Pose);
				}
				else if (this.BodyBoneScales != null && this.HeadBoneScales != null)
				{
					SimpleBoneScaleMod simpleMod = new SimpleBoneScaleMod(this.Name);
					simpleMod.BodyBoneScales = this.BodyBoneScales;
					simpleMod.HeadBoneScales = this.HeadBoneScales;
					this.mod = simpleMod;
				}
				else
				{
					throw new Exception("Unknown mod type");
				}

				App.CustomizePlus?.AddModification(this.mod);
			}
		}
	}
}
