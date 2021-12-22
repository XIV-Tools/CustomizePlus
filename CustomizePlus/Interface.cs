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

            ImGui.SetNextWindowSize(new Vector2(232, 75), ImGuiCond.Always);
            if (ImGui.Begin(
				"CustomizePlus",
				ref this.Visible,
				ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
            }

            ImGui.End();
        }
    }
}
