// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Helpers;
using System;
using System.Numerics;
using Dalamud.Utility;
using ImGuiNET;

namespace CustomizePlus.UI.Dialogs
{
    /// <summary>
    ///     Simple "OK/Cancel" dialog as a safety precaution against lossy procedures.
    /// </summary>
    public class ConfirmationDialog : WindowBase
    {
        private string _affirmativeResponse = "OK";

        private string _confirmationPrompt = "Confirm choice?";

        private Action _doAfterConfirmed = () => { };
        private string _negativeResponse = "Cancel";
        private string _titleBar = "Confirmation";
        /// <inheritdoc/>
        protected override string Title => _titleBar;

        /// <inheritdoc/>
        protected override Vector2 MinSize => new(100, 100);
        /// <inheritdoc/>
        protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize;

        /// <inheritdoc/>
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
            window._confirmationPrompt = msg;
            window._doAfterConfirmed = performUponConfirmation;

            if (!title.IsNullOrWhitespace())
            {
                window._titleBar = title;
            }

            if (!confirm.IsNullOrWhitespace())
            {
                window._affirmativeResponse = confirm;
            }

            if (!cancel.IsNullOrWhitespace())
            {
                window._negativeResponse = cancel;
            }

            window.Focus();
        }

        /// <inheritdoc/>
        protected override void DrawContents()
        {
            CtrlHelper.StaticLabel(_confirmationPrompt, CtrlHelper.TextAlignment.Center);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            var buttonSize = new Vector2(100, CtrlHelper.IconButtonWidth);

            if (ImGui.BeginTable("Options", 3, ImGuiTableFlags.SizingFixedFit,
                    new Vector2(ImGui.GetContentRegionAvail().X, CtrlHelper.IconButtonWidth)))
            {
                ImGui.TableSetupColumn("##Space1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##Buttons", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("##Space 2", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                ImGui.TableNextColumn();

                if (ImGui.Button(_affirmativeResponse))
                {
                    _doAfterConfirmed();
                    Close();
                }

                ImGui.SameLine();
                ImGui.Dummy(new Vector2(CtrlHelper.IconButtonWidth));
                ImGui.SameLine();

                if (ImGui.Button(_negativeResponse))
                {
                    Close();
                }

                ImGui.TableNextColumn();

                ImGui.EndTable();
            }
        }
    }
}