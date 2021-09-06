// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusWpfUi
{
	using System;
	using System.Runtime.ExceptionServices;
	using System.Runtime.InteropServices;
	using Serilog;
	using Serilog.Core;
	using Serilog.Events;
	using XivToolsWpf.Dialogs;

	public class Logger : CustomizePlusLib.ILogger
	{
		public Logger()
		{
			LoggerConfiguration config = new LoggerConfiguration();
			config.WriteTo.File("log.txt");
			config.WriteTo.Sink<ErrorDialogLogDestination>();
			config.WriteTo.Debug();

			Serilog.Log.Logger = config.CreateLogger();

			Log.Information("OS: " + RuntimeInformation.OSDescription, "Info");
			Log.Information("Framework: " + RuntimeInformation.FrameworkDescription, "Info");
			Log.Information("OS Architecture: " + RuntimeInformation.OSArchitecture.ToString(), "Info");
			Log.Information("Process Architecture: " + RuntimeInformation.ProcessArchitecture.ToString(), "Info");
		}

		public void Error(Exception ex, string message) => Log.Error(ex, message);
		public void Information(string message) => Log.Information(message);

		private class ErrorDialogLogDestination : ILogEventSink
		{
			public void Emit(LogEvent logEvent)
			{
				if (logEvent.Level >= LogEventLevel.Error)
				{
					ErrorDialog.ShowError(ExceptionDispatchInfo.Capture(new Exception(logEvent.MessageTemplate.Text, logEvent.Exception)), logEvent.Level == LogEventLevel.Fatal);
				}
			}
		}
	}
}