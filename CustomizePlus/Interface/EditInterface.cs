// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Numerics;
	using System.Runtime.CompilerServices;
	using Anamnesis.Posing;
	using CustomizePlus.Data;
	using CustomizePlus.Data.Configuration;
	using Dalamud.Interface;
	using Dalamud.Interface.Components;
	using Dalamud.Logging;
	using Dalamud.Utility;
	using ImGuiNET;

	public class EditInterface : WindowBase
	{
		protected override string Title => $"Edit Scale: {this.BackupScale.ScaleName}";
		protected override string DrawTitle => $"{this.Title}###customize_plus_scale_edit_window{this.Index}"; //keep the same ID for all scale editor windows

		protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoScrollbar |
			(this.dirty ? ImGuiWindowFlags.UnsavedDocument : ImGuiWindowFlags.None);
        private int precision = 3;

        protected override bool LockCloseButton => this.dirty;

		protected BodyScale BackupScale { get; private set; }
		protected BodyScale WorkingScale { get; private set; }

		private bool dirty = false;

		private Dictionary<BoneData.BoneFamily, bool> boneGroups = BoneData.DisplayableFamilies.ToDictionary(x => x.Key, x => false);

		private static readonly PluginConfiguration Config = Plugin.ConfigurationManager.Configuration;
		private bool autoPopulated = true;

		private float windowHorz; //for formatting :V

		public static void Show(BodyScale scale)
		{
			EditInterface editWnd = Plugin.InterfaceManager.Show<EditInterface>();

			if (scale == null)
			{
				editWnd.BackupScale = new BodyScale();
			}
			else
			{
				editWnd.BackupScale = scale;
			}

			editWnd.WorkingScale = new BodyScale(editWnd.BackupScale);

			editWnd.autoPopulated = editWnd.WorkingScale.TryRepopulateBoneList();
			if (!editWnd.autoPopulated)
			{
				MessageWindow.Show($"Unable to locate bone data of {editWnd.WorkingScale.CharacterName} within running game.\nDefault bones will be loaded instead.");
				editWnd.WorkingScale.CreateDefaultBoneList();
			}
		}
		
		protected override void DrawContents()
		{
			PluginConfiguration config = Plugin.ConfigurationManager.Configuration;
			this.windowHorz = ImGui.GetWindowWidth();

			if (!this.WorkingScale.SameNamesAs(this.BackupScale))
			{
				this.dirty = true;
			}

			ImGui.SetNextItemWidth(windowHorz / 4);
			RenderTextBox("Character Name", ref this.WorkingScale.CharacterName);

			ImGui.SameLine();
			ImGui.Spacing();

			ImGui.SetNextItemWidth(windowHorz / 4);
			RenderTextBox("Scale Name", ref this.WorkingScale.ScaleName);

			RenderCheckBox("Enabled", ref this.WorkingScale.BodyScaleEnabled);

			ImGui.SameLine();

			RenderCheckBox("Dynamic Preview", config.AutomaticEditMode, (b) => config.AutomaticEditMode = b);
			AppendTooltip($"Applies changes automatically without saving.");

			ImGui.SameLine();

			RenderCheckBox("Mirror Mode", Config.MirrorMode, (b) => Config.MirrorMode = b);
			AppendTooltip($"Bone changes will be reflected from left to right and vice versa");

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();
			ImGui.SetNextItemWidth(150);
			ImGui.SliderInt("Precision", ref precision, 1, 6);

			ImGui.Separator();

			if (ImGui.RadioButton("Position", Config.EditingAttribute == EditMode.Position))
				Config.EditingAttribute = EditMode.Position;

			ImGui.SameLine();
			if (ImGui.RadioButton("Rotation", Config.EditingAttribute == EditMode.Rotation))
				Config.EditingAttribute = EditMode.Rotation;

			ImGui.SameLine();
			if (ImGui.RadioButton("Scale", Config.EditingAttribute == EditMode.Scale))
				Config.EditingAttribute = EditMode.Scale;

            if (Config.EditingAttribute != EditMode.Scale)
			{
				ImGui.SameLine();
				ImGui.PushFont(UiBuilder.IconFont);
				ImGui.Text(FontAwesomeIcon.ExclamationTriangle.ToIconString());
				ImGui.PopFont();
				ImGui.SameLine();
				ImGui.Text($"{Config.EditingAttribute} is an advanced setting and might not look properly with some animations, use at your own risk.");
			}

			ImGui.Separator();

			RenderBoneRow("n_root");

			string col1Label = Config.EditingAttribute == EditMode.Rotation ? "Yaw" : "X";
			string col2Label = Config.EditingAttribute == EditMode.Rotation ? "Pitch" : "Y";
			string col3Label = Config.EditingAttribute == EditMode.Rotation ? "Roll" : "Z";
			string col4Label = Config.EditingAttribute == EditMode.Scale ? "All" : "N/A";

			ImGui.Separator();
			ImGui.BeginTable("Bones", 6, ImGuiTableFlags.SizingStretchSame);
			ImGui.TableNextColumn();
			ImGui.Text("Bones:");
			ImGui.TableNextColumn();
			ImGui.Text(col1Label);
			ImGui.TableNextColumn();
			ImGui.Text(col2Label);
			ImGui.TableNextColumn();
			ImGui.Text(col3Label);
			ImGui.TableNextColumn();
			ImGui.Text(col4Label);
			ImGui.TableNextColumn();
			ImGui.Text("Name");
			ImGui.EndTable();
			ImGui.Separator();

			ImGui.BeginChild("scrolling", new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 56), false);

			foreach(var famGroup in this.WorkingScale.GetBonesByFamily())
			{
				bool tempRef = this.boneGroups[famGroup.Key];
				RenderArrowToggle($"##{famGroup.Key}", ref tempRef);
				ImGui.SameLine();
				RenderLabel(famGroup.Key.ToString());
				if (BoneData.DisplayableFamilies.TryGetValue(famGroup.Key, out string? tip) && tip != null)
				{
					AppendTooltip(tip);
				}

				if (tempRef)
				{
					ImGui.Spacing();

					foreach(string codename in famGroup.Value)
					{
						RenderBoneRow(codename);
					}

					ImGui.Spacing();
				}
				this.boneGroups[famGroup.Key] = tempRef;

				ImGui.Separator();
			}

			ImGui.EndChild();

			ImGui.Separator();

			//----------------------------------

			if (ImGui.Button("Save") && this.dirty)
			{
				void Proceed()
				{
					AddToConfig();
					Plugin.ConfigurationManager.SaveConfiguration();
					Plugin.LoadConfig();
				}

				if ((this.WorkingScale.CharacterName != this.BackupScale.CharacterName
					|| this.WorkingScale.ScaleName != this.BackupScale.ScaleName)
					&& config.BodyScales.Contains(this.WorkingScale))
				{
					ConfirmationDialog.Show($"Overwrite existing scaling '{this.WorkingScale.ScaleName}' on {this.WorkingScale.CharacterName}?",
						Proceed, "Confirm Overwrite");
				}
				else
				{
					Proceed();
				}
			}
			AppendTooltip("Save changes and continue editing");

			ImGui.SameLine();

			if (ImGui.Button("Save and Close"))
			{
				if (!this.dirty)
				{
					this.Close();
				}
				else
				{
					void Proceed()
					{
						AddToConfig();
						Plugin.ConfigurationManager.SaveConfiguration();
						Plugin.LoadConfig();
						this.Close();
					}

					if ((this.WorkingScale.CharacterName != this.BackupScale.CharacterName
						|| this.WorkingScale.ScaleName != this.BackupScale.ScaleName)
						&& config.BodyScales.Contains(this.WorkingScale))
					{
						ConfirmationDialog.Show($"Overwrite existing scaling '{this.WorkingScale.ScaleName}' on {this.WorkingScale.CharacterName}?",
							Proceed, "Confirm Overwrite");
					}
					else
					{
						Proceed();
					}
				}
			}
			AppendTooltip("Save changes and stop editing");

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			ImGui.Text("Save with new scale/character name to create a copy.");

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			if (ImGui.Button("Revert") && this.dirty) {
				ConfirmationDialog.Show("Revert all unsaved work?", RevertAll);
			}
			AppendTooltip("Remove all pending changes, reverting to last save");

			ImGui.SameLine();

			if (ImGui.Button("Cancel")) {
				if (!this.dirty)
				{
					this.Close();
				}
				else
				{
					void Close()
					{
						this.RevertAll();
						this.Close();
					}
					ConfirmationDialog.Show("Revert unsaved work and exit editor?", Close);
				}
			}
			AppendTooltip("Close the editor without saving\n(reverting all unsaved changes)");
		}

		private void AddToConfig()
		{
			PluginConfiguration config = Plugin.ConfigurationManager.Configuration;

			BodyScale pruned = this.WorkingScale.GetPrunedScale();

			if (config.BodyScales.Remove(this.WorkingScale))
			{
				config.BodyScales.Add(pruned);
			}
			else
			{
				config.BodyScales.Add(pruned);
				Plugin.ConfigurationManager.ToggleOffAllOtherMatching(pruned);
			}

			Plugin.ConfigurationManager.SaveConfiguration();

			this.BackupScale = new BodyScale(pruned);
			this.dirty = false;
		}

		private void UpdateCurrent(string boneName, BoneEditsContainer boneValue, bool autoMode = false)
		{
			PluginConfiguration config = Plugin.ConfigurationManager.Configuration;

			this.WorkingScale.Bones[boneName] = boneValue.DeepCopy();

			if (autoMode && config.BodyScales.Remove(this.WorkingScale))
			{
				config.BodyScales.Add(this.WorkingScale);
				Plugin.ConfigurationManager.SaveConfiguration();
				Plugin.LoadConfig(true);
			}
		}

		private void RevertAll()
		{
			var config = Plugin.ConfigurationManager.Configuration;

			this.WorkingScale = new BodyScale(this.BackupScale);
			this.WorkingScale.CreateDefaultBoneList();

			if (config.BodyScales.Remove(this.WorkingScale))
			{
				config.BodyScales.Add(this.WorkingScale);
				Plugin.ConfigurationManager.SaveConfiguration();
				Plugin.LoadConfig(true);
			}
		}


		#region ImGui helper functions

		private bool RenderTextBox(string label, ref string value)
		{
			return ImGui.InputText(label, ref value, 1024);
		}

		private bool RenderCheckBox(string label, ref bool value)
		{
			return ImGui.Checkbox(label, ref value);
		}

		private bool RenderCheckBox(string label, in bool shown, Action<bool> toggle)
		{
			bool temp = shown;
			bool toggled = ImGui.Checkbox(label, ref temp);

			if (toggled)
			{
				toggle(temp);
			}

			return toggled;
		}

		private bool RenderArrowToggle(string label, ref bool value)
		{
			bool toggled = ImGui.ArrowButton(label, value ? ImGuiDir.Down : ImGuiDir.Right);

			if (toggled)
			{
				value = !value;
			}

			return value;
		}

		private void AppendTooltip(string text)
		{
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip(text);
			}
		}

		private bool RenderResetButton(string codename, ref Vector3 value)
		{
			bool output = ImGuiComponents.IconButton(codename, FontAwesomeIcon.Recycle);
			AppendTooltip($"Reset '{BoneData.GetBoneDisplayName(codename)}' to default {Config.EditingAttribute} values");

			if (output)
			{
				value = Config.EditingAttribute switch
				{
					EditMode.Scale => Vector3.One,
					_ => Vector3.Zero
				};
			}

			return output;
		}

		private bool RenderRevertButton(string codename, ref Vector3 value)
		{
			bool output = ImGuiComponents.IconButton(codename, FontAwesomeIcon.ArrowCircleLeft);
			AppendTooltip($"Revert '{BoneData.GetBoneDisplayName(codename)}' to last saved {Config.EditingAttribute} values");

			if (output)
			{
				//if the backup scale doesn't contain bone values to revert TO, then just reset it

				if (this.BackupScale.TryGetBone(codename, out BoneEditsContainer bec) && bec != null)
				{
					value = Config.EditingAttribute switch
					{
						EditMode.Position => bec.Position,
						EditMode.Rotation => bec.Rotation,
						_ => bec.Scale
					};
				}
				else
				{
					value = Config.EditingAttribute switch
					{
						EditMode.Scale => Vector3.One,
						_ => Vector3.Zero
					};
				}
			}

			return output;
		}

		private bool RenderDragBox(string label, ref Vector3 value)
		{
			float velocity = Config.EditingAttribute == EditMode.Rotation ? 1.0f : 0.001f;
			float minValue = Config.EditingAttribute == EditMode.Rotation ? -360.0f : -10.0f;
			float maxValue = Config.EditingAttribute == EditMode.Rotation ? 360.0f : 10.0f;

			float temp = Config.EditingAttribute switch
			{
				EditMode.Position => 0.0f,
				EditMode.Rotation => 0.0f,
				_ => value.X == value.Y && value.Y == value.Z ? value.X : 1.0f
			};
			

			if (ImGui.DragFloat(label, ref temp, velocity, minValue, maxValue, $"%.{precision}f"))
			{
				value = new Vector3(temp, temp, temp);
				return true;
			}
			return false;
		}

		private bool RenderTripleDragBoxes(string label, ref Vector3 value)
		{
			float velocity = Config.EditingAttribute == EditMode.Rotation ? 1.0f : 0.001f;
			float minValue = Config.EditingAttribute == EditMode.Rotation ? -360.0f : -10.0f;
			float maxValue = Config.EditingAttribute == EditMode.Rotation ? 360.0f : 10.0f;

			return ImGui.DragFloat3(label, ref value, velocity, minValue, maxValue, $"%.{precision}f");
		}

		private void RenderLabel(string? text, string tooltip = "")
		{
			ImGui.Text(text ?? "Unknown Bone");
			if (!tooltip.IsNullOrWhitespace())
			{
				AppendTooltip(tooltip);
			}
		}

		private void RenderBoneRow(string codename)
		{
			if (!this.WorkingScale.TryGetBone(codename, out var currentBoneEdits) || currentBoneEdits == null)
			{
				currentBoneEdits = new BoneEditsContainer();
				this.WorkingScale.Bones.Add(codename, currentBoneEdits);
			}

			string displayName = BoneData.GetBoneDisplayName(codename) ?? codename;

			bool flagUpdate = false;

			Vector3 whichBoneVector = Config.EditingAttribute switch
			{
				EditMode.Position => currentBoneEdits.Position,
				EditMode.Rotation => currentBoneEdits.Rotation,
				_ => currentBoneEdits.Scale
			};

			ImGui.PushID(codename);

			flagUpdate |= RenderResetButton(codename, ref whichBoneVector);

			ImGui.SameLine();

			flagUpdate |= RenderRevertButton(codename, ref whichBoneVector);

			ImGui.SameLine();

			ImGui.SetNextItemWidth(this.windowHorz * 3 / 6);
			flagUpdate |= RenderTripleDragBoxes($"##{displayName}", ref whichBoneVector);

			ImGui.SameLine();

			if (Config.EditingAttribute != EditMode.Scale)
				ImGui.BeginDisabled();

			ImGui.SetNextItemWidth(this.windowHorz / 7);
			flagUpdate |= RenderDragBox($"##{displayName}AllAxes", ref whichBoneVector);

			if (Config.EditingAttribute != EditMode.Scale)
				ImGui.EndDisabled();

			ImGui.SameLine();

			RenderLabel(displayName, BoneData.IsIVCSBone(codename) ? $"(IVCS) {codename}" : codename);

			ImGui.PopID();

			if (flagUpdate)
			{
				this.dirty = true;

				currentBoneEdits.UpdateVector(Config.EditingAttribute, whichBoneVector);

				//this is really ugly, but I couldn't think of a better way to pull these values out
				//and be able to pass them back further below
				string mirrorName = String.Empty;
				BoneEditsContainer? mirrorEdits = null;
				if (Config.MirrorMode)
				{
					mirrorName = BoneData.GetBoneMirror(codename);
					if (mirrorName != null
						&& this.WorkingScale.TryGetMirror(codename, out mirrorEdits)
						&& mirrorEdits != null)
					{
						//IVCS bones have their axes oriented differently, so they get reflected differently
						if (BoneData.IsIVCSBone(codename))
						{
							mirrorEdits = currentBoneEdits.ReflectIVCS();
						}
						else
						{
							mirrorEdits = currentBoneEdits.ReflectAcrossZPlane();
						}
					}
				}

				if (Plugin.ConfigurationManager.Configuration.AutomaticEditMode)
				{
					this.UpdateCurrent(codename, currentBoneEdits, true);

					if (Config.MirrorMode && !mirrorName.IsNullOrEmpty() && mirrorEdits != null)
					{
						this.UpdateCurrent(mirrorName, mirrorEdits, true);
					}
				}
			}
		}

		#endregion

	}

	public enum EditMode
	{
		Position,
		Rotation,
		Scale
	}


}
