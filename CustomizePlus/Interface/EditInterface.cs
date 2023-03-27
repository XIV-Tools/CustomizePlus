// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Numerics;
	using System.Windows.Forms;
	using Anamnesis.Files;
	using Anamnesis.Posing;
	using CustomizePlus.Memory;
	using Dalamud.Interface;
	using Dalamud.Interface.Components;
	using Dalamud.Logging;
	using ImGuiNET;
	using Microsoft.VisualBasic.ApplicationServices;
	using Newtonsoft.Json;
	using static System.Net.Mime.MediaTypeNames;
	using static CustomizePlus.BodyScale;

	public class EditInterface : WindowBase
	{
		protected BodyScale? Scale { get; private set; }

		protected override string Title => $"(WIP) Edit Scale: {this.originalScaleName}";
		protected BodyScale? ScaleUpdated { get; private set; }

		private int scaleIndex = -1;

		private string newScaleName = string.Empty;
		private string newScaleCharacter = string.Empty;
		private string originalScaleName = string.Empty;
		private string originalScaleCharacter = string.Empty;
		private HkVector4 originalScaleValue = HkVector4.One;
		private Vector4 newScaleValue = HkVector4.One.GetAsNumericsVector();
		private Vector4 originalRootScale = new Vector4(1f, 1f, 1f, 0f);
		private Vector4 newRootScale = HkVector4.One.GetAsNumericsVector();

		private BodyScale? scaleStart;
		private Dictionary<string, HkVector4> boneScalesOriginal = new Dictionary<string, HkVector4>();
		private Dictionary<string, HkVector4> boneScalesNew = new Dictionary<string, HkVector4>();
		private Dictionary<string, HkVector4> boneOffsetsOriginal = new Dictionary<string, HkVector4>();
		private Dictionary<string, HkVector4> boneOffsetsNew = new Dictionary<string, HkVector4>();
		private readonly List<string> boneNamesLegacy = LegacyBoneNameConverter.GetLegacyNames();
		private readonly List<string> boneNamesModern = LegacyBoneNameConverter.GetModernNames();
		private List<string> boneNamesModernUsed = new List<string>();
		private List<string> boneNamesLegacyUsed = new List<string>();
		private bool scaleEnabled = false;
		private bool reset = false;

		/*private bool showBodyBones = true;
		private bool showAccessoryBones = true;
		private bool showClothBones = false;
		private bool showArmorBones = false;
		private bool showWeaponBones = false;*/

		public enum BoneEditType : int
		{
			Scale = 0,
			Offset = 1
		};

		private int boneEditType = (int)BoneEditType.Scale;

		public void Show(BodyScale scale)
		{
			Configuration config = Plugin.Configuration;
			EditInterface editWnd = Plugin.InterfaceManager.Show<EditInterface>();
			editWnd.Scale = scale;
			editWnd.ScaleUpdated = scale;
			if (scale == null)
			{
				scale = new BodyScale();
			}

			editWnd.scaleStart = scale;
			editWnd.ScaleUpdated = scale;
			editWnd.originalScaleName = scale.ScaleName;
			editWnd.originalScaleCharacter = scale.CharacterName;
			editWnd.newScaleCharacter = scale.CharacterName;

			editWnd.scaleEnabled = scale.BodyScaleEnabled;

			for (int i = 0; i < editWnd.boneNamesLegacy.Count && i < editWnd.boneNamesModern.Count; i++)
			{
				if(scale.Bones.ContainsKey(editWnd.boneNamesLegacy[i]) || scale.Offsets.ContainsKey(editWnd.boneNamesLegacy[i]))
				{
					HkVector4 tempBone = HkVector4.One;
					HkVector4 tempOffset = HkVector4.Zero;

					if (!scale.Bones.TryGetValue(editWnd.boneNamesLegacy[i], out tempBone)) { tempBone = HkVector4.One; }
					editWnd.boneScalesOriginal.Add(editWnd.boneNamesLegacy[i], tempBone);
					editWnd.boneScalesNew.Add(editWnd.boneNamesLegacy[i], tempBone);

					if (!scale.Offsets.TryGetValue(editWnd.boneNamesLegacy[i], out tempOffset)) { tempOffset = HkVector4.Zero; }
					editWnd.boneOffsetsOriginal.Add(editWnd.boneNamesLegacy[i], tempOffset);
					editWnd.boneOffsetsNew.Add(editWnd.boneNamesLegacy[i], tempOffset);

					editWnd.boneNamesModernUsed.Add(editWnd.boneNamesModern[i]);
					editWnd.boneNamesLegacyUsed.Add(editWnd.boneNamesLegacy[i]);
				}
			}

			editWnd.originalRootScale = scale.RootScale.GetAsNumericsVector();

			editWnd.newRootScale = editWnd.originalRootScale;

			editWnd.originalScaleName = scale.ScaleName;
			editWnd.originalScaleCharacter = scale.CharacterName;
			editWnd.newScaleName = editWnd.originalScaleName;
			editWnd.newScaleCharacter = editWnd.originalScaleCharacter;

			editWnd.scaleIndex = -1;
		}

		protected override void DrawContents()
		{
			Configuration config = Plugin.Configuration;

			string newScaleNameTemp = this.newScaleName;
			string newScaleCharacterTemp = this.newScaleCharacter;
			bool enabledTemp = this.scaleEnabled;
			bool resetTemp = this.reset;

			if (ImGui.Checkbox("Enable", ref enabledTemp))
			{
				this.scaleEnabled = enabledTemp;
				if (config.AutomaticEditMode)
				{
					AddToConfig(this.newScaleName, this.newScaleCharacter);
					config.Save();
					Plugin.LoadConfig(true);
				}
			}

			ImGui.SameLine();

			ImGui.SetNextItemWidth(200);

			if (ImGui.InputText("Character Name", ref newScaleCharacterTemp, 1024))
			{
				this.newScaleCharacter = newScaleCharacterTemp;
			}

			ImGui.SameLine();

			ImGui.SetNextItemWidth(300);
			if (ImGui.InputText("Scale Name", ref newScaleNameTemp, 1024))
			{
				this.newScaleName = newScaleNameTemp;
			}

			ImGui.SameLine();

			bool autoModeEnable = config.AutomaticEditMode;
			if (ImGui.Checkbox("Automatic Mode", ref autoModeEnable))
			{
				config.AutomaticEditMode = autoModeEnable;
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Applies changes automatically without saving.");

			ImGui.Separator();

			Vector4 rootScaleLocal = this.newRootScale;

			if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Recycle))
			{
				rootScaleLocal = new Vector4(1f, 1f, 1f, 1f);
				this.newRootScale = rootScaleLocal;
				if (config.AutomaticEditMode)
				{
					this.UpdateCurrent("Root", new HkVector4(1f, 1f, 1f, 1f), config.AutomaticEditMode);
				}
				this.reset = true;
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Reset");

			ImGui.SameLine();

			Vector4 rootScaleLocalTemp = new Vector4((float)rootScaleLocal.X, (float)rootScaleLocal.Y, (float)rootScaleLocal.Z, (float)rootScaleLocal.W);

			if (ImGui.DragFloat4("Root", ref rootScaleLocalTemp, 0.001f, 0f, 10f))
			{
				if (this.reset)
				{
					rootScaleLocalTemp = new Vector4(1f, 1f, 1f, 1f);
					this.reset = false;
				}
				else if (!((rootScaleLocalTemp.X == rootScaleLocalTemp.Y) && (rootScaleLocalTemp.X == rootScaleLocalTemp.Z) && (rootScaleLocalTemp.Y == rootScaleLocalTemp.Z)))
				{
					rootScaleLocalTemp.W = 0;
				}
				else if (rootScaleLocalTemp.W != 0)
				{
					rootScaleLocalTemp.X = rootScaleLocalTemp.W;
					rootScaleLocalTemp.Y = rootScaleLocalTemp.W;
					rootScaleLocalTemp.Z = rootScaleLocalTemp.W;
				}
				rootScaleLocal = new Vector4(rootScaleLocalTemp.X, rootScaleLocalTemp.Y, rootScaleLocalTemp.Z, rootScaleLocalTemp.W);
				this.newRootScale = rootScaleLocal;
				if (config.AutomaticEditMode)
				{
					this.UpdateCurrent("Root", new HkVector4(rootScaleLocal.X, rootScaleLocal.Y, rootScaleLocal.Z, rootScaleLocalTemp.W), config.AutomaticEditMode);
				}
			}
			
			bool editBodyBones = config.EditBodyBones;
			bool editAccessoryBones = config.EditAccessoryBones;
			bool editClothBones = config.EditClothBones;
			bool editArmorBones = config.EditArmorBones;
			bool editWeaponBones = config.EditWeaponBones;
			
			ImGui.Separator();
			ImGui.Text("Filters:");
			ImGui.SameLine();
			if(ImGui.Checkbox("Body", ref editBodyBones))
			{
				config.EditBodyBones = editBodyBones;
			}
			ImGui.SameLine();
			if (ImGui.Checkbox("Accessory", ref editAccessoryBones))
			{
				config.EditAccessoryBones = editAccessoryBones;
			}
			ImGui.SameLine();
			if (ImGui.Checkbox("Cloth", ref editClothBones))
			{
				config.EditClothBones = editClothBones;
			}
			ImGui.SameLine();
			if (ImGui.Checkbox("Armor", ref editArmorBones))
			{
				config.EditArmorBones = editArmorBones;
			}
			ImGui.SameLine();
			if (ImGui.Checkbox("Weapon (Broken)", ref editWeaponBones))
			{
				config.EditWeaponBones = editWeaponBones;
			}

			ImGui.Separator();
			ImGui.Text("Edit:");
			ImGui.SameLine();
			ImGui.RadioButton("Scale", ref this.boneEditType, (int)BoneEditType.Scale);
			ImGui.SameLine();
			ImGui.RadioButton("Offset", ref this.boneEditType, (int)BoneEditType.Offset);

			ImGui.Separator();
			ImGui.BeginTable("Bones", this.boneEditType == (int)BoneEditType.Scale ? 6 : 5, ImGuiTableFlags.SizingStretchSame & ImGuiTableFlags.Borders);
			ImGui.TableSetupColumn("Bones:", ImGuiTableColumnFlags.WidthFixed, 36);
			ImGui.TableSetupColumn("X", ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableSetupColumn("Y", ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableSetupColumn("Z", ImGuiTableColumnFlags.WidthStretch);
			if (this.boneEditType == (int)BoneEditType.Scale)
			{
				ImGui.TableSetupColumn("All", ImGuiTableColumnFlags.WidthStretch);
			}
			ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 150);

			ImGui.TableNextColumn();
			ImGui.Text("Bones:");
			ImGui.TableNextColumn();
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ((ImGui.GetColumnWidth() - ImGui.CalcTextSize("X").X) / 2));
			ImGui.Text("X");
			ImGui.TableNextColumn();
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ((ImGui.GetColumnWidth() - ImGui.CalcTextSize("Y").X) / 2));
			ImGui.Text("Y");
			ImGui.TableNextColumn();
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ((ImGui.GetColumnWidth() - ImGui.CalcTextSize("Z").X) / 2));
			ImGui.Text("Z");
			if(this.boneEditType == (int)BoneEditType.Scale)
			{
				ImGui.TableNextColumn();
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ((ImGui.GetColumnWidth() - ImGui.CalcTextSize("All").X) / 2));
				ImGui.Text("All");
			}
			ImGui.TableNextColumn();
			ImGui.Text("Name");
			ImGui.EndTable();

			ImGui.BeginChild("scrolling", new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 56), false);

			int boneCount = this.boneEditType == (int)BoneEditType.Scale ? boneScalesNew.Count : boneOffsetsNew.Count;
			for (int i = 0; i < boneCount; i++)
			{
				string boneNameLocalLegacy = this.boneNamesLegacyUsed[i];

				string boneNameLocalModern = this.boneNamesModernUsed[i];

				ImGui.PushID(i);

				if (!this.IsBoneNameEditable(boneNameLocalModern, config))
				{
					ImGui.PopID();
					continue;
				}
				
				switch(this.boneEditType)
				{
					case (int)BoneEditType.Scale:
						DrawBoneScale(config, i, boneNameLocalLegacy, boneNameLocalModern);
						break;
					case (int)BoneEditType.Offset:
						DrawBoneOffset(config, i, boneNameLocalLegacy, boneNameLocalModern);
						break;
				}

				ImGui.PopID();
			}

			ImGui.EndChild();

			ImGui.Separator();

			if (ImGui.Button("Save"))
			{
				AddToConfig(this.newScaleName, this.newScaleCharacter);
				if (this.newScaleCharacter != this.originalScaleCharacter)
					this.originalScaleCharacter = this.newScaleCharacter;
				if (this.newScaleName != this.originalScaleName)
					this.originalScaleName = this.newScaleName;
				config.Save();
				Plugin.LoadConfig();
			}

			/* TODO feature: undo of some variety. Option below is a revert to what was present when edit was opened, but needs additonal logic
			 * ImGui.SameLine();
			if (ImGui.Button("Revert"))
			{
				RevertToOriginal();
				//config.Save();
			}
			*/

			ImGui.SameLine();

			if (ImGui.Button("Save and Close"))
			{
				AddToConfig(this.newScaleName, this.newScaleCharacter);
				config.Save();
				Plugin.LoadConfig();
				this.Close();
			}
			ImGui.SameLine();
			if (ImGui.Button("Cancel"))
			{
				this.Close();
			}

			ImGui.SameLine();

			ImGui.Text("    Save and close with new scale name or new character name to create a copy.");
		}

		private void DrawBoneScale(Configuration config, int i, string boneNameLocalLegacy, string boneNameLocalModern)
		{
			HkVector4 currentHkVector = HkVector4.One;
			string label = "Not Found";

			try
			{
				if (this.boneScalesNew.TryGetValue(boneNameLocalLegacy, out currentHkVector))
				{
					label = boneNameLocalModern;
				}
				else if (this.boneScalesNew.TryGetValue(boneNameLocalModern, out currentHkVector))
				{
					label = boneNameLocalModern;
				}
				else
				{
					currentHkVector = HkVector4.One;
				}
			}
			catch (Exception ex)
			{

			}

			Vector4 currentVector4 = currentHkVector.GetAsNumericsVector();

			if (ImGuiComponents.IconButton(i, FontAwesomeIcon.Recycle))
			{
				this.reset = true;
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Reset");

			if (this.reset)
			{
				currentVector4.W = 1F;
				currentVector4.X = 1F;
				currentVector4.Y = 1F;
				currentVector4.Z = 1F;
				this.reset = false;
				try
				{
					if (this.boneScalesNew.ContainsKey(boneNameLocalModern))
					{
						this.boneScalesNew[boneNameLocalModern] = new HkVector4(currentVector4.X, currentVector4.Y, currentVector4.Z, currentVector4.W);
					}
					else if (this.boneScalesNew.Remove(boneNameLocalLegacy))
					{
						this.boneScalesNew[boneNameLocalLegacy] = new HkVector4(currentVector4.X, currentVector4.Y, currentVector4.Z, currentVector4.W);
					}
					else
					{
						throw new Exception();
					}
				}
				catch
				{
					//throw new Exception();
				}
				if (config.AutomaticEditMode)
				{
					this.UpdateCurrent(boneNameLocalLegacy, new HkVector4(currentVector4.X, currentVector4.Y, currentVector4.Z, currentVector4.W), config.AutomaticEditMode);
				}
			}
			else if (currentVector4.X == currentVector4.Y && currentVector4.Y == currentVector4.Z)
			{
				currentVector4.W = currentVector4.X;
			}
			else
			{
				currentVector4.W = 0;
			}

			ImGui.SameLine(36);

			ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 190);
			if (ImGui.DragFloat4(label, ref currentVector4, 0.001f, 0f, 10f))
			{
				try
				{
					if (this.reset)
					{
						currentVector4.W = 1F;
						currentVector4.X = 1F;
						currentVector4.Y = 1F;
						currentVector4.Z = 1F;
						this.reset = false;
					}
					else if (!((currentVector4.X == currentVector4.Y) && (currentVector4.X == currentVector4.Z) && (currentVector4.Y == currentVector4.Z)))
					{
						currentVector4.W = 0;
					}
					else if (currentVector4.W != 0)
					{
						currentVector4.X = currentVector4.W;
						currentVector4.Y = currentVector4.W;
						currentVector4.Z = currentVector4.W;
					}
				}
				catch (Exception ex)
				{

				}
				try
				{
					if (this.boneScalesNew.ContainsKey(boneNameLocalModern))
					{
						this.boneScalesNew[boneNameLocalModern] = new HkVector4(currentVector4.X, currentVector4.Y, currentVector4.Z, currentVector4.W);
					}
					else if (this.boneScalesNew.Remove(boneNameLocalLegacy))
					{
						this.boneScalesNew[boneNameLocalLegacy] = new HkVector4(currentVector4.X, currentVector4.Y, currentVector4.Z, currentVector4.W);
					}
					else
					{
						throw new Exception();
					}
				}
				catch
				{
					//throw new Exception();
				}
				if (config.AutomaticEditMode)
				{
					this.UpdateCurrent(boneNameLocalLegacy, new HkVector4(currentVector4.X, currentVector4.Y, currentVector4.Z, currentVector4.W), config.AutomaticEditMode);
				}
			}
		}

		private void DrawBoneOffset(Configuration config, int i, string boneNameLocalLegacy, string boneNameLocalModern)
		{
			HkVector4 currentHkVector = HkVector4.One;
			string label = "Not Found";

			try
			{
				if (this.boneOffsetsNew.TryGetValue(boneNameLocalLegacy, out currentHkVector))
				{
					label = boneNameLocalModern;
				}
				else if (this.boneOffsetsNew.TryGetValue(boneNameLocalModern, out currentHkVector))
				{
					label = boneNameLocalModern;
				}
				else
				{
					currentHkVector = HkVector4.One;
				}
			}
			catch (Exception ex)
			{

			}

			Vector4 currentVector4 = currentHkVector.GetAsNumericsVector();
			Vector3 currentVector3 = new Vector3(currentVector4.X, currentVector4.Y, currentVector4.Z);

			if (ImGuiComponents.IconButton(i, FontAwesomeIcon.Recycle))
			{
				this.reset = true;
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Reset");

			if (this.reset)
			{
				currentVector3.X = 0F;
				currentVector3.Y = 0F;
				currentVector3.Z = 0F;
				this.reset = false;
				try
				{
					if (this.boneOffsetsNew.ContainsKey(boneNameLocalModern))
					{
						this.boneOffsetsNew[boneNameLocalModern] = new HkVector4(currentVector3.X, currentVector3.Y, currentVector3.Z, 1f);
					}
					else if (this.boneOffsetsNew.Remove(boneNameLocalLegacy))
					{
						this.boneOffsetsNew[boneNameLocalLegacy] = new HkVector4(currentVector3.X, currentVector3.Y, currentVector3.Z, 1f);
					}
					else
					{
						throw new Exception();
					}
				}
				catch
				{
					//throw new Exception();
				}
				if (config.AutomaticEditMode)
				{
					this.UpdateCurrentOffset(boneNameLocalLegacy, new HkVector4(currentVector3.X, currentVector3.Y, currentVector3.Z, 1f), config.AutomaticEditMode);
				}
			}

			ImGui.SameLine(36);

			ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 190);
			if (ImGui.DragFloat3(label, ref currentVector3, 0.001f, -5f, 5f))
			{
				try
				{
					if (this.reset)
					{
						currentVector3.X = 0F;
						currentVector3.Y = 0F;
						currentVector3.Z = 0F;
						this.reset = false;
					}
				}
				catch (Exception ex)
				{

				}
				try
				{
					if (this.boneOffsetsNew.ContainsKey(boneNameLocalModern))
					{
						this.boneOffsetsNew[boneNameLocalModern] = new HkVector4(currentVector3.X, currentVector3.Y, currentVector3.Z, 1f);
					}
					else if (this.boneOffsetsNew.Remove(boneNameLocalLegacy))
					{
						this.boneOffsetsNew[boneNameLocalLegacy] = new HkVector4(currentVector3.X, currentVector3.Y, currentVector3.Z, 1f);
					}
					else
					{
						throw new Exception();
					}
				}
				catch
				{
					//throw new Exception();
				}
				if (config.AutomaticEditMode)
				{
					this.UpdateCurrentOffset(boneNameLocalLegacy, new HkVector4(currentVector3.X, currentVector3.Y, currentVector3.Z, 1f), config.AutomaticEditMode);
				}
			}
		}

		private void AddToConfig(string scaleName, string characterName)
		{
			Configuration config = Plugin.Configuration;
			BodyScale newBody = new BodyScale();

			for (int i = 0; i < this.boneNamesLegacy.Count && i < this.boneScalesNew.Count; i++)
			{
				string legacyName = boneNamesLegacyUsed[i];

				if (!this.ScaleUpdated.Bones.ContainsKey(legacyName))
					newBody.Bones.Add(legacyName, this.boneScalesNew[legacyName]);

				newBody.Bones[legacyName] = this.boneScalesNew[legacyName];
			}

			for (int i = 0; i < this.boneNamesLegacy.Count && i < this.boneOffsetsNew.Count; i++)
			{
				string legacyName = boneNamesLegacyUsed[i];

				if (!this.ScaleUpdated.Offsets.ContainsKey(legacyName))
					newBody.Offsets.Add(legacyName, this.boneOffsetsNew[legacyName]);

				newBody.Offsets[legacyName] = this.boneOffsetsNew[legacyName];
			}

			newBody.BodyScaleEnabled = this.scaleEnabled;
			newBody.ScaleName = scaleName;
			newBody.CharacterName = characterName;

			newBody.RootScale = new HkVector4(this.newRootScale.X, this.newRootScale.Y, this.newRootScale.Z, 0);
			if (this.originalScaleName == scaleName && this.originalScaleCharacter == characterName)
			{
				int matchIndex = -1;
				for (int i = 0; i < config.BodyScales.Count; i++)
				{
					if (config.BodyScales[i].ScaleName == scaleName && config.BodyScales[i].CharacterName == characterName)
					{
						matchIndex = i;
						break;
					}
				}
				if (matchIndex >= 0)
				{
					config.BodyScales.RemoveAt(matchIndex);
					config.BodyScales.Insert(matchIndex, newBody);
				}
			}
			else
			{
				this.originalScaleName = scaleName;
				this.originalScaleCharacter = characterName;
				config.BodyScales.Add(newBody);
				if (this.scaleEnabled)
				{
					config.ToggleOffAllOtherMatching(characterName, scaleName);
				}
			}
		}

		private void RevertToOriginal() //Currently Unused
		{
			this.boneScalesNew = this.boneScalesOriginal;
			this.newRootScale = this.originalRootScale;
		}

		private void UpdateCurrent(string boneName, HkVector4 boneValue, bool autoMode = false)
		{
			Configuration config = Plugin.Configuration;
			BodyScale newBody = this.ScaleUpdated;

			if (boneName == "Root")
			{
				newBody.RootScale = boneValue;
			}
			else
			{
				newBody.Bones[boneName] = boneValue;
			}

			if (this.scaleIndex == -1 || this.scaleIndex > config.BodyScales.Count)
			{
				this.scaleIndex = GetCurrentScaleIndex(this.originalScaleName, this.originalScaleCharacter);
			}

			config.BodyScales[this.scaleIndex] = newBody;
			config.Save();
			Plugin.LoadConfig(autoMode);
		}

		private void UpdateCurrentOffset(string boneName, HkVector4 offsetValue, bool autoMode = false)
		{
			Configuration config = Plugin.Configuration;
			BodyScale newBody = this.ScaleUpdated;

			if (boneName == "Root")
			{
				return;
			}
			else
			{
				newBody.Offsets[boneName] = offsetValue;
			}

			if (this.scaleIndex == -1 || this.scaleIndex > config.BodyScales.Count)
			{
				this.scaleIndex = GetCurrentScaleIndex(this.originalScaleName, this.originalScaleCharacter);
			}

			config.BodyScales[this.scaleIndex] = newBody;
			config.Save();
			Plugin.LoadConfig(autoMode);
		}

		private int GetCurrentScaleIndex(string scaleName, string scaleCharacter)
		{
			Configuration config = Plugin.Configuration;
			int matchIndex = -1;
			for (int i = 0; i < config.BodyScales.Count; i++)
			{
				if (config.BodyScales[i].ScaleName == scaleName && config.BodyScales[i].CharacterName == scaleCharacter)
				{
					matchIndex = i;
					break;
				}
			}
			if (matchIndex >= 0)
			{
				return matchIndex;
			}
			return -1;
		}

		private bool IsBoneNameEditable(string boneNameModern, Configuration? config = null)
		{
			config = config ?? Plugin.Configuration;
			// Megahack method
			if (boneNameModern == "Root" || boneNameModern == "Throw" || boneNameModern == "Abdomen")
				return false;
			if (boneNameModern.Contains("Earring"))
				return config.EditAccessoryBones;
			if(boneNameModern.Contains("Cloth"))
				return config.EditClothBones;
			if (boneNameModern.Contains("Pauldron") || boneNameModern.Contains("Poleyn") || boneNameModern.Contains("Couter"))
				return config.EditArmorBones;
			if (boneNameModern.Contains("Scabbard") || boneNameModern.Contains("Holster") || boneNameModern.Contains("Shield")
				|| boneNameModern.Contains("Weapon") || boneNameModern.Contains("Sheathe"))
				return config.EditWeaponBones;
			/*if (boneNameModern == "Root" || boneNameModern == "Throw" || boneNameModern == "Abdomen" 
				|| boneNameModern.Contains("Cloth") || boneNameModern.Contains("Scabbard") || boneNameModern.Contains("Pauldron")
				|| boneNameModern.Contains("Holster") || boneNameModern.Contains("Poleyn") || boneNameModern.Contains("Shield")
				|| boneNameModern.Contains("Couter") || boneNameModern.Contains("Weapon") || boneNameModern.Contains("Sheathe"))
				return false;*/
			return config.EditBodyBones;
		}
	}
}
