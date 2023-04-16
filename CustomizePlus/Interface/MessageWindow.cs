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
	public class MessageWindow : WindowBase
	{
		protected override string Title => "Customize+ message";
		protected override Vector2 MinSize => new Vector2(50, 100);

		public string WindowId { get; set; }
		public string Text { get; set; }
		public Action OnButtonPressed { get; set; }

		public static void Show(string text, Action onButtonPressed, string windowId = null)
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
		}

		protected override void DrawContents()
		{
			ImGui.SetWindowSize(new Vector2(600, 100));
			ImGui.Text(Text);

			//https://github.com/ocornut/imgui/discussions/3862
			float avail = ImGui.GetContentRegionAvail().X;
			float off = (avail - 400) * 1f;
			if (off > 0.0f)
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + off);
			if(WindowId != null)
			{
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

			if (ImGui.Button("OK"))
			{
				if (OnButtonPressed != null)
					OnButtonPressed();
				this.Close();
			}
		}
	}
}
