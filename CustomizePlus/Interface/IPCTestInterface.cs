// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Numerics;
	using System.Windows.Forms;
	using Anamnesis.Files;
	using Anamnesis.Posing;
	using CustomizePlus.Memory;
	using Dalamud.Interface;
	using Dalamud.Interface.Components;
	using Dalamud.Logging;
	using Dalamud.Plugin;
	using ImGuiNET;
	using Newtonsoft.Json;
	using static CustomizePlus.BodyScale;
	using Dalamud.Plugin.Ipc;
	using CustomizePlus.Helpers;
	using CustomizePlus.Data;
	using CustomizePlus.Data.Configuration;

	public class IPCTestInterface : WindowBase
	{
		//private DalamudPluginInterface localPlugin;
		private bool subscribed = true;

		private ICallGateSubscriber<string, string>? getBodyScale;
		//private readonly ICallGateSubscriber<Character?, string?>? ProviderGetBodyScaleFromCharacter;
		private ICallGateSubscriber<string, string, object>? setBodyScale;
		//private readonly ICallGateSubscriber<string, Character?, object>? ProviderSetBodyScaleToCharacter;
		private ICallGateSubscriber<string, object>? revert;
		//private readonly ICallGateSubscriber<Character?, object>? ProviderRevertCharacter;
		//private readonly ICallGateSubscriber<string>? _getApiVersion;
		//private readonly ICallGateSubscriber<string?, object?>? _onScaleUpdate;

		public IPCTestInterface()
		{
			//localPlugin = pi;
		}

		private void SubscribeEvents()
		{
			if (!subscribed)
			{
				subscribed = true;
			}
		}

		public void UnsubscribeEvents()
		{
			if (subscribed)
			{
				subscribed = false;
			}
		}

		public override void Dispose()
		{
			subscribed = false;
		}

		protected BodyScale? Scale { get; private set; }

		protected override string Title => $"(WIP) IPC Test: {this.newScaleCharacter}";
		protected BodyScale? ScaleUpdated { get; private set; }

		private string newScaleName = string.Empty;
		private string newScaleCharacter = string.Empty;
		private string originalScaleName = string.Empty;
		private string originalScaleCharacter = string.Empty;
		private BoneEditsContainer rootEditsContainer = new BoneEditsContainer();

		private Dictionary<string, BoneEditsContainer> boneValuesOriginal = new Dictionary<string, BoneEditsContainer>();
		private Dictionary<string, BoneEditsContainer> boneValuesNew = new Dictionary<string, BoneEditsContainer>();
		private readonly List<string> boneCodenames = BoneData.GetBoneCodenames();
		private readonly List<string> boneDispNames = BoneData.GetBoneDisplayNames();
		private List<string> boneDispNamesUsed = new List<string>();
		private List<string> boneCodenamesUsed = new List<string>();
		private bool scaleEnabled = false;
		private bool reset = false;

		private bool automaticEditMode = false;

		private EditMode editMode;

		public static void Show(DalamudPluginInterface pi)
		{
			DalamudPluginInterface localPlugin = pi;
			IPCTestInterface editWnd = Plugin.InterfaceManager.Show<IPCTestInterface>();
			editWnd.getBodyScale = localPlugin.GetIpcSubscriber<string, string>("CustomizePlus.GetBodyScale");
			//localPlugin.GetIpcSubscriber<Character?, string?> ProviderGetBodyScaleFromCharacter;
			editWnd.setBodyScale = localPlugin.GetIpcSubscriber<string, string, object>("CustomizePlus.SetBodyScale");
			//localPlugin.GetIpcSubscriber<string, Character?, object> ProviderSetBodyScaleToCharacter;
			editWnd.revert = localPlugin.GetIpcSubscriber<string, object>("CustomizePlus.Revert");
			//localPlugin.GetIpcSubscriber<Character?, object>? ProviderRevertCharacter;
			//_getApiVersion = localPlugin.GetIpcSubscriber<string>("CustomizePlus.GetApiVersion");
			//_onScaleUpdate = localPlugin.GetIpcSubscriber<string?, object?>("CustomizePlus.OnScaleUpdate"); ;
			//UnsubscribeEvents();


			var scale = BodyScale.BuildDefault();
			editWnd.Scale = scale;
			editWnd.ScaleUpdated = scale;
			if (scale == null)
			{
				
			}

			editWnd.ScaleUpdated = scale;
			editWnd.originalScaleName = scale.ScaleName;
			editWnd.originalScaleCharacter = scale.CharacterName;
			editWnd.newScaleCharacter = scale.CharacterName;

			editWnd.scaleEnabled = scale.BodyScaleEnabled;

			for (int i = 0; i < editWnd.boneCodenames.Count && i < editWnd.boneDispNames.Count; i++)
			{
				BoneEditsContainer tempContainer = new BoneEditsContainer { Scale = Constants.OneVector };
				if (scale.Bones.TryGetValue(editWnd.boneCodenames[i], out tempContainer))
				{
					editWnd.boneValuesOriginal.Add(editWnd.boneCodenames[i], tempContainer);
					editWnd.boneValuesNew.Add(editWnd.boneCodenames[i], tempContainer);
					editWnd.boneDispNamesUsed.Add(editWnd.boneDispNames[i]);
					editWnd.boneCodenamesUsed.Add(editWnd.boneCodenames[i]);
				}
			}

			editWnd.originalScaleName = scale.ScaleName;
			editWnd.originalScaleCharacter = scale.CharacterName;
			editWnd.newScaleName = editWnd.originalScaleName;
			editWnd.newScaleCharacter = editWnd.originalScaleCharacter;

			
			//DrawContents();
		}

		protected override void DrawContents()
		{
			try
			{
				SubscribeEvents();
				DrawScaleEdit(new BodyScale(), DalamudServices.PluginInterface);
			}
			catch (Exception e)
			{
				PluginLog.LogError($"Error during IPC Tests:\n{e}");
			}
		}

		public void DrawScaleEdit(BodyScale scale, DalamudPluginInterface pi)
		{
			string newScaleNameTemp = this.newScaleName;
			string newScaleCharacterTemp = this.newScaleCharacter;
			bool enabledTemp = this.scaleEnabled;
			bool resetTemp = this.reset;

			if (ImGui.Checkbox("Enable", ref enabledTemp))
			{
				this.scaleEnabled = enabledTemp;
				if (automaticEditMode)
				{

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

			bool autoModeEnable = automaticEditMode;
			if (ImGui.Checkbox("Automatic Mode", ref autoModeEnable))
			{
				automaticEditMode = autoModeEnable;
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

			ImGui.Separator();

			if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Recycle))
			{
				this.rootEditsContainer = new BoneEditsContainer();
				if (automaticEditMode)
				{
					this.UpdateCurrent("n_root", this.rootEditsContainer);
				}
				this.reset = true;
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Reset");

			ImGui.SameLine();

			Vector3 rootLocalTemp = Constants.OneVector;
			bool isRootControlDisabled = false;
			switch (editMode)
			{
				case EditMode.Position:
					rootLocalTemp = rootEditsContainer.Position;
					break;
				case EditMode.Rotation:
					rootLocalTemp = Constants.ZeroVector;
					isRootControlDisabled = true;
					break;
				case EditMode.Scale:
					rootLocalTemp = rootEditsContainer.Scale;
					break;
			}

			if (isRootControlDisabled)
				ImGui.BeginDisabled();
			if (ImGui.DragFloat3("Root", ref rootLocalTemp, 0.001f, 0f, 10f))
			{
				if (this.reset)
				{
					rootLocalTemp = new Vector3(1f, 1f, 1f);
					this.reset = false;
				}
				/*else if (!((rootLocalTemp.X == rootLocalTemp.Y) && (rootLocalTemp.X == rootLocalTemp.Z) && (rootLocalTemp.Y == rootLocalTemp.Z)))
				{
					rootLocalTemp.W = 0;
				}
				else if (rootLocalTemp.W != 0)
				{
					rootLocalTemp.X = rootLocalTemp.W;
					rootLocalTemp.Y = rootLocalTemp.W;
					rootLocalTemp.Z = rootLocalTemp.W;
				}*/

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

				if (automaticEditMode)
				{
					this.UpdateCurrent("n_root", this.rootEditsContainer);
				}
			}
			if (isRootControlDisabled)
				ImGui.EndDisabled();

			string col1Label = "X";
			string col2Label = "Y";
			string col3Label = "Z";
			string col4Label = "All";

			switch (editMode)
			{
				case EditMode.Position:
					col4Label = "Unused";
					break;
				case EditMode.Rotation:
					col1Label = "Roll";
					col2Label = "Yaw";
					col3Label = "Pitch";
					col4Label = "Unused";
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
				string codenameLocal = this.boneCodenamesUsed[i];

				string dispNameLocal = this.boneDispNamesUsed[i];

				ImGui.PushID(i);

				/*
				if (!BoneData.IsEditableBone(codenameLocal))
				{
					ImGui.PopID();
					continue;
				}
				*/

				BoneEditsContainer currentEditsContainer = new BoneEditsContainer { Position = Constants.ZeroVector, Rotation = Constants.ZeroVector, Scale = Constants.OneVector };
				string label = "Not Found";

				try
				{
					if (this.boneValuesNew.TryGetValue(codenameLocal, out currentEditsContainer))
						label = dispNameLocal;
					else if (this.boneValuesNew.TryGetValue(dispNameLocal, out currentEditsContainer))
						label = dispNameLocal;
					else
						currentEditsContainer = new BoneEditsContainer { Position = Constants.ZeroVector, Rotation = Constants.ZeroVector, Scale = Constants.OneVector };
				}
				catch (Exception ex)
				{

				}

				Vector3 currentVector = Constants.OneVector;
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
							//currentVector.W = 0F;
							currentVector.X = 0F;
							currentVector.Y = 0F;
							currentVector.Z = 0F;
							break;
						case EditMode.Scale:
							//currentVector.W = 1F;
							currentVector.X = 1F;
							currentVector.Y = 1F;
							currentVector.Z = 1F;
							break;
					}
					this.reset = false;
					try
					{
						if (this.boneValuesNew.ContainsKey(dispNameLocal))
							editsContainer = this.boneValuesNew[dispNameLocal];
						else if (this.boneValuesNew.Remove(codenameLocal, out BoneEditsContainer removedContainer))
						{
							editsContainer = removedContainer;
							this.boneValuesNew[codenameLocal] = editsContainer;
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
					if (automaticEditMode)
					{
						this.UpdateCurrent(codenameLocal, editsContainer);
					}
				}
				/*else if (currentVector.X == currentVector.Y && currentVector.Y == currentVector.Z)
				{
					currentVector.W = currentVector.X;
				}
				else
				{
					currentVector.W = 0;
				}*/

				ImGui.SameLine();

				ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 190);

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

				if (ImGui.DragFloat3(label, ref currentVector, increment, minLimit, maxLimit))
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
									//currentVector.W = 0F;
									currentVector.X = 0F;
									currentVector.Y = 0F;
									currentVector.Z = 0F;
									break;
								case EditMode.Scale:
									//currentVector.W = 1F;
									currentVector.X = 1F;
									currentVector.Y = 1F;
									currentVector.Z = 1F;
									break;
							}
							this.reset = false;
						}
						/*else if (!((currentVector.X == currentVector.Y) && (currentVector.X == currentVector.Z) && (currentVector.Y == currentVector.Z)))
						{
							currentVector.W = 0;
						}
						else if (currentVector.W != 0)
						{
							currentVector.X = currentVector.W;
							currentVector.Y = currentVector.W;
							currentVector.Z = currentVector.W;
						}*/
					}
					catch (Exception ex)
					{

					}
					try
					{
						if (this.boneValuesNew.ContainsKey(dispNameLocal))
							editsContainer = this.boneValuesNew[dispNameLocal];
						else if (this.boneValuesNew.Remove(codenameLocal, out BoneEditsContainer removedContainer))
						{
							editsContainer = removedContainer;
							this.boneValuesNew[codenameLocal] = editsContainer;
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
					if (automaticEditMode)
					{
						this.UpdateCurrent(codenameLocal, editsContainer);
					}
				}


				ImGui.PopID();
			}

			ImGui.EndChild();

			ImGui.Separator();

			if (ImGui.Button("Save"))
			{
				ApplyViaIPC(this.newScaleName, this.newScaleCharacter, pi);
			}

			
			ImGui.SameLine();
			if (ImGui.Button("Reset"))
			{
				RevertToOriginal(this.newScaleCharacter, pi);
			}

			ImGui.SameLine();
			if (ImGui.Button("Load from IPC"))
			{
				GetFromIPC(this.newScaleCharacter, pi);
			}

			
		}

		private void ApplyViaIPC(string scaleName, string characterName, DalamudPluginInterface pi)
		{
			//BodyScale newBody = new BodyScale();
			BodyScale newBody = new BodyScale();

			for (int i = 0; i < this.boneCodenames.Count && i < this.boneValuesNew.Count; i++)
			{
				string legacyName = boneCodenamesUsed[i];

				newBody.Bones[legacyName] = this.boneValuesNew[legacyName];
			}

			newBody.Bones["n_root"] = this.rootEditsContainer;

			newBody.BodyScaleEnabled = true;
			newBody.ScaleName = "IPC";
			newBody.CharacterName = newScaleCharacter;

			//newBody.RootScale = new HkVector4(this.newRootScale.X, this.newRootScale.Y, this.newRootScale.Z, 0);

			var bodyString = JsonConvert.SerializeObject(newBody);
			//PluginLog.Information($"{pi.PluginNames}");
			setBodyScale = pi.GetIpcSubscriber<string, string, object>("CustomizePlus.SetBodyScale");
			//PluginLog.Information($"{_setBodyScale}: -- {bodyString} -- {newBody.CharacterName}");
			setBodyScale.InvokeAction(bodyString, newBody.CharacterName);
		}

		private void GetFromIPC(string characterName, DalamudPluginInterface pi)
		{
			getBodyScale = pi.GetIpcSubscriber<string, string>("CustomizePlus.GetBodyScale");
			//PluginLog.Information($"{_setBodyScale}: -- {bodyString} -- {newBody.CharacterName}");
			var bodyScaleString = getBodyScale.InvokeFunc(newScaleCharacter);

			//PluginLog.Information(bodyScaleString);
			if (bodyScaleString != null)
			{
				BodyScale? bodyScale = JsonConvert.DeserializeObject<BodyScale?>(bodyScaleString);
				PluginLog.Information($"IPC request for {characterName} found scale named: {bodyScale.ScaleName}");
			}
			else
			{
				PluginLog.Information($"No scale found on IPC request for {characterName}");
			}
			
			//if (bodyScale != null)
			//	this.ScaleUpdated = bodyScale;

			
		}

		private void RevertToOriginal(string characterName, DalamudPluginInterface pi) // Use to unassign override scale in IPC testing mode
		{
			revert = pi.GetIpcSubscriber<string, object>("CustomizePlus.Revert");
			revert.InvokeAction(newScaleCharacter);
		}

		private void UpdateCurrent(string boneName, BoneEditsContainer boneValue)
		{
			BodyScale newBody = this.ScaleUpdated;

			newBody.Bones[boneName] = boneValue;
		}
	}
}
