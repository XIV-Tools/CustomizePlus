// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusWpfUi
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Threading;
	using CustomizePlusLib;
	using Serilog;
	using XivToolsWpf;

	/// <summary>
	/// Interaction logic for App.xaml.
	/// </summary>
	public partial class App : Application
	{
		public static CustomizePlusApi? CustomizePlus;
		private Memory? memory;
		private Logger? logger;

		protected override void OnStartup(StartupEventArgs e)
		{
			AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;
			this.Dispatcher.UnhandledException += this.DispatcherOnUnhandledException;
			Application.Current.DispatcherUnhandledException += this.CurrentOnDispatcherUnhandledException;
			TaskScheduler.UnobservedTaskException += this.TaskSchedulerOnUnobservedTaskException;
			this.Exit += this.OnExit;

			base.OnStartup(e);

			LocalizationProvider.Load();
			Themes.ApplySystemTheme();

			Process process;
			try
			{
				process = Process.GetProcessesByName("ffxiv_dx11")[0];
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to locate FFXIV DX11 process");
				return;
			}

			this.memory = new Memory(process);
			this.logger = new Logger();

			CustomizePlus = new CustomizePlusApi(this.memory, this.logger);
		}

		protected void OnExit(object sender, ExitEventArgs e)
		{
			CustomizePlus?.Stop();
		}

		private void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
		{
			if (e.Exception == null)
				return;

			Log.Fatal(e.Exception, e.Exception.Message);
		}

		private void CurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			Log.Fatal(e.Exception, e.Exception.Message);
		}

		private void DispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			Log.Fatal(e.Exception, e.Exception.Message);
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Exception? ex = e.ExceptionObject as Exception;

			if (ex == null)
				return;

			Log.Fatal(ex, ex.Message);
		}
	}
}