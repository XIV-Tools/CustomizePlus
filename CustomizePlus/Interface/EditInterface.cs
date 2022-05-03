// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
	using ImGuiNET;

	public class EditInterface : WindowBase
	{
		protected BodyScale? Scale { get; private set; }
		protected override string Title => $"Edit Scale: {this.Scale?.CharacterName}";

		public static void Show(BodyScale scale)
		{
			EditInterface editWnd = Plugin.InterfaceManager.Show<EditInterface>();
			editWnd.Scale = scale;
		}

		protected override void DrawContents()
		{
			ImGui.Text("Coming Soon");
		}
	}
}
