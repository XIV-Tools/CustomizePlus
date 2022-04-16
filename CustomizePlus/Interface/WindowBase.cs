// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
	using System;
	using System.Numerics;
	using ImGuiNET;

	public abstract class WindowBase : InterfaceBase
	{
		private bool visible;

		protected abstract string Title { get; }

		protected virtual Vector2 MinSize => new Vector2(256, 256);
		protected virtual Vector2 MaxSize => new Vector2(1920, 1080);

		private string DrawTitle => $"{this.Title}###{this.Index}";

		public override void Open()
		{
			base.Open();
			this.visible = true;
		}

		public override void Focus()
		{
			ImGui.SetWindowFocus(this.DrawTitle);
			base.Focus();
		}

		public sealed override void Draw()
		{
			ImGui.SetNextWindowSizeConstraints(this.MinSize, this.MaxSize);

			if (ImGui.Begin(this.DrawTitle, ref this.visible))
			{
				this.DrawContents();
			}

			ImGui.End();

			if (!this.visible)
			{
				this.Close();
			}
		}

		protected abstract void DrawContents();
	}
}
