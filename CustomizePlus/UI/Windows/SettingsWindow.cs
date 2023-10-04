// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Helpers;
using CustomizePlus.Services;
using CustomizePlus.UI.Windows.Debug;
using Dalamud.Interface;
using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Diagnostics;
using System.IO;

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

        #region General Settings
        // General Settings
        private void DrawGeneralSettings()
        {
            var isShouldDraw = ImGui.CollapsingHeader("General");

            if (!isShouldDraw)
                return;

            DrawPluginEnabledCheckbox();
            DrawOpenConfigLocation();
            ImGui.SameLine();
            DrawRediscover();
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

        private void DrawRediscover() {
            if (ImGui.Button("Rediscover Profiles")) {
                Plugin.ProfileManager.CheckForNewProfiles();
            }
            CtrlHelper.AddHoverText("Rediscover profiles.");
        }

        private void DrawOpenConfigLocation() {
            var path = DalamudServices.PluginInterface.GetPluginConfigDirectory();
            if (ImGui.Button("Open Config Folder")) {
                try {

                    if (Path.Exists(path)) {
                        Process.Start("explorer.exe", path);
                    }
                } catch (Exception e) {
                    PluginLog.Error($"Failed to open Config Location at {path} due to:\n{e}");
                }
            }
            CtrlHelper.AddHoverText("Open Config Folder");
        }

        #endregion

        #region Advanced Settings
        // Advanced Settings
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
            DrawIPCTest();
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

        // This Entire window needs a serious rework at some point, its useless right now.
        private void DrawIPCTest() {
            // IPC Testing Window - Hidden unless enabled in json.
            var isChecked = Plugin.ConfigurationManager.Configuration.DebuggingModeEnabled;
            if (isChecked) {
                if (ImGui.Button("D: IPC Test Window BROKEN")) {
                    IPCTestWindow.Show(DalamudServices.PluginInterface);
                }
                CtrlHelper.AddHoverText("D: Test IPC");
            }
        }

        #endregion
    }
}