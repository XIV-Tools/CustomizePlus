// © Customize+.
// Licensed under the MIT license.

using System;
using System.Numerics;
using Dalamud.Utility;
using ImGuiNET;

namespace CustomizePlus.Interface
{
    /// <summary>
    ///     Simple "OK/Cancel" dialog as a safety precaution against lossy procedures.
    /// </summary>
    public class ConfirmationDialog : WindowBase
    {
        private string affirmativeResponse = "OK";

        private string confirmationPrompt = "Confirm choice?";

        private Action doAfterConfirmed = () => { };
        private string negativeResponse = "Cancel";
        private string titleBar = "Confirmation";
        protected override string Title => titleBar;

        protected override ImGuiWindowFlags WindowFlags =>
            ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse;

        protected override bool LockCloseButton => true;

        /// <summary>
        ///     Displays a prompt requiring the user to confirm or cancel the execution of some Action.
        /// </summary>
        /// <param name="msg">Message to display to the user.</param>
        /// <param name="performUponConfirmation">Action to perform if user responds in the affirmative.</param>
        /// <param name="title">Title for the window (default "Confirmation").</param>
        /// <param name="confirm">Label for confirm button (default "OK").</param>
        /// <param name="cancel">Label for cancel button (default "Cancel").</param>
        public static void Show(string msg, Action performUponConfirmation, string title = "", string confirm = "",
            string cancel = "")
        {
            var window = Plugin.InterfaceManager.Show<ConfirmationDialog>();
            window.confirmationPrompt = msg;
            window.doAfterConfirmed = performUponConfirmation;

            if (!title.IsNullOrWhitespace())
            {
                window.titleBar = title;
            }

            if (!confirm.IsNullOrWhitespace())
            {
                window.affirmativeResponse = confirm;
            }

            if (!cancel.IsNullOrWhitespace())
            {
                window.negativeResponse = cancel;
            }
        }

        protected override void DrawContents()
        {
            RenderTextCentered(confirmationPrompt);

            RenderTextCentered("---");

            var buttonSize = new Vector2(100, 0);
            ImGui.SetCursorPosX(CenterMultiple(buttonSize.X, 2));

            if (ImGui.Button(affirmativeResponse))
            {
                doAfterConfirmed();
                Close();
            }

            ImGui.SameLine();

            if (ImGui.Button(negativeResponse))
            {
                Close();
            }
        }

        private static void RenderTextCentered(string text)
        {
            var windowHorz = ImGui.GetWindowWidth();
            var textHorz = ImGui.CalcTextSize(text).X;

            ImGui.SetCursorPosX((windowHorz - textHorz) * 0.5f);
            ImGui.Text(text);
        }

        private static float CenterMultiple(float width, int count)
        {
            var avail = ImGui.GetWindowWidth();
            return (avail - width * count - ImGui.GetStyle().ItemSpacing.X) / count;
        }
    }
}