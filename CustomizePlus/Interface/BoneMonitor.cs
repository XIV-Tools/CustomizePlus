// © Customize+.
// Licensed under the MIT license.

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
using CustomizePlus.Extensions;
using CustomizePlus.Memory;

namespace CustomizePlus.Interface
{
	internal class BoneMonitor : WindowBase
	{
		protected override string Title => $"Bone Monitor: {this.targetProfile.ToDebugString()}";
		protected override string DrawTitle => $"{this.Title}###customize_plus_scale_edit_window{this.Index}"; //keep the same ID for all scale editor windows

		protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

		private BoneAttribute targetAttribute = default;
		private readonly Dictionary<BoneData.BoneFamily, bool> groupExpandedState = new();
		private bool modelFrozen = false;
		private bool aggregateDeforms = false;


		private CharacterProfile targetProfile;
		private Armature? TargetSkeleton => targetProfile.Armature;

		public static void Show(CharacterProfile prof)
		{
			BoneMonitor editWnd = Plugin.InterfaceManager.Show<BoneMonitor>();

			editWnd.targetProfile = prof;

			Plugin.ArmatureManager.RenderCharacterProfiles(prof);

			editWnd.ConfirmSkeletonConnection();
		}

		protected unsafe override void DrawContents()
		{
			ImGui.TextUnformatted($"Character Name: {targetProfile.CharName}");

			ImGui.SameLine();
			ImGui.TextUnformatted($"|");

			ImGui.SameLine();
			ImGui.TextUnformatted($"Profile Name: {targetProfile.ProfName}");

			ImGui.SameLine();
			ImGui.TextUnformatted($"|");

			ImGui.SameLine();
			bool tempEnabled = this.targetProfile.Enabled;
			if (CtrlHelper.Checkbox("Live", ref tempEnabled))
			{
				this.targetProfile.Enabled = tempEnabled;
				this.ConfirmSkeletonConnection();
			}
			CtrlHelper.AddHoverText($"Hook the editor into the game to edit and preview live bone data");

			ImGui.Separator();

			if (ImGui.RadioButton("Position", targetAttribute == BoneAttribute.Position))
			{
				targetAttribute = BoneAttribute.Position;
			}

			ImGui.SameLine();
			if (ImGui.RadioButton("Rotation", targetAttribute == BoneAttribute.Rotation))
			{
				targetAttribute = BoneAttribute.Rotation;
			}

			ImGui.SameLine();
			if (ImGui.RadioButton("Scale", targetAttribute == BoneAttribute.Scale))
			{
				targetAttribute = BoneAttribute.Scale;
			}

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			ImGui.TextUnformatted("|");

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			CtrlHelper.Checkbox("Aggregate Deforms", ref this.aggregateDeforms);

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			ImGui.TextUnformatted("|");

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			if (!this.targetProfile.Enabled) ImGui.BeginDisabled();
			if (ImGui.Button("Reload Bone Data"))
			{
				this.TargetSkeleton.RebuildSkeleton();
			}
			if (!this.targetProfile.Enabled) ImGui.EndDisabled();

			ImGui.Separator();

			if (ImGui.BeginTable("Bones", 9, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollY,
				new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 56)))
			{
				ImGui.TableSetupColumn("Opt", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthFixed);

				ImGui.TableSetupColumn("\tX", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableSetupColumn("\tY", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableSetupColumn("\tZ", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableSetupColumn("\tW", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);

				ImGui.TableSetupColumn("pSke", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthFixed);
				ImGui.TableSetupColumn("Pose", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthFixed);
				ImGui.TableSetupColumn("Bone", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthFixed);

				ImGui.TableSetupColumn("Bone Name", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);

				ImGui.TableSetupScrollFreeze(0, 1);

				ImGui.TableHeadersRow();

				if (TargetSkeleton == null) return;

				var relevantBoneNames = targetProfile.Enabled
				? this.TargetSkeleton.GetExtantBoneNames()
				: this.targetProfile.Bones.Keys;

				//TODO this shouldn't be necessary, the armature shouldn't cease to exist when the profile is disabled?
				//if it must, then this section should conditionally reroute to show the saved values from within the profile
				//instead of showing nothing
				if (relevantBoneNames.Any(x => !TargetSkeleton.Bones.ContainsKey(x))) return;

				var groupedBones = relevantBoneNames.GroupBy(x => BoneData.GetBoneFamily(this.TargetSkeleton.Bones[x].BoneName));

				int lastBoneIndex = -1;

				foreach (var boneGroup in groupedBones.OrderBy(x => (int)x.Key))
				{
					ImGui.TableNextRow();
					ImGui.TableSetColumnIndex(0);

					//create a dropdown entry for the family if one doesn't already exist
					//mind that it'll only be rendered if bones exist to fill it
					if (!this.groupExpandedState.TryGetValue(boneGroup.Key, out bool expanded))
					{
						this.groupExpandedState[boneGroup.Key] = false;
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
						foreach (string codename in boneGroup.OrderBy(x => BoneData.GetBoneIndex(this.TargetSkeleton.Bones[x].BoneName)))
						{
							for (int tri = 0; tri < this.TargetSkeleton.Bones[codename].TripleIndices.Count; ++tri)
							{
								TransformationInfo(codename, tri);
							}
						}
					}

					this.groupExpandedState[boneGroup.Key] = expanded;
				}

				ImGui.EndTable();
			}

			ImGui.Separator();

			//----------------------------------

			if (ImGui.Button("Cancel"))
			{
				this.Close();
			}
		}

		public void ConfirmSkeletonConnection()
		{
			if (!this.TargetSkeleton.TryLinkSkeleton())
			{
				targetProfile.Enabled = false;
				this.DisplayNoLinkMsg();
			}
		}

		public void DisplayNoLinkMsg()
		{
			string msg = $"The editor can't find ${targetProfile.CharName} or their bone data in the game's memory.\nCertain editing features will be disabled.";
			MessageWindow.Show(msg);
		}

		#region ImGui helper functions

		private bool MysteryButton(string codename, ref Vector4 value)
		{
			bool output = ImGuiComponents.IconButton(codename, FontAwesomeIcon.ChartLine);
			CtrlHelper.AddHoverText($"This will do... something... at some point...?");

			return output;
		}

		//private bool ResetButton(string codename, ref Vector3 value)
		//{
		//	bool output = ImGuiComponents.IconButton(this.TargetSkeleton.Bones[codename].BoneName, FontAwesomeIcon.Recycle);
		//	CtrlHelper.AddHoverText($"Reset values");

		//	if (output)
		//	{
		//		value = this.targetAttribute switch
		//		{
		//			BoneAttribute.Scale => Vector3.One,
		//			_ => Vector3.Zero
		//		};
		//	}

		//	return output;
		//}

		private void TransformationInfo(string codename, int triplexIndex)
		{
			if (this.targetProfile.Enabled
				&& this.TargetSkeleton.Bones.TryGetValue(codename, out ModelBone? mb)
				&& mb != null
				&& mb.TripleIndices.ElementAtOrDefault(triplexIndex) is var triplex
				&& triplex != null
				&& mb.TryGetGameTransform(triplexIndex, out FFXIVClientStructs.Havok.hkQsTransformf deform))
			{
				string displayName = mb.GetDisplayName();

				Vector4 rowVector = deform.GetAttribute(this.targetAttribute).GetAsNumericsVector();

				if (this.aggregateDeforms)
				{
					IEnumerable<Vector4> vectors = mb.GetLineage()
						.Where(x => x.GetGameTransforms().Count() > triplexIndex)
						.Select(x => x
							.GetGameTransforms()[triplexIndex]
							.GetAttribute(this.targetAttribute)
							.GetAsNumericsVector());

					if (vectors.Any())
					{
						rowVector = vectors.Aggregate((x, y) =>
						{
							return this.targetAttribute switch
							{
								BoneAttribute.Position => x + y,
								BoneAttribute.Rotation => Quaternion.Multiply(x.ToQuaternion(), y.ToQuaternion()).GetAsNumericsVector(),
								BoneAttribute.Scale => x * y,
								_ => throw new NotImplementedException()
							};
						});
					}
				}

				ImGui.PushID(codename.GetHashCode() + triplexIndex);

				ImGui.TableNextRow();
				ImGui.TableSetColumnIndex(0);
				MysteryButton(codename, ref rowVector);

				//----------------------------------
				ImGui.PushFont(UiBuilder.MonoFont);

				ImGui.TableNextColumn();
				ImGui.TextDisabled($"{rowVector.X,8:0.00000}");

				ImGui.TableNextColumn();
				ImGui.TextDisabled($"{rowVector.Y,8:0.00000}");

				ImGui.TableNextColumn();
				ImGui.TextDisabled($"{rowVector.Z,8:0.00000}");

				ImGui.TableNextColumn();
				ImGui.TextDisabled($"{rowVector.W,8:0.00000}");

				//----------------------------------

				ImGui.TableNextColumn();
				ImGui.TextDisabled($"{triplex.Item1,3}");

				ImGui.TableNextColumn();
				ImGui.TextDisabled($"{triplex.Item2,3}");

				ImGui.TableNextColumn();
				ImGui.TextDisabled($"{triplex.Item3,3}");

				//----------------------------------

				ImGui.TableNextColumn();

				CtrlHelper.StaticLabel(mb.BoneName, displayName);

				ImGui.PopFont();

				ImGui.PopID();
			}			
		}

		#endregion
	}

}
