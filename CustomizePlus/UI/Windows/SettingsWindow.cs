// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Helpers;
using Dalamud.Interface;
using ImGuiNET;

namespace CustomizePlus.UI.Windows
{
    internal class SettingsWindow : WindowBase
    {
        protected override string Title => "Customize+ settings";
        protected override bool SingleInstance => true;
        protected override bool LockCloseButton => false;

        public static void Show()
        {
            Plugin.InterfaceManager.Show<SettingsWindow>();
        }

        protected override void DrawContents()
        {
            DrawGeneralSettings();
            DrawAdvancedSettings();
        }

        private void DrawGeneralSettings()
        {
            var isShouldDraw = ImGui.CollapsingHeader("General");

            if (!isShouldDraw)
                return;

            DrawPluginEnabledCheckbox();
        }

        private void DrawPluginEnabledCheckbox()
        {
            var isChecked = Plugin.ConfigurationManager.Configuration.PluginEnabled;
            //users doesn't really need to know what exactly this checkbox does so we just tell them it toggles all profiles
            if (CtrlHelper.CheckboxWithTextAndHelp("##pluginenabled", "Enable profiles",
                    "Globally enables or disables all profiles.", ref isChecked))
            {
                Plugin.ConfigurationManager.Configuration.PluginEnabled = isChecked;
                Plugin.ConfigurationManager.SaveConfiguration();
                Plugin.ReloadHooks();
            }
        }

        private void DrawAdvancedSettings()
        {
            var isShouldDraw = ImGui.CollapsingHeader("Advanced");

            if (!isShouldDraw)
                return;

            ImGui.NewLine();
            CtrlHelper.LabelWithIcon(FontAwesomeIcon.ExclamationTriangle,
                "These are advanced settings. NO support is provided for them, unless they are not working at all.");
            ImGui.NewLine();

            DrawEnableRootPositionCheckbox();
        }

        private void DrawEnableRootPositionCheckbox()
        {
            var isChecked = Plugin.ConfigurationManager.Configuration.RootPositionEditingEnabled;
            if (CtrlHelper.CheckboxWithTextAndHelp("##rootpos", "Root editing",
                    "Enables ability to edit the root bones.", ref isChecked))
            {
                Plugin.ConfigurationManager.Configuration.RootPositionEditingEnabled = isChecked;
                Plugin.ConfigurationManager.SaveConfiguration();
            }
        }
    }
}