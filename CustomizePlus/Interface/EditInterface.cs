// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
	using System;
	using System.Collections.Generic;
	using System.Numerics;
	using Anamnesis.Posing;
	using CustomizePlus.Data;
	using CustomizePlus.Data.Configuration;
	using Dalamud.Interface;
	using Dalamud.Interface.Components;
	using Dalamud.Logging;
	using ImGuiNET;

	public class EditInterface : WindowBase
	{
		protected override string Title => $"(WIP) Edit Scale: {this.originalScaleName}";
		protected override string DrawTitle => $"{this.Title}###customize_plus_scale_edit_window{this.Index}"; //keep the same ID for all scale editor windows
		protected BodyScale? Scale { get; private set; }

		private int scaleIndex = -1;
		private int precision = 3;

		private string newScaleName = string.Empty;
		private string newScaleCharacter = string.Empty;
		private string originalScaleName = string.Empty;
		private string originalScaleCharacter = string.Empty;
		private BoneEditsContainer rootEditsContainer = new BoneEditsContainer();

		private Dictionary<string, BoneEditsContainer> boneValuesOriginal = new Dictionary<string, BoneEditsContainer>();
		private Dictionary<string, BoneEditsContainer> boneValuesNew = new Dictionary<string, BoneEditsContainer>();
		private readonly List<string> boneNamesLegacy = LegacyBoneNameConverter.GetLegacyNames();
		private readonly List<string> boneNamesModern = LegacyBoneNameConverter.GetModernNames();
		private List<string> boneNamesModernUsed = new List<string>();
		private List<string> boneNamesLegacyUsed = new List<string>();
		private bool scaleEnabled = false;
		private bool reset = false;

		private EditMode editMode;

		public static void Show(BodyScale scale)
		{
			EditInterface editWnd = Plugin.InterfaceManager.Show<EditInterface>();
			editWnd.editMode = EditMode.Scale;

			if (scale == null)
			{
				scale = new BodyScale();
			}

			editWnd.Scale = scale;
			editWnd.originalScaleName = scale.ScaleName;
			editWnd.originalScaleCharacter = scale.CharacterName;
			editWnd.newScaleName = editWnd.originalScaleName;
			editWnd.newScaleCharacter = editWnd.originalScaleCharacter;

			editWnd.scaleEnabled = scale.BodyScaleEnabled;

			for (int i = 0; i < editWnd.boneNamesLegacy.Count && i < editWnd.boneNamesModern.Count; i++)
			{
				PluginLog.Debug($"Loading bone {i}: {editWnd.boneNamesLegacy[i]}");

				BoneEditsContainer tempContainer = new BoneEditsContainer { Position = Constants.ZeroVector, Rotation = Constants.ZeroVector, Scale = Constants.OneVector };
				if (scale.Bones.TryGetValue(editWnd.boneNamesLegacy[i], out tempContainer))
				{
					PluginLog.Debug($"Found scale");
					editWnd.boneValuesOriginal.Add(editWnd.boneNamesLegacy[i], tempContainer);
					editWnd.boneValuesNew.Add(editWnd.boneNamesLegacy[i], tempContainer);
					editWnd.boneNamesModernUsed.Add(editWnd.boneNamesModern[i]);
					editWnd.boneNamesLegacyUsed.Add(editWnd.boneNamesLegacy[i]);
				}
			}

			editWnd.rootEditsContainer = scale.Bones["n_root"];

			editWnd.scaleIndex = -1;
		}

		protected override void DrawContents()
		{
			PluginConfiguration config = Plugin.ConfigurationManager.Configuration;

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
					Plugin.ConfigurationManager.SaveConfiguration();
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

			if (ImGui.RadioButton("Position", editMode == EditMode.Position))
				editMode = EditMode.Position;

			ImGui.SameLine();
			if (ImGui.RadioButton("Rotation", editMode == EditMode.Rotation))
				editMode = EditMode.Rotation;

			ImGui.SameLine();
			if (ImGui.RadioButton("Scale", editMode == EditMode.Scale))
				editMode = EditMode.Scale;

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();
			ImGui.SetNextItemWidth(300);
			ImGui.SliderInt("Precision", ref precision, 1, 6);

			if (editMode != EditMode.Scale)
			{
				ImGui.SameLine();
				ImGui.PushFont(UiBuilder.IconFont);
				ImGui.Text(FontAwesomeIcon.ExclamationTriangle.ToIconString());
				ImGui.PopFont();
				ImGui.SameLine();
				ImGui.Text($"{editMode} is an advanced setting and might not look properly with some animations, use at your own risk.");
			}

			ImGui.Separator();

			if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Recycle))
			{
				this.rootEditsContainer = new BoneEditsContainer();
				if (config.AutomaticEditMode)
				{
					this.UpdateCurrent("n_root", this.rootEditsContainer, config.AutomaticEditMode);
				}
				this.reset = true;
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Reset");

			ImGui.SameLine();

			Vector3 rootLocalTemp = Constants.OneVector;
			float rootScaleValueAllAxes = 1; //value used for scale when user just want to scale all axes
			bool isRootControlDisabled = false;
			switch (editMode)
			{
				case EditMode.Position:
					rootLocalTemp = rootEditsContainer.Position;
					rootScaleValueAllAxes = 0;
					break;
				case EditMode.Rotation:
					rootLocalTemp = Constants.ZeroVector;
					rootScaleValueAllAxes = 0;
					isRootControlDisabled = true;
					break;
				case EditMode.Scale:
					rootLocalTemp = rootEditsContainer.Scale;
					rootScaleValueAllAxes = (rootLocalTemp.X == rootLocalTemp.Y && rootLocalTemp.X == rootLocalTemp.Z && rootLocalTemp.Y == rootLocalTemp.Z) ? rootLocalTemp.X : 0;
					break;
			}

			if (isRootControlDisabled)
				ImGui.BeginDisabled();

			ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 425);
			if (ImGui.DragFloat3("##Root", ref rootLocalTemp, 0.001f, 0f, 10f, $"%.{precision}f"))
			{
				if (this.reset)
				{
					rootLocalTemp = new Vector3(1f, 1f, 1f);
					rootScaleValueAllAxes = 1;
					this.reset = false;
				}
				else if (!(rootLocalTemp.X == rootLocalTemp.Y && rootLocalTemp.X == rootLocalTemp.Z && rootLocalTemp.Y == rootLocalTemp.Z))
				{
					rootScaleValueAllAxes = 0;
				}

				switch (editMode)
				{
					case EditMode.Position:
						rootEditsContainer.Position = new Vector3(rootLocalTemp.X, rootLocalTemp.Y, rootLocalTemp.Z);
						break;
					case EditMode.Rotation:
						rootEditsContainer.Rotation = new Vector3(rootLocalTemp.X, rootLocalTemp.Y, rootLocalTemp.Z);
						break;
					case EditMode.Scale:
						rootEditsContainer.Scale = new Vector3(rootLocalTemp.X, rootLocalTemp.Y, rootLocalTemp.Z);
						break;
				}

				if (config.AutomaticEditMode)
				{
					this.UpdateCurrent("n_root", this.rootEditsContainer, config.AutomaticEditMode);
				}
			}
			if (isRootControlDisabled)
				ImGui.EndDisabled();

			ImGui.SameLine();
			if(editMode != EditMode.Scale)
				ImGui.BeginDisabled();

			ImGui.SetNextItemWidth(100);
			if (ImGui.DragFloat("##RootAllAxes", ref rootScaleValueAllAxes, 0.001f, 0f, 10f, $"%.{precision}f"))
			{
				if (rootScaleValueAllAxes != 0)
				{
					rootLocalTemp.X = rootScaleValueAllAxes;
					rootLocalTemp.Y = rootScaleValueAllAxes;
					rootLocalTemp.Z = rootScaleValueAllAxes;
				}

				rootEditsContainer.Scale = new Vector3(rootLocalTemp.X, rootLocalTemp.Y, rootLocalTemp.Z);

				if (config.AutomaticEditMode)
				{
					this.UpdateCurrent("n_root", this.rootEditsContainer, config.AutomaticEditMode);
				}
			}

			if (editMode != EditMode.Scale)
				ImGui.EndDisabled();

			ImGui.SameLine();
			ImGui.Text("Root");

			string col1Label = "X";
			string col2Label = "Y";
			string col3Label = "Z";
			string col4Label = "All";

			switch (editMode)
			{
				case EditMode.Rotation:
					col1Label = "Yaw";
					col2Label = "Pitch";
					col3Label = "Roll";
					break;
			}

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

			ImGui.BeginChild("scrolling", new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 56), false);

			for (int i = 0; i < boneValuesNew.Count; i++)
			{
				string boneNameLocalLegacy = this.boneNamesLegacyUsed[i];

				string boneNameLocalModern = this.boneNamesModernUsed[i];

				ImGui.PushID(i);

				if (!this.IsBoneNameEditable(boneNameLocalModern))
				{
					ImGui.PopID();
					continue;
				}

				BoneEditsContainer currentEditsContainer = new BoneEditsContainer { Position = Constants.ZeroVector, Rotation = Constants.ZeroVector, Scale = Constants.OneVector };
				string label = "Not Found";

				try
				{
					if (this.boneValuesNew.TryGetValue(boneNameLocalLegacy, out currentEditsContainer))
						label = boneNameLocalModern;
					else if (this.boneValuesNew.TryGetValue(boneNameLocalModern, out currentEditsContainer))
						label = boneNameLocalModern;
					else
						currentEditsContainer = new BoneEditsContainer { Position = Constants.ZeroVector, Rotation = Constants.ZeroVector, Scale = Constants.OneVector };
				}
				catch (Exception ex)
				{

				}

				Vector3 currentVector = Constants.OneVector;
				float currentScaleValueAllAxes = 1; //value used for scale when user just want to scale all axes
				switch (editMode)
				{
					case EditMode.Position:
						currentVector = currentEditsContainer.Position;
						break;
					case EditMode.Rotation:
						currentVector = currentEditsContainer.Rotation;
						break;
					case EditMode.Scale:
						currentVector = currentEditsContainer.Scale;
						currentScaleValueAllAxes = (currentVector.X == currentVector.Y && currentVector.X == currentVector.Z && currentVector.Y == currentVector.Z) ? currentVector.X : 0;
						break;
				}

				if (ImGuiComponents.IconButton(i, FontAwesomeIcon.Recycle))
				{
					this.reset = true;
				}

				if (ImGui.IsItemHovered())
					ImGui.SetTooltip($"Reset");

				if (this.reset)
				{
					BoneEditsContainer editsContainer = null;

					switch (editMode)
					{
						case EditMode.Position:
						case EditMode.Rotation:
							currentScaleValueAllAxes = 0;
							currentVector.X = 0F;
							currentVector.Y = 0F;
							currentVector.Z = 0F;
							break;
						case EditMode.Scale:
							currentScaleValueAllAxes = 1;
							currentVector.X = 1F;
							currentVector.Y = 1F;
							currentVector.Z = 1F;
							break;
					}
					this.reset = false;
					try
					{
						if (this.boneValuesNew.ContainsKey(boneNameLocalModern))
							editsContainer = this.boneValuesNew[boneNameLocalModern];
						else if (this.boneValuesNew.Remove(boneNameLocalLegacy, out BoneEditsContainer removedContainer))
						{
							editsContainer = removedContainer;
							this.boneValuesNew[boneNameLocalLegacy] = editsContainer;
						}
						else
							throw new Exception();

						switch (editMode)
						{
							case EditMode.Position:
								editsContainer.Position = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
							case EditMode.Rotation:
								editsContainer.Rotation = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
							case EditMode.Scale:
								editsContainer.Scale = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
						}
					}
					catch
					{
						//throw new Exception();
					}
					if (config.AutomaticEditMode)
					{
						this.UpdateCurrent(boneNameLocalLegacy, editsContainer, config.AutomaticEditMode);
					}
				}
				else if (currentVector.X == currentVector.Y && currentVector.Y == currentVector.Z)
				{
					currentScaleValueAllAxes = currentVector.X;
				}
				else
				{
					currentScaleValueAllAxes = 0;
				}

				ImGui.SameLine();

				ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 400);

				float minLimit = -10f;
				float maxLimit = 10f;
				float increment = 0.001f;

				switch (editMode)
				{
					case EditMode.Rotation:
						minLimit = -360f;
						maxLimit = 360f;
						increment = 1f;
						break;
				}

				if (ImGui.DragFloat3($"##{label}", ref currentVector, increment, minLimit, maxLimit, $"%.{precision}f"))
				{
					BoneEditsContainer editsContainer = null;
					try
					{
						if (this.reset)
						{
							switch (editMode)
							{
								case EditMode.Position:
								case EditMode.Rotation:
									currentScaleValueAllAxes = 0F;
									currentVector.X = 0F;
									currentVector.Y = 0F;
									currentVector.Z = 0F;
									break;
								case EditMode.Scale:
									currentScaleValueAllAxes = 1F;
									currentVector.X = 1F;
									currentVector.Y = 1F;
									currentVector.Z = 1F;
									break;
							}
							this.reset = false;
						}
						else if (!((currentVector.X == currentVector.Y) && (currentVector.X == currentVector.Z) && (currentVector.Y == currentVector.Z)))
						{
							currentScaleValueAllAxes = 0;
						}
					}
					catch (Exception ex)
					{

					}
					try
					{
						if (this.boneValuesNew.ContainsKey(boneNameLocalModern))
							editsContainer = this.boneValuesNew[boneNameLocalModern];
						else if (this.boneValuesNew.Remove(boneNameLocalLegacy, out BoneEditsContainer removedContainer))
						{
							editsContainer = removedContainer;
							this.boneValuesNew[boneNameLocalLegacy] = editsContainer;
						}
						else
							throw new Exception();

						switch (editMode)
						{
							case EditMode.Position:
								editsContainer.Position = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
							case EditMode.Rotation:
								editsContainer.Rotation = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
							case EditMode.Scale:
								editsContainer.Scale = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
						}
					}
					catch
					{
						//throw new Exception();
					}
					if (config.AutomaticEditMode)
					{
						this.UpdateCurrent(boneNameLocalLegacy, editsContainer, config.AutomaticEditMode);
					}
				}

				ImGui.SameLine();
				if (editMode != EditMode.Scale)
					ImGui.BeginDisabled();

				ImGui.SetNextItemWidth(100);
				if (ImGui.DragFloat($"##{label}AllAxes", ref currentScaleValueAllAxes, 0.001f, 0f, 10f, $"%.{precision}f"))
				{
					if (currentScaleValueAllAxes != 0)
					{
						currentVector.X = currentScaleValueAllAxes;
						currentVector.Y = currentScaleValueAllAxes;
						currentVector.Z = currentScaleValueAllAxes;
					}

					BoneEditsContainer editsContainer = null;
					if (this.boneValuesNew.ContainsKey(boneNameLocalModern))
						editsContainer = this.boneValuesNew[boneNameLocalModern];
					else if (this.boneValuesNew.Remove(boneNameLocalLegacy, out BoneEditsContainer removedContainer))
					{
						editsContainer = removedContainer;
						this.boneValuesNew[boneNameLocalLegacy] = editsContainer;
					}
					else
						throw new Exception();

					editsContainer.Scale = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);

					if (config.AutomaticEditMode)
					{
						this.UpdateCurrent(boneNameLocalLegacy, editsContainer, config.AutomaticEditMode);
					}
				}

				if (editMode != EditMode.Scale)
					ImGui.EndDisabled();

				ImGui.SameLine();
				ImGui.Text(label);

				ImGui.PopID();
			}

			ImGui.EndChild();

			ImGui.Separator();

			if (ImGui.Button("Save"))
			{
				bool forceClose = false;
				if (this.newScaleCharacter != this.originalScaleCharacter || this.newScaleName != this.originalScaleName)
					forceClose = true;

				AddToConfig(this.newScaleName, this.newScaleCharacter);
				Plugin.ConfigurationManager.SaveConfiguration();
				Plugin.LoadConfig();

				if(forceClose)
				{
					MessageWindow.Show("Customize+ detected that you have changed either character name or scale name.\nIn order to properly make a copy, the editing window was automatically closed.", new Vector2(485, 100));
					this.Close();
				}
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
				Plugin.ConfigurationManager.SaveConfiguration();
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

		//TODO: refactoring, this should use existing BodyScale for existing scales
		private void AddToConfig(string scaleName, string characterName)
		{
			PluginConfiguration config = Plugin.ConfigurationManager.Configuration;
			BodyScale newBody = new BodyScale();

			bool isSameScale = this.originalScaleName == scaleName && this.originalScaleCharacter == characterName;

			for (int i = 0; i < this.boneNamesLegacy.Count && i < this.boneValuesNew.Count; i++)
			{
				string legacyName = boneNamesLegacyUsed[i];

				//create a deep copy if we are making a copy of scale so we don't reference data from other scale
				newBody.Bones[legacyName] = isSameScale ? this.boneValuesNew[legacyName] : this.boneValuesNew[legacyName].DeepCopy();
			}

			newBody.Bones["n_root"] = isSameScale ? this.rootEditsContainer : this.rootEditsContainer.DeepCopy();

			newBody.BodyScaleEnabled = this.scaleEnabled;
			newBody.ScaleName = scaleName;
			newBody.CharacterName = characterName;

			if (isSameScale)
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
					Plugin.ConfigurationManager.ToggleOffAllOtherMatching(characterName, scaleName);
				}
			}

			this.Scale = newBody;
		}

		/*private void RevertToOriginal() //Currently Unused
		{
			this.boneValuesNew = this.boneValuesOriginal;
			this.newRootScale = this.originalRootScale;
		}*/

		private void UpdateCurrent(string boneName, BoneEditsContainer boneValue, bool autoMode = false)
		{
			PluginConfiguration config = Plugin.ConfigurationManager.Configuration;
			BodyScale newBody = this.Scale;

			newBody.Bones[boneName] = boneValue;

			if (this.scaleIndex == -1 || this.scaleIndex > config.BodyScales.Count)
			{
				this.scaleIndex = GetCurrentScaleIndex(this.originalScaleName, this.originalScaleCharacter);
			}

			config.BodyScales[this.scaleIndex] = newBody;
			Plugin.ConfigurationManager.SaveConfiguration();
			Plugin.LoadConfig(autoMode);
		}

		private int GetCurrentScaleIndex(string scaleName, string scaleCharacter)
		{
			PluginConfiguration config = Plugin.ConfigurationManager.Configuration;
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

		private bool IsBoneNameEditable(string boneNameModern)
		{
			// Megahack method
			if (boneNameModern == "Root" || boneNameModern == "Throw" || boneNameModern == "Abdomen" 
				|| boneNameModern.Contains("Cloth") || boneNameModern.Contains("Scabbard") || boneNameModern.Contains("Pauldron")
				|| boneNameModern.Contains("Holster") || boneNameModern.Contains("Poleyn") || boneNameModern.Contains("Shield")
				|| boneNameModern.Contains("Couter") || boneNameModern.Contains("Weapon") || boneNameModern.Contains("Sheathe"))
				return false;
			return true;
		}
	}

	public enum EditMode
	{
		Position,
		Rotation,
		Scale
	}
}
