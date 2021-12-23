// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
    using System.Numerics;
    using ImGuiNET;

    public class Interface
    {
		public bool Visible;

		public void Show()
		{
			this.Visible = true;
		}

		public void Close()
		{
			this.Visible = false;
		}

		public void Draw()
        {
            if (!this.Visible)
                return;

            ImGui.SetNextWindowSize(new Vector2(450, 600), ImGuiCond.Always);
            if (ImGui.Begin(
				"CustomizePlus",
				ref this.Visible,
				ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
				ImGui.BeginChild("Scrolling");
				ImGui.Text(Plugin.Status.ToString());
				ImGui.EndChild();
			}

            ImGui.End();
        }
    }
}
