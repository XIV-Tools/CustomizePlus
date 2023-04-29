// © Customize+.
// Licensed under the MIT license.

using Dalamud.Utility;
using ImGuiNET;
using System.Numerics;
namespace CustomizePlus.Interface
{
	/// <summary>
	/// Simple "OK/Cancel" dialog as a safety precaution against lossy procedures.
	/// </summary>
	public class ConfirmationDialog : WindowBase
	{
		protected override string Title => this.titleBar;
		protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse;
		protected override bool LockCloseButton => true;

		private string confirmationPrompt = "Confirm choice?";
		private string titleBar = "Confirmation";
		private string affirmativeResponse = "OK";
		private string negativeResponse = "Cancel";

		private System.Action doAfterConfirmed = () => { };

		/// <summary>
		/// Displays a prompt requiring the user to confirm or cancel the execution of some Action.
		/// </summary>
		/// <param name="msg">Message to display to the user.</param>
		/// <param name="performUponConfirmation">Action to perform if user responds in the affirmative.</param>
		/// <param name="title">Title for the window (default "Confirmation").</param>
		/// <param name="confirm">Label for confirm button (default "OK").</param>
		/// <param name="cancel">Label for cancel button (default "Cancel").</param>
		public static void Show(string msg, System.Action performUponConfirmation, string title = "", string confirm = "", string cancel = "")
		{
			ConfirmationDialog window = Plugin.InterfaceManager.Show<ConfirmationDialog>();
			window.confirmationPrompt = msg;
			window.doAfterConfirmed = performUponConfirmation;

			if (!title.IsNullOrWhitespace())
				window.titleBar = title;

			if (!confirm.IsNullOrWhitespace())
				window.affirmativeResponse = confirm;

			if (!cancel.IsNullOrWhitespace())
				window.negativeResponse = cancel;
		}

		protected override void DrawContents()
		{
			RenderTextCentered(this.confirmationPrompt);

			RenderTextCentered("---");

			var buttonSize = new Vector2(100, 0);
			ImGui.SetCursorPosX(CenterMultiple(buttonSize.X, 2));

			if (ImGui.Button(affirmativeResponse))
			{
				this.doAfterConfirmed();
				this.Close();
			}

			ImGui.SameLine();

			if (ImGui.Button(negativeResponse))
			{
				this.Close();
			}
		}

		private static void RenderTextCentered(string text)
		{
			float windowHorz = ImGui.GetWindowWidth();
			float textHorz = ImGui.CalcTextSize(text).X;

			ImGui.SetCursorPosX((windowHorz - textHorz) * 0.5f);
			ImGui.Text(text);
		}

		private static float CenterMultiple(float width, int count)
		{
			var avail = ImGui.GetWindowWidth();
			return (avail - (width * count) - ImGui.GetStyle().ItemSpacing.X) / count;
		}
	}
}
