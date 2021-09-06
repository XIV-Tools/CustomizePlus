// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusWpfUi
{
	using System.Windows;
	using System.Windows.Controls;
	using CustomizePlusLib.Anamnesis;
	using CustomizePlusWpfUi.Serialization;
	using Microsoft.Win32;

	public partial class EntryListItem : UserControl
	{
		public EntryListItem()
		{
			this.InitializeComponent();
		}

		private void OnEditClicked(object sender, RoutedEventArgs e)
		{
			if (this.DataContext is MainWindow.Entry entry)
			{
				entry.Pose = null;
				entry.Apply();
			}
		}

		private void OnImportClicked(object sender, RoutedEventArgs e)
		{
			if (this.DataContext is MainWindow.Entry entry)
			{
				OpenFileDialog dlg = new OpenFileDialog();
				dlg.Filter = "Anamnesis Poses (*.pose)|*.pose";

				if (dlg.ShowDialog() != true)
					return;

				PoseFile poseFile = SerializerService.DeserializeFile<PoseFile>(dlg.FileName);

				entry.BodyBoneScales?.Clear();
				entry.HeadBoneScales?.Clear();
				entry.Pose = poseFile;

				entry.Apply();
			}
		}
	}
}
