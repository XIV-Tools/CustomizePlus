// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Data;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Utility;
using ImGuiNET;
using System;
using System.Numerics;

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
			string temp = get();
			bool result = TextBox(label, ref temp);
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

		public static bool CheckboxToggle(string label, in bool shown, Action<bool> toggle)
		{
			bool temp = shown;
			bool toggled = ImGui.Checkbox(label, ref temp);

			if (toggled)
			{
				toggle(temp);
			}

			return toggled;
		}

		public static bool ArrowToggle(string label, ref bool value)
		{
			bool toggled = ImGui.ArrowButton(label, value ? ImGuiDir.Down : ImGuiDir.Right);

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
	}
}
