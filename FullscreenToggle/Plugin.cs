// © XIV-Tools.
// Licensed under the MIT license.

namespace FullscreenToggle;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;

public sealed class Plugin : IDalamudPlugin
{
	private const int GwlStyle = -16;
	private const int WsChild = 0x40000000;
	private const int WsBorder = 0x00800000;
	private const int WlDlgFrame = 0x00400000;
	private const int WsCaption = WsBorder | WlDlgFrame;

	private readonly IntPtr wnd;
	private bool isDown = false;
	private bool isWindowed = true;

	public Plugin()
	{
		try
		{
			Process process = Process.GetCurrentProcess();
			this.wnd = process.MainWindowHandle;

			Framework.Update += this.OnUpdate;
		}
		catch (Exception ex)
		{
			PluginLog.Error(ex, "Error instantiating plugin");
		}
	}

	[PluginService][RequiredVersion("1.0")] public static Framework Framework { get; private set; } = null!;

	public string Name => "Fullscreen Toggle";

	public void Dispose()
	{
		Framework.Update -= this.OnUpdate;
	}

	public void Toggle()
	{
		if (this.isWindowed)
		{
			this.Borderless();
		}
		else
		{
			this.Windowed();
		}

		this.isWindowed = !this.isWindowed;
	}

	public void Borderless()
	{
		PluginLog.Information("Swap to borderless");
		int style = GetWindowLong(this.wnd, GwlStyle);
		SetWindowLong(this.wnd, GwlStyle, style & ~WsCaption);
		ShowWindow(this.wnd, 3);
	}

	public void Windowed()
	{
		PluginLog.Information("Swap to windowed");
		int style = GetWindowLong(this.wnd, GwlStyle);
		SetWindowLong(this.wnd, GwlStyle, style | WsCaption);
		ShowWindow(this.wnd, 9);
	}

	[DllImport("user32.DLL")]
	private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

	[DllImport("user32.DLL")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.DLL")]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	[DllImport("user32.dll")]
	private static extern short GetKeyState(int nVirtKey);

	private void OnUpdate(Framework framework)
	{
		IntPtr foregroundWnd = GetForegroundWindow();

		if (foregroundWnd != this.wnd)
			return;

		int alt = GetKeyState(0x12) & 0x80;
		int enter = GetKeyState(0x0D) & 0x80;
		bool isDown = alt == 128 && enter == 128;

		if (isDown != this.isDown)
		{
			this.isDown = isDown;

			if (this.isDown)
			{
				this.Toggle();
			}
		}
	}
}

