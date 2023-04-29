// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
	using System;
	using System.Numerics;
	using Dalamud.Logging;
	using ImGuiNET;

	public abstract class WindowBase : InterfaceBase
	{
		private bool visible;
		private bool alwaysVisibleDummy = true; //dummy variable for LockCloseButton = true

		protected abstract string Title { get; }

		protected virtual Vector2? ForcedSize { get; set; }
		protected virtual Vector2 MinSize => new Vector2(550, 256);
		protected virtual Vector2 MaxSize => new Vector2(2560, 1440);

		protected virtual ImGuiWindowFlags WindowFlags { get; set; } = ImGuiWindowFlags.None;
		protected virtual bool LockCloseButton { get; set; }

		/// <summary>
		/// Normally you wouldn't want to override this
		/// </summary>
		protected virtual string DrawTitle => $"{this.Title}";

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

			if(ForcedSize != null)
				ImGui.SetNextWindowSize((Vector2)this.ForcedSize);

			if (ImGui.Begin(this.DrawTitle, ref (LockCloseButton ? ref this.alwaysVisibleDummy : ref this.visible), WindowFlags))
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
