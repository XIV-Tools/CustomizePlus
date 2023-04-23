// © Customize+.
// Licensed under the MIT license.

using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Interface
{
	/// <summary>
	/// Very basic message window implementation to show the window which might be shown once or multiple times and can execute some action after being closed
	/// </summary>
	public class MessageWindow : WindowBase
	{
		protected override string Title => "Customize+ message";
		protected override Vector2 MinSize => new Vector2(200, 100);
		protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse;
		protected override bool LockCloseButton => true;

		public string WindowId { get; set; }
		public string Text { get; set; }
		public Action OnButtonPressed { get; set; }

		/// <summary>
		/// Show message window
		/// </summary>
		/// <param name="text">Window test</param>
		/// <param name="windowSize">Optional window size</param>
		/// <param name="onButtonPressed">Action to be executed when button is pressed</param>
		/// <param name="windowId">Window id, if set will only show this window once and never again</param>
		public static void Show(string text, Vector2? windowSize = null, Action? onButtonPressed = null, string windowId = null)
		{
			if (windowId != null && Plugin.ConfigurationManager.Configuration.ViewedMessageWindows.Contains(windowId.ToLowerInvariant()))
			{
				if (onButtonPressed != null)
					onButtonPressed();
				return;
			}

			MessageWindow window = Plugin.InterfaceManager.Show<MessageWindow>();
			window.Text = text;
			window.WindowId = windowId;
			window.OnButtonPressed = onButtonPressed;
			window.ForcedSize = windowSize ?? new Vector2(600, 100);
		}

		protected override void DrawContents()
		{
			ImGui.TextWrapped(Text);

			if(WindowId != null)
			{
				ImGui.SetCursorPosX(((Vector2)ForcedSize).X / 2 - 130);
				if (ImGui.Button("I understand, do not show this to me again"))
				{
					Plugin.ConfigurationManager.Configuration.ViewedMessageWindows.Add(WindowId.ToLowerInvariant());
					Plugin.ConfigurationManager.SaveConfiguration();
					if(OnButtonPressed != null)
						OnButtonPressed();
					this.Close();
				}
				return;
			}

			ImGui.SetCursorPosX(((Vector2)ForcedSize).X / 2 - 20);
			if (ImGui.Button("OK"))
			{
				if (OnButtonPressed != null)
					OnButtonPressed();
				this.Close();
			}
		}
	}
}
