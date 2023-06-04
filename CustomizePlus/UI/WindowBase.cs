// © Customize+.
// Licensed under the MIT license.

using System.Numerics;

using ImGuiNET;

namespace CustomizePlus.UI
{
    public abstract class WindowBase : UserInterfaceBase
    {
        private bool _isAlwaysVisibleDummy = true; //dummy variable for LockCloseButton = true
        private bool _isVisible;

        protected abstract string Title { get; }

        protected virtual Vector2? ForcedSize { get; set; }
        protected virtual Vector2 MinSize => new(550, 256);
        protected virtual Vector2 MaxSize => new(2560, 1440);

        protected virtual ImGuiWindowFlags WindowFlags { get; set; } = ImGuiWindowFlags.None;
        protected virtual bool LockCloseButton { get; set; }

        /// <summary>
        ///     Gets normally you wouldn't want to override this.
        /// </summary>
        protected virtual string DrawTitle => $"{Title}";

        public override void Open()
        {
            base.Open();
            _isVisible = true;
        }

        public override void Focus()
        {
            ImGui.SetWindowFocus(DrawTitle);
            base.Focus();
        }

        public sealed override void Draw()
        {
            ImGui.SetNextWindowSizeConstraints(MinSize, MaxSize);

            if (ForcedSize != null)
            {
                ImGui.SetNextWindowSize((Vector2)ForcedSize);
            }

            if (ImGui.Begin(DrawTitle, ref LockCloseButton ? ref _isAlwaysVisibleDummy : ref _isVisible, WindowFlags))
            {
                DrawContents();
            }

            ImGui.End();

            if (!_isVisible)
            {
                Close();
            }
        }

        protected abstract void DrawContents();
    }
}