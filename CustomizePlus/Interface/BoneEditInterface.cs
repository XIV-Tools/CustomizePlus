// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Numerics;
	using System.Runtime.CompilerServices;
	using CustomizePlus.Data;
	using CustomizePlus.Data.Configuration;
	using CustomizePlus.Data.Profile;
	using CustomizePlus.Data.Armature;
	using CustomizePlus.Helpers;
	using Dalamud.Interface;
	using Dalamud.Interface.Components;
	using Dalamud.Logging;
	using Dalamud.Utility;
	using ImGuiNET;
	using Newtonsoft.Json.Linq;
	using System.Drawing;

	public class BoneEditInterface : WindowBase
	{
		protected override string Title => $"Edit Profile: {this.profileInProgress.ProfName}";
		protected override bool SingleInstance => true;
		protected override string DrawTitle => $"{this.Title}###customize_plus_scale_edit_window{this.Index}"; //keep the same ID for all scale editor windows

		protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoScrollbar |
			(this.dirty ? ImGuiWindowFlags.UnsavedDocument : ImGuiWindowFlags.None);
        private int precision = 3;

		protected override bool LockCloseButton => true;

		public EditorSessionSettings Settings;

		private CharacterProfile profileInProgress;
		private string originalCharName;
		private string originalProfName;
		private Armature? skeletonInProgress => profileInProgress.Armature;

		private bool dirty = false;

		private float windowHorz; //for formatting :V

		public static void Show(CharacterProfile prof)
		{
			BoneEditInterface editWnd = Plugin.InterfaceManager.Show<BoneEditInterface>();

			editWnd.profileInProgress = prof;
			editWnd.originalCharName = prof.CharName;
			editWnd.originalProfName = prof.ProfName;

			//By having the armature manager to do its checks on this profile,
			//	we force it to generate and track a new armature for it
			Plugin.ArmatureManager.RenderCharacterProfiles(prof);

			editWnd.Settings = new EditorSessionSettings(prof.Armature);

			editWnd.ConfirmSkeletonConnection();
		}
		
		protected override void DrawContents()
		{
			this.windowHorz = ImGui.GetWindowWidth();

			if (this.profileInProgress.CharName != this.originalCharName
				|| this.profileInProgress.ProfName != this.originalProfName)
			{
				this.dirty = true;
			}

			ImGui.SetNextItemWidth(windowHorz / 4);
			CtrlHelper.TextPropertyBox("Character Name",
				() => this.profileInProgress.CharName,
				(s) => this.profileInProgress.CharName = s);

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			ImGui.SetNextItemWidth(windowHorz / 4);
			CtrlHelper.TextPropertyBox("Profile Name",
				() => this.profileInProgress.ProfName,
				(s) => this.profileInProgress.ProfName = s);

			bool tempEnabled = this.profileInProgress.Enabled;
			if (CtrlHelper.Checkbox("Preview Enabled", ref tempEnabled))
			{
				this.profileInProgress.Enabled = tempEnabled;
				this.ConfirmSkeletonConnection();
			}
			CtrlHelper.AddHoverText($"Hook the editor into the game to edit and preview live bone data");

			ImGui.SameLine();

			if (!this.profileInProgress.Enabled) ImGui.BeginDisabled();

			if (CtrlHelper.Checkbox("Show Live Bones", ref Settings.ShowLiveBones))
			{
				this.ConfirmSkeletonConnection();
			}
			CtrlHelper.AddHoverText($"If selected, present for editing all bones found in the game data,\nelse show only bones for which the profile already contains edits.");

			ImGui.SameLine();

			if (CtrlHelper.Checkbox("Mirror Mode", ref Settings.MirrorModeEnabled))
			{
				this.ConfirmSkeletonConnection();
			}
			CtrlHelper.AddHoverText($"Bone changes will be reflected from left to right and vice versa");

			ImGui.SameLine();

			if (CtrlHelper.Checkbox("Parenting Mode", ref Settings.ParentingEnabled))
			{
				this.ConfirmSkeletonConnection();
			}
			CtrlHelper.AddHoverText($"Changes will propagate \"outward\" from edited bones");

			if (!this.profileInProgress.Enabled) ImGui.EndDisabled();

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			ImGui.SetNextItemWidth(150);
			ImGui.SliderInt("Precision", ref precision, 1, 6);
			CtrlHelper.AddHoverText("Level of precision to display while editing values");

			ImGui.Separator();

			if (ImGui.RadioButton("Position", Settings.EditingAttribute == BoneAttribute.Position))
			{
				Settings.EditingAttribute = BoneAttribute.Position;
			}
			CtrlHelper.AddHoverText($"{FontAwesomeIcon.ExclamationTriangle} May cause unintended animation changes. Edit at your own risk");

			ImGui.SameLine();
			if (ImGui.RadioButton("Rotation", Settings.EditingAttribute == BoneAttribute.Rotation))
			{
				Settings.EditingAttribute = BoneAttribute.Rotation;
			}
			CtrlHelper.AddHoverText($"{FontAwesomeIcon.ExclamationTriangle} May cause unintended animation changes. Edit at your own risk");

			ImGui.SameLine();
			if (ImGui.RadioButton("Scale", Settings.EditingAttribute == BoneAttribute.Scale))
			{
				Settings.EditingAttribute = BoneAttribute.Scale;
			}

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			ImGui.TextUnformatted("|");

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			if (!Settings.EditStack.UndoPossible()) ImGui.BeginDisabled();
			if(ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
			{
				Settings.EditStack.Undo();
			}
			CtrlHelper.AddHoverText("Undo last edit");
			if (!Settings.EditStack.UndoPossible()) ImGui.EndDisabled();

			ImGui.SameLine();

			if (!Settings.EditStack.RedoPossible()) ImGui.BeginDisabled();
			if (ImGuiComponents.IconButton(FontAwesomeIcon.RedoAlt))
			{
				Settings.EditStack.Redo();
			}
			CtrlHelper.AddHoverText("Redo next edit");
			if (!Settings.EditStack.RedoPossible()) ImGui.EndDisabled();

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			ImGui.TextUnformatted("|");

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			if (!this.profileInProgress.Enabled) ImGui.BeginDisabled();
			if (ImGui.Button("Reload Bone Data"))
			{
				this.skeletonInProgress.RebuildSkeleton();
			}
			CtrlHelper.AddHoverText("Refresh the skeleton data obtained from in-game");
			if (!this.profileInProgress.Enabled) ImGui.EndDisabled();

			ImGui.Separator();

			//CompleteBoneEditor("n_root");

			string col1Label = Settings.EditingAttribute == BoneAttribute.Rotation
				? $"{FontAwesomeIcon.ArrowsLeftRight.ToIconString()} Roll"
				: $"{FontAwesomeIcon.ArrowsUpDown.ToIconString()} X";
			string col2Label = Settings.EditingAttribute == BoneAttribute.Rotation
				? $"{FontAwesomeIcon.ArrowsUpDown.ToIconString()} Pitch"
				: $"{FontAwesomeIcon.ArrowsLeftRight.ToIconString()} Y";	
			string col3Label = Settings.EditingAttribute == BoneAttribute.Rotation
				? $"{FontAwesomeIcon.GroupArrowsRotate.ToIconString()} Yaw"
				: $"{FontAwesomeIcon.ArrowsToEye.ToIconString()} Z";	
			string col4Label = Settings.EditingAttribute == BoneAttribute.Scale
				? $"{FontAwesomeIcon.ArrowsUpDownLeftRight.ToIconString()} All"
				: $"{FontAwesomeIcon.Ban.ToIconString()} N/A";

			//ImGui.Separator();
			//if (ImGui.BeginTable("Bones", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
			//{
			//	ImGui.TableSetupColumn("Options", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
			//	ImGui.TableSetupColumn("X");
			//	ImGui.TableSetupColumn("Y");
			//	ImGui.TableSetupColumn("Z");
			//	ImGui.TableSetupColumn("All");
			//	ImGui.TableSetupColumn("Bone Name", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
			//	ImGui.TableHeadersRow();

			//	IEnumerable<string> relevantBoneNames = Settings.ShowLiveBones
			//	? this.skeletonInProgress.GetExtantBoneNames()
			//	: this.profileInProgress.Bones.Keys;

			//	var groupedBones = relevantBoneNames.GroupBy(x => BoneData.GetBoneFamily(x));

			//	foreach (var boneGroup in groupedBones.OrderBy(x => (int)x.Key))
			//	{
			//		//ImGui.PushID((int)boneGroup.Key);

			//		//ImGui.TableNextRow();
			//		//ImGui.TableNextColumn();

			//		////create a dropdown entry for the family if one doesn't already exist
			//		////mind that it'll only be rendered if bones exist to fill it
			//		//if (!Settings.GroupExpandedState.TryGetValue(boneGroup.Key, out bool expanded))
			//		//{
			//		//	Settings.GroupExpandedState[boneGroup.Key] = false;
			//		//	expanded = false;
			//		//}

			//		//CtrlHelper.ArrowToggle($"##{boneGroup.Key}", ref expanded);
			//		//ImGui.SameLine();
			//		//CtrlHelper.StaticLabel(boneGroup.Key.ToString());
			//		//if (BoneData.DisplayableFamilies.TryGetValue(boneGroup.Key, out string? tip) && tip != null)
			//		//{
			//		//	CtrlHelper.AddHoverText(tip);
			//		//}

			//		//ImGui.TableNextColumn();
			//		//ImGui.TableNextColumn();
			//		//ImGui.TableNextColumn();
			//		//ImGui.TableNextColumn();
			//		//ImGui.TableNextColumn();

			//		bool expanded = true;
			//		if (expanded)
			//		{
			//			//ImGui.Spacing();

			//			foreach (string codename in boneGroup.OrderBy(x => BoneData.GetBoneIndex(x)))
			//			{

			//				ImGui.TableNextRow();

			//				CompleteBoneRow(codename, col1Label, col2Label, col3Label);
			//			}

			//			//ImGui.Spacing();
			//		}

			//		Settings.GroupExpandedState[boneGroup.Key] = expanded;

			//		ImGui.Separator();
			//		//ImGui.PopID();
			//	}
			//}
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

			IEnumerable<string> relevantBoneNames = Settings.ShowLiveBones
				? this.skeletonInProgress.GetExtantBoneNames()
				: this.profileInProgress.Bones.Keys;

			var groupedBones = relevantBoneNames.GroupBy(x => BoneData.GetBoneFamily(x));

			foreach (var boneGroup in groupedBones.OrderBy(x => (int)x.Key))
			{
				//create a dropdown entry for the family if one doesn't already exist
				//mind that it'll only be rendered if bones exist to fill it
				if (!Settings.GroupExpandedState.TryGetValue(boneGroup.Key, out bool expanded))
				{
					Settings.GroupExpandedState[boneGroup.Key] = false;
					expanded = false;
				}

				CtrlHelper.ArrowToggle($"##{boneGroup.Key}", ref expanded);
				ImGui.SameLine();
				CtrlHelper.StaticLabel(boneGroup.Key.ToString());
				if (BoneData.DisplayableFamilies.TryGetValue(boneGroup.Key, out string? tip) && tip != null)
				{
					CtrlHelper.AddHoverText(tip);
				}

				if (expanded)
				{
					ImGui.Spacing();

					foreach (string codename in boneGroup.OrderBy(x => BoneData.GetBoneIndex(x)))
					{
						CompleteBoneEditor(codename);
					}

					ImGui.Spacing();
				}

				Settings.GroupExpandedState[boneGroup.Key] = expanded;

				ImGui.Separator();
			}

			ImGui.EndChild();

			ImGui.Separator();

			//----------------------------------

			if (ImGui.Button("Save") && this.dirty)
			{
				if (this.dirty)
				{
					Plugin.ProfileManager.SaveWorkingCopy(this.profileInProgress, false);
					this.skeletonInProgress.RebuildSkeleton();
				}
			}
			CtrlHelper.AddHoverText("Save changes and continue editing");

			ImGui.SameLine();

			if (ImGui.Button("Save and Close"))
			{
				if (this.dirty)
				{
					Plugin.ProfileManager.SaveWorkingCopy(this.profileInProgress, true);
					//Plugin.RefreshPlugin();
				}

				this.Close();
			}
			CtrlHelper.AddHoverText("Save changes and stop editing");

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			ImGui.Spacing();
			ImGui.SameLine();

			if (ImGui.Button("Revert") && this.dirty)
			{
				ConfirmationDialog.Show("Revert all unsaved work?",
					() =>
					{
						Plugin.ProfileManager.RevertWorkingCopy(profileInProgress);
						this.skeletonInProgress.RebuildSkeleton();
					});

			}
			CtrlHelper.AddHoverText("Remove all pending changes, reverting to last save");

			ImGui.SameLine();

			if (ImGui.Button("Cancel"))
			{
				//convenient data handling means we just drop it
				Plugin.ProfileManager.StopEditing(profileInProgress);
				this.Close();
			}
			CtrlHelper.AddHoverText("Close the editor without saving\n(reverting all unsaved changes)");
		}

		public void ConfirmSkeletonConnection()
		{
			if (!this.skeletonInProgress.TryLinkSkeleton())
			{
				profileInProgress.Enabled = false;

				Settings.ShowLiveBones = false;
				Settings.MirrorModeEnabled = false;
				Settings.ParentingEnabled = false;
				this.DisplayNoLinkMsg();
			}
			else if (!this.profileInProgress.Enabled)
			{
				Settings.ShowLiveBones = false;
				Settings.MirrorModeEnabled = false;
				Settings.ParentingEnabled = false;
			}
		}

		public void DisplayNoLinkMsg()
		{
			string msg = $"The editor can't find ${profileInProgress.CharName} or their bone data in the game's memory.\nCertain editing features will be disabled.";
			MessageWindow.Show(msg);
		}

		#region ImGui helper functions

		public bool ResetBoneButton(string codename, ref Vector3 value)
		{
			bool output = ImGuiComponents.IconButton(codename, FontAwesomeIcon.Recycle);
			CtrlHelper.AddHoverText($"Reset '{BoneData.GetBoneDisplayName(codename)}' to default {Settings.EditingAttribute} values");

			if (output)
			{
				value = Settings.EditingAttribute switch
				{
					BoneAttribute.Scale => Vector3.One,
					_ => Vector3.Zero
				};
			}

			return output;
		}

		private bool RevertBoneButton(string codename, ref Vector3 value)
		{
			bool output = ImGuiComponents.IconButton(codename, FontAwesomeIcon.ArrowCircleLeft);
			CtrlHelper.AddHoverText($"Revert '{BoneData.GetBoneDisplayName(codename)}' to last saved {Settings.EditingAttribute} values");

			if (output)
			{
				//if the backup scale doesn't contain bone values to revert TO, then just reset it

				if (Plugin.ProfileManager.Profiles.TryGetValue(this.profileInProgress, out var oldProf)
					&& oldProf != null
					&& oldProf.Bones.TryGetValue(codename, out var bec)
					&& bec != null)
				{
					value = Settings.EditingAttribute switch
					{
						BoneAttribute.Position => bec.Translation,
						BoneAttribute.Rotation => bec.Rotation,
						_ => bec.Scaling
					};
				}
				else
				{
					value = Settings.EditingAttribute switch
					{
						BoneAttribute.Scale => Vector3.One,
						_ => Vector3.Zero
					};
				}
			}

			return output;
		}

		private bool BoneSlider(string label, ref Vector3 value)
		{
			float velocity = Settings.EditingAttribute == BoneAttribute.Rotation ? 1.0f : 0.001f;
			float minValue = Settings.EditingAttribute == BoneAttribute.Rotation ? -360.0f : -10.0f;
			float maxValue = Settings.EditingAttribute == BoneAttribute.Rotation ? 360.0f : 10.0f;

			float temp = Settings.EditingAttribute switch
			{
				BoneAttribute.Position => 0.0f,
				BoneAttribute.Rotation => 0.0f,
				_ => value.X == value.Y && value.Y == value.Z ? value.X : 1.0f
			};


			if (ImGui.DragFloat(label, ref temp, velocity, minValue, maxValue, $"%.{precision}f"))
			{
				value = new Vector3(temp, temp, temp);
				return true;
			}
			return false;
		}

		private bool TripleBoneSlider(string label, ref Vector3 value)
		{
			float velocity = Settings.EditingAttribute == BoneAttribute.Rotation ? 1.0f : 0.001f;
			float minValue = Settings.EditingAttribute == BoneAttribute.Rotation ? -360.0f : -10.0f;
			float maxValue = Settings.EditingAttribute == BoneAttribute.Rotation ? 360.0f : 10.0f;

			return ImGui.DragFloat3(label, ref value, velocity, minValue, maxValue, $"%.{precision}f");
		}

		private void CompleteBoneRow(string codename, string col1, string col2, string col3)
		{
			BoneTransform transform = new BoneTransform();

			if (this.Settings.ShowLiveBones)
			{
				if (this.skeletonInProgress.Bones.TryGetValue(codename, out var mb)
					&& mb != null
					&& mb.PluginTransform != null)
				{
					transform = mb.PluginTransform;
				}
			}
			else if (!this.profileInProgress.Bones.TryGetValue(codename, out transform) || transform == null)
			{
				return;
			}

			string displayName = BoneData.GetBoneDisplayName(codename) ?? codename;

			bool flagUpdate = false;

			Vector3 newVector = Settings.EditingAttribute switch
			{
				BoneAttribute.Position => transform.Translation,
				BoneAttribute.Rotation => transform.Rotation,
				_ => transform.Scaling
			};

			Vector3 originalVector = newVector;

			ImGui.PushID(codename);

			flagUpdate |= ResetBoneButton(codename, ref newVector);

			flagUpdate |= RevertBoneButton(codename, ref newVector);

			//----------------------------------
			ImGui.TableNextColumn();

			float velocity = Settings.EditingAttribute == BoneAttribute.Rotation ? 1.0f : 0.001f;
			float minValue = Settings.EditingAttribute == BoneAttribute.Rotation ? -360.0f : -10.0f;
			float maxValue = Settings.EditingAttribute == BoneAttribute.Rotation ? 360.0f : 10.0f;

			flagUpdate |= ImGui.DragFloat($"##{displayName}-{col1}", ref newVector.X, velocity, minValue, maxValue, $"%.{precision}f");

			//----------------------------------
			ImGui.TableNextColumn();

			flagUpdate |= ImGui.DragFloat($"##{displayName}-{col1}", ref newVector.Y, velocity, minValue, maxValue, $"%.{precision}f");

			//----------------------------------
			ImGui.TableNextColumn();

			flagUpdate |= ImGui.DragFloat($"##{displayName}-{col1}", ref newVector.Z, velocity, minValue, maxValue, $"%.{precision}f");

			//----------------------------------
			ImGui.TableNextColumn();

			if (Settings.EditingAttribute != BoneAttribute.Scale) ImGui.BeginDisabled();

			flagUpdate |= BoneSlider($"##{displayName}AllAxes", ref newVector);

			if (Settings.EditingAttribute != BoneAttribute.Scale) ImGui.EndDisabled();

			//----------------------------------
			ImGui.TableNextColumn();

			CtrlHelper.StaticLabel(displayName, BoneData.IsIVCSBone(codename) ? $"(IVCS) {codename}" : codename);

			ImGui.PopID();

			if (flagUpdate)
			{
				this.dirty = true;

				FrameStackManager.Axis whichValue = FrameStackManager.Axis.X;
				if (originalVector.Y != newVector.Y) whichValue = FrameStackManager.Axis.Y;
				if (originalVector.Z != newVector.Z) whichValue = FrameStackManager.Axis.Z;

				this.Settings.EditStack.Do(codename, this.Settings.EditingAttribute, whichValue, originalVector, newVector);

				transform.UpdateAttribute(Settings.EditingAttribute, newVector);

				//if we have access to the armature, then use it to push the values through
				//as the bone information allows us to propagate them to siblings and children
				//otherwise access them through the profile directly

				if (this.profileInProgress.Enabled && this.skeletonInProgress.Bones.Any(x => x.Value.BoneName == codename))
				{
					this.skeletonInProgress.UpdateBoneTransform(codename, transform, Settings.MirrorModeEnabled, Settings.ParentingEnabled);
				}
				else
				{
					this.profileInProgress.Bones[codename].UpdateToMatch(transform);
				}
			}
		}

		private void CompleteBoneEditor(string codename)
		{
			BoneTransform transform = new BoneTransform();

			if (this.Settings.ShowLiveBones)
			{
				if (this.skeletonInProgress.Bones.TryGetValue(codename, out var mb)
					&& mb != null
					&& mb.PluginTransform != null)
				{
					transform = mb.PluginTransform;
				}
			}
			else if (!this.profileInProgress.Bones.TryGetValue(codename, out transform) || transform == null)
			{
				return;
			}


			string displayName = BoneData.GetBoneDisplayName(codename) ?? codename;

			bool flagUpdate = false;

			Vector3 newVector = Settings.EditingAttribute switch
			{
				BoneAttribute.Position => transform.Translation,
				BoneAttribute.Rotation => transform.Rotation,
				_ => transform.Scaling
			};

			Vector3 originalVector = newVector;

			ImGui.PushID(codename);

			flagUpdate |= ResetBoneButton(codename, ref newVector);

			ImGui.SameLine();

			flagUpdate |= RevertBoneButton(codename, ref newVector);

			ImGui.SameLine();

			ImGui.SetNextItemWidth(this.windowHorz * 3 / 6);
			flagUpdate |= TripleBoneSlider($"##{displayName}", ref newVector);

			ImGui.SameLine();

			if (Settings.EditingAttribute != BoneAttribute.Scale)
				ImGui.BeginDisabled();

			ImGui.SetNextItemWidth(this.windowHorz / 7);
			flagUpdate |= BoneSlider($"##{displayName}AllAxes", ref newVector);

			if (Settings.EditingAttribute != BoneAttribute.Scale)
				ImGui.EndDisabled();

			ImGui.SameLine();

			CtrlHelper.StaticLabel(displayName, BoneData.IsIVCSBone(codename) ? $"(IVCS) {codename}" : codename);

			ImGui.PopID();

			if (flagUpdate)
			{
				this.dirty = true;

				FrameStackManager.Axis whichValue = FrameStackManager.Axis.X;
				if (originalVector.Y != newVector.Y) whichValue = FrameStackManager.Axis.Y;
				if (originalVector.Z != newVector.Z) whichValue = FrameStackManager.Axis.Z;

				this.Settings.EditStack.Do(codename, this.Settings.EditingAttribute, whichValue, originalVector, newVector);

				transform.UpdateAttribute(Settings.EditingAttribute, newVector);

				//if we have access to the armature, then use it to push the values through
				//as the bone information allows us to propagate them to siblings and children
				//otherwise access them through the profile directly

				if (this.profileInProgress.Enabled && this.skeletonInProgress.Bones.Any(x => x.Value.BoneName == codename))
				{
					this.skeletonInProgress.UpdateBoneTransform(codename, transform, Settings.MirrorModeEnabled, Settings.ParentingEnabled);
				}
				else
				{
					this.profileInProgress.Bones[codename].UpdateToMatch(transform);
				}
			}
		}

		#endregion

	}

	public struct EditorSessionSettings
	{
		public bool ShowLiveBones = false;
		public bool MirrorModeEnabled = false;
		public bool ParentingEnabled = false;
		public BoneAttribute EditingAttribute = BoneAttribute.Scale;

		public Dictionary<BoneData.BoneFamily, bool> GroupExpandedState = new();
		public FrameStackManager EditStack;

		public EditorSessionSettings(Armature armRef)
		{
			this.EditStack = new FrameStackManager(armRef);
		}
	}


}
