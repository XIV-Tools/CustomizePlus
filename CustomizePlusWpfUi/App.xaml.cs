// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusWpfUi
{
	using System.Windows;
	using XivToolsWpf;

	/// <summary>
	/// Interaction logic for App.xaml.
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			Themes.ApplySystemTheme();
		}
	}
}
