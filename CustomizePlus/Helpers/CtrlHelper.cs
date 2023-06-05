// © Customize+.
// Licensed under the MIT license.

using System;
using Dalamud.Interface;
using Dalamud.Utility;
using ImGuiNET;

namespace CustomizePlus.Helpers
{
    public static class CtrlHelper
    {
        public static bool TextBox(string label, ref string value)
        {
            return ImGui.InputText(label, ref value, 1024);
        }

        public static bool TextPropertyBox(string label, Func<string> get, Action<string> set)
        {
            var temp = get();
            var result = TextBox(label, ref temp);
            if (result)
            {
                set(temp);
            }

            return result;
        }

        public static bool Checkbox(string label, ref bool value)
        {
            return ImGui.Checkbox(label, ref value);
        }

        public static bool CheckboxWithTextAndHelp(string label, string text, string helpText, ref bool value)
        {
            var checkBoxState = ImGui.Checkbox(label, ref value);
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text(FontAwesomeIcon.InfoCircle.ToIconString());
            ImGui.PopFont();
            AddHoverText(helpText);
            ImGui.SameLine();
            ImGui.Text(text);

            AddHoverText(helpText);

            return checkBoxState;
        }

        public static bool CheckboxToggle(string label, in bool shown, Action<bool> toggle)
        {
            var temp = shown;
            var toggled = ImGui.Checkbox(label, ref temp);

            if (toggled)
            {
                toggle(temp);
            }

            return toggled;
        }

        public static bool ArrowToggle(string label, ref bool value)
        {
            var toggled = ImGui.ArrowButton(label, value ? ImGuiDir.Down : ImGuiDir.Right);

            if (toggled)
            {
                value = !value;
            }

            return value;
        }

        public static void AddHoverText(string text)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(text);
            }
        }


        public static void StaticLabel(string? text, string tooltip = "")
        {
            ImGui.Text(text ?? "Unknown Bone");
            if (!tooltip.IsNullOrWhitespace())
            {
                AddHoverText(tooltip);
            }
        }

        public static void LabelWithIcon(FontAwesomeIcon icon, string text)
        {
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text(icon.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.Text(text);
        }
    }
}