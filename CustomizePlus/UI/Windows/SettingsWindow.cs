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
            DrawApplyToNPCsCheckbox();
            DrawApplyToNPCsInCutscenesCheckbox();
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

        private void DrawApplyToNPCsCheckbox()
        {
            var isChecked = Plugin.ConfigurationManager.Configuration.ApplyToNPCs;
            if (CtrlHelper.CheckboxWithTextAndHelp("##applytonpcs", "LEGACY? Apply to NPCs",
                    "Apply profiles to NPCs.\nSpecify a profile with the name 'Default' for it to apply to all NPCs and non-specified players.",
                    ref isChecked))
            {
                Plugin.ConfigurationManager.Configuration.ApplyToNPCs = isChecked;
                Plugin.ConfigurationManager.SaveConfiguration();
            }
        }

        private void DrawApplyToNPCsInCutscenesCheckbox()
        {
            var isChecked = Plugin.ConfigurationManager.Configuration.ApplyToNPCsInCutscenes;
            if (CtrlHelper.CheckboxWithTextAndHelp("##applytonpcscutscenes", "LEGACY? Apply to NPCs in Cutscenes",
                    "Apply profiles to NPCs in cutscenes.\nSpecify a profile with the name 'DefaultCutscene' to apply it to all generic characters while in a cutscene.",
                    ref isChecked))
            {
                Plugin.ConfigurationManager.Configuration.ApplyToNPCsInCutscenes = isChecked;
                Plugin.ConfigurationManager.SaveConfiguration();
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