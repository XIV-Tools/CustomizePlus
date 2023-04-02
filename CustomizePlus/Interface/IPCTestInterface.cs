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
	/*
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
		private HkVector4 originalScaleValue = HkVector4.One;
		private Vector4 newScaleValue = HkVector4.One.GetAsNumericsVector();
		private Vector4 originalRootScale = new Vector4(1f, 1f, 1f, 0f);
		private Vector4 newRootScale = HkVector4.One.GetAsNumericsVector();

		private BodyScale? scaleStart;
		private Dictionary<string, BoneEditsContainer>? boneValuesOriginal = new Dictionary<string, BoneEditsContainer>();
		private Dictionary<string, BoneEditsContainer>? boneValuesNew = new Dictionary<string, BoneEditsContainer>();
		private readonly List<string> boneNamesLegacy = LegacyBoneNameConverter.GetLegacyNames();
		private readonly List<string> boneNamesModern = LegacyBoneNameConverter.GetModernNames();
		private List<string> boneNamesModernUsed = new List<string>();
		private List<string> boneNamesLegacyUsed = new List<string>();
		private bool scaleEnabled = false;
		private bool reset = false;

		private bool automaticEditMode = false;

		public void Show(DalamudPluginInterface pi)
		{
			DalamudPluginInterface localPlugin = pi;
			getBodyScale = localPlugin.GetIpcSubscriber<string, string>("CustomizePlus.GetBodyScale");
			//localPlugin.GetIpcSubscriber<Character?, string?> ProviderGetBodyScaleFromCharacter;
			setBodyScale = localPlugin.GetIpcSubscriber<string, string, object>("CustomizePlus.SetBodyScale");
			//localPlugin.GetIpcSubscriber<string, Character?, object> ProviderSetBodyScaleToCharacter;
			revert = localPlugin.GetIpcSubscriber<string, object>("CustomizePlus.Revert");
			//localPlugin.GetIpcSubscriber<Character?, object>? ProviderRevertCharacter;
			//_getApiVersion = localPlugin.GetIpcSubscriber<string>("CustomizePlus.GetApiVersion");
			//_onScaleUpdate = localPlugin.GetIpcSubscriber<string?, object?>("CustomizePlus.OnScaleUpdate"); ;
			//UnsubscribeEvents();
			IPCTestInterface editWnd = Plugin.InterfaceManager.Show<IPCTestInterface>();

			var scale = ConfigurationInterface.BuildDefault(new BodyScale());
			editWnd.Scale = scale;
			editWnd.ScaleUpdated = scale;
			if (scale == null)
			{
				
			}

			editWnd.scaleStart = scale;
			editWnd.ScaleUpdated = scale;
			editWnd.originalScaleName = scale.ScaleName;
			editWnd.originalScaleCharacter = scale.CharacterName;
			editWnd.newScaleCharacter = scale.CharacterName;

			editWnd.scaleEnabled = scale.BodyScaleEnabled;

			for (int i = 0; i < editWnd.boneNamesLegacy.Count && i < editWnd.boneNamesModern.Count; i++)
			{
				//HkVector4 tempBone = HkVector4.One;
				BoneEditsContainer tempContainer = new BoneEditsContainer { Scale = HkVector4.One };
				if (scale.Bones.TryGetValue(editWnd.boneNamesLegacy[i], out tempContainer))
				{
					editWnd.boneValuesOriginal.Add(editWnd.boneNamesLegacy[i], tempContainer);
					editWnd.boneValuesNew.Add(editWnd.boneNamesLegacy[i], tempContainer);
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

			
			//DrawContents();
		}

		protected override void DrawContents()
		{
			try
			{
				SubscribeEvents();
				DrawScaleEdit(new BodyScale(), Plugin.PluginInterface);
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

			ImGui.Separator();

			Vector4 rootScaleLocal = this.newRootScale;

			if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Recycle))
			{
				rootScaleLocal = new Vector4(1f, 1f, 1f, 1f);
				this.newRootScale = rootScaleLocal;
				if (automaticEditMode)
				{
					this.UpdateCurrent("Root", new HkVector4(1f, 1f, 1f, 1f));
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
				if (automaticEditMode)
				{
					this.UpdateCurrent("Root", new HkVector4(rootScaleLocal.X, rootScaleLocal.Y, rootScaleLocal.Z, rootScaleLocalTemp.W));
				}
			}

			ImGui.Separator();
			ImGui.BeginTable("Bones", 6, ImGuiTableFlags.SizingStretchSame);
			ImGui.TableNextColumn();
			ImGui.Text("Bones:");
			ImGui.TableNextColumn();
			ImGui.Text("X");
			ImGui.TableNextColumn();
			ImGui.Text("Y");
			ImGui.TableNextColumn();
			ImGui.Text("Z");
			ImGui.TableNextColumn();
			ImGui.Text("All");
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

				HkVector4 currentHkVector = HkVector4.One;
				string label = "Not Found";

				try
				{
					if (this.boneValuesNew.TryGetValue(boneNameLocalLegacy, out currentHkVector))
					{
						label = boneNameLocalModern;
					}
					else if (this.boneValuesNew.TryGetValue(boneNameLocalModern, out currentHkVector))
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
						if (this.boneValuesNew.ContainsKey(boneNameLocalModern))
						{
							this.boneValuesNew[boneNameLocalModern] = new HkVector4(currentVector4.X, currentVector4.Y, currentVector4.Z, currentVector4.W);
						}
						else if (this.boneValuesNew.Remove(boneNameLocalLegacy))
						{
							this.boneValuesNew[boneNameLocalLegacy] = new HkVector4(currentVector4.X, currentVector4.Y, currentVector4.Z, currentVector4.W);
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
					if (automaticEditMode)
					{
						this.UpdateCurrent(boneNameLocalLegacy, new HkVector4(currentVector4.X, currentVector4.Y, currentVector4.Z, currentVector4.W));
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

				ImGui.SameLine();

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
						if (this.boneValuesNew.ContainsKey(boneNameLocalModern))
						{
							this.boneValuesNew[boneNameLocalModern] = new HkVector4(currentVector4.X, currentVector4.Y, currentVector4.Z, currentVector4.W);
						}
						else if (this.boneValuesNew.Remove(boneNameLocalLegacy))
						{
							this.boneValuesNew[boneNameLocalLegacy] = new HkVector4(currentVector4.X, currentVector4.Y, currentVector4.Z, currentVector4.W);
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
					if (automaticEditMode)
					{
						this.UpdateCurrent(boneNameLocalLegacy, new HkVector4(currentVector4.X, currentVector4.Y, currentVector4.Z, currentVector4.W));
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

			for (int i = 0; i < this.boneNamesLegacy.Count && i < this.boneValuesNew.Count; i++)
			{
				string legacyName = boneNamesLegacyUsed[i];

				if (!this.ScaleUpdated.Bones.ContainsKey(legacyName))
					newBody.Bones.Add(legacyName, this.boneValuesNew[legacyName]);

				newBody.Bones[legacyName] = this.boneValuesNew[legacyName];

				newBody.BodyScaleEnabled = true;
				newBody.ScaleName = "IPC";
				newBody.CharacterName = newScaleCharacter;
			}

			newBody.RootScale = new HkVector4(this.newRootScale.X, this.newRootScale.Y, this.newRootScale.Z, 0);

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

		private void UpdateCurrent(string boneName, HkVector4 boneValue)
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
	}*/
}
