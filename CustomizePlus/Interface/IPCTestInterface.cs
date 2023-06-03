// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
	using CustomizePlus.Data;
	using CustomizePlus.Data.Profile;
	using Dalamud.Interface;
	using Dalamud.Interface.Components;
	using Dalamud.Logging;
	using Dalamud.Plugin;
	using Dalamud.Plugin.Ipc;
	using ImGuiNET;
	using Newtonsoft.Json;
	using System;
	using System.Collections.Generic;
	using System.Numerics;

	public class IPCTestInterface : WindowBase
	{
		//private DalamudPluginInterface localPlugin;
		private bool subscribed = true;

		private ICallGateSubscriber<string, string>? getCharacterProfile;
		//private readonly ICallGateSubscriber<Character?, string?>? ProviderGetCharacterProfileFromCharacter;
		private ICallGateSubscriber<string, string, object>? setCharacterProfile;
		//private readonly ICallGateSubscriber<string, Character?, object>? ProviderSetCharacterProfileToCharacter;
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

		protected CharacterProfile? Scale { get; private set; }

		protected override string Title => $"(WIP) IPC Test: {this.newScaleCharacter}";
		protected CharacterProfile? ScaleUpdated { get; private set; }

		private string newScaleName = string.Empty;
		private string newScaleCharacter = string.Empty;
		private string originalScaleName = string.Empty;
		private string originalScaleCharacter = string.Empty;
		private BoneTransform rootEditsContainer = new BoneTransform();

		private Dictionary<string, BoneTransform> boneValuesOriginal = new Dictionary<string, BoneTransform>();
		private Dictionary<string, BoneTransform> boneValuesNew = new Dictionary<string, BoneTransform>();
		private readonly List<string> boneCodenames = BoneData.GetBoneCodenames();
		private readonly List<string> boneDispNames = BoneData.GetBoneDisplayNames();
		private List<string> boneDispNamesUsed = new List<string>();
		private List<string> boneCodenamesUsed = new List<string>();
		private bool scaleEnabled = false;
		private bool reset = false;

		private bool automaticBoneAttribute = false;

		private BoneAttribute BoneAttribute;

		public static void Show(DalamudPluginInterface pi)
		{
			DalamudPluginInterface localPlugin = pi;
			IPCTestInterface editWnd = Plugin.InterfaceManager.Show<IPCTestInterface>();
			editWnd.getCharacterProfile = localPlugin.GetIpcSubscriber<string, string>("CustomizePlus.GetCharacterProfile");
			//localPlugin.GetIpcSubscriber<Character?, string?> ProviderGetCharacterProfileFromCharacter;
			editWnd.setCharacterProfile = localPlugin.GetIpcSubscriber<string, string, object>("CustomizePlus.SetCharacterProfile");
			//localPlugin.GetIpcSubscriber<string, Character?, object> ProviderSetCharacterProfileToCharacter;
			editWnd.revert = localPlugin.GetIpcSubscriber<string, object>("CustomizePlus.Revert");
			//localPlugin.GetIpcSubscriber<Character?, object>? ProviderRevertCharacter;
			//_getApiVersion = localPlugin.GetIpcSubscriber<string>("CustomizePlus.GetApiVersion");
			//_onScaleUpdate = localPlugin.GetIpcSubscriber<string?, object?>("CustomizePlus.OnScaleUpdate"); ;
			//UnsubscribeEvents();


			var prof = new CharacterProfile();
			editWnd.Scale = prof;
			editWnd.ScaleUpdated = prof;
			if (prof == null)
			{

			}

			editWnd.ScaleUpdated = prof;
			editWnd.originalScaleName = prof.ProfName;
			editWnd.originalScaleCharacter = prof.CharName;
			editWnd.newScaleCharacter = prof.CharName;

			editWnd.scaleEnabled = prof.Enabled;

			for (int i = 0; i < editWnd.boneCodenames.Count && i < editWnd.boneDispNames.Count; i++)
			{
				BoneTransform tempContainer = new BoneTransform { Scaling = Vector3.One };
				if (prof.Bones.TryGetValue(editWnd.boneCodenames[i], out tempContainer))
				{
					editWnd.boneValuesOriginal.Add(editWnd.boneCodenames[i], tempContainer);
					editWnd.boneValuesNew.Add(editWnd.boneCodenames[i], tempContainer);
					editWnd.boneDispNamesUsed.Add(editWnd.boneDispNames[i]);
					editWnd.boneCodenamesUsed.Add(editWnd.boneCodenames[i]);
				}
			}

			editWnd.originalScaleName = prof.ProfName;
			editWnd.originalScaleCharacter = prof.CharName;
			editWnd.newScaleName = editWnd.originalScaleName;
			editWnd.newScaleCharacter = editWnd.originalScaleCharacter;


			//DrawContents();
		}

		protected override void DrawContents()
		{
			try
			{
				SubscribeEvents();
				DrawScaleEdit(new CharacterProfile(), DalamudServices.PluginInterface);
			}
			catch (Exception e)
			{
				PluginLog.LogError($"Error during IPC Tests:\n{e}");
			}
		}

		public void DrawScaleEdit(CharacterProfile scale, DalamudPluginInterface pi)
		{
			string newScaleNameTemp = this.newScaleName;
			string newScaleCharacterTemp = this.newScaleCharacter;
			bool enabledTemp = this.scaleEnabled;
			bool resetTemp = this.reset;

			if (ImGui.Checkbox("Enable", ref enabledTemp))
			{
				this.scaleEnabled = enabledTemp;
				if (automaticBoneAttribute)
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

			bool autoModeEnable = automaticBoneAttribute;
			if (ImGui.Checkbox("Automatic Mode", ref autoModeEnable))
			{
				automaticBoneAttribute = autoModeEnable;
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Applies changes automatically without saving.");

			if (ImGui.RadioButton("Position", BoneAttribute == BoneAttribute.Position))
				BoneAttribute = BoneAttribute.Position;

			ImGui.SameLine();
			if (ImGui.RadioButton("Rotation", BoneAttribute == BoneAttribute.Rotation))
				BoneAttribute = BoneAttribute.Rotation;

			ImGui.SameLine();
			if (ImGui.RadioButton("Scale", BoneAttribute == BoneAttribute.Scale))
				BoneAttribute = BoneAttribute.Scale;

			ImGui.Separator();

			if (ImGuiComponents.IconButton(-1, FontAwesomeIcon.Recycle))
			{
				this.rootEditsContainer = new BoneTransform();
				if (automaticBoneAttribute)
				{
					this.UpdateCurrent("n_root", this.rootEditsContainer);
				}
				this.reset = true;
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip($"Reset");

			ImGui.SameLine();

			Vector3 rootLocalTemp = Vector3.One;
			bool isRootControlDisabled = false;
			switch (BoneAttribute)
			{
				case BoneAttribute.Position:
					rootLocalTemp = rootEditsContainer.Translation;
					break;
				case BoneAttribute.Rotation:
					rootLocalTemp = Vector3.Zero;
					isRootControlDisabled = true;
					break;
				case BoneAttribute.Scale:
					rootLocalTemp = rootEditsContainer.Scaling;
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

				switch (BoneAttribute)
				{
					case BoneAttribute.Position:
						rootEditsContainer.Translation = new Vector3(rootLocalTemp.X, rootLocalTemp.Y, rootLocalTemp.Z);
						break;
					case BoneAttribute.Rotation:
						rootEditsContainer.Rotation = new Vector3(rootLocalTemp.X, rootLocalTemp.Y, rootLocalTemp.Z);
						break;
					case BoneAttribute.Scale:
						rootEditsContainer.Scaling = new Vector3(rootLocalTemp.X, rootLocalTemp.Y, rootLocalTemp.Z);
						break;
				}

				if (automaticBoneAttribute)
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

			switch (BoneAttribute)
			{
				case BoneAttribute.Position:
					col4Label = "Unused";
					break;
				case BoneAttribute.Rotation:
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

				BoneTransform currentEditsContainer = new BoneTransform { Translation = Vector3.Zero, Rotation = Vector3.Zero, Scaling = Vector3.One };
				string label = "Not Found";

				try
				{
					if (this.boneValuesNew.TryGetValue(codenameLocal, out currentEditsContainer))
						label = dispNameLocal;
					else if (this.boneValuesNew.TryGetValue(dispNameLocal, out currentEditsContainer))
						label = dispNameLocal;
					else
						currentEditsContainer = new BoneTransform { Translation = Vector3.Zero, Rotation = Vector3.Zero, Scaling = Vector3.One };
				}
				catch (Exception ex)
				{

				}

				Vector3 currentVector = Vector3.One;
				switch (BoneAttribute)
				{
					case BoneAttribute.Position:
						currentVector = currentEditsContainer.Translation;
						break;
					case BoneAttribute.Rotation:
						currentVector = currentEditsContainer.Rotation;
						break;
					case BoneAttribute.Scale:
						currentVector = currentEditsContainer.Scaling;
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
					BoneTransform editsContainer = null;

					switch (BoneAttribute)
					{
						case BoneAttribute.Position:
						case BoneAttribute.Rotation:
							//currentVector.W = 0F;
							currentVector.X = 0F;
							currentVector.Y = 0F;
							currentVector.Z = 0F;
							break;
						case BoneAttribute.Scale:
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
						else if (this.boneValuesNew.Remove(codenameLocal, out BoneTransform removedContainer))
						{
							editsContainer = removedContainer;
							this.boneValuesNew[codenameLocal] = editsContainer;
						}
						else
							throw new Exception();

						switch (BoneAttribute)
						{
							case BoneAttribute.Position:
								editsContainer.Translation = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
							case BoneAttribute.Rotation:
								editsContainer.Rotation = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
							case BoneAttribute.Scale:
								editsContainer.Scaling = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
						}
					}
					catch
					{
						//throw new Exception();
					}
					if (automaticBoneAttribute)
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

				switch (BoneAttribute)
				{
					case BoneAttribute.Rotation:
						minLimit = -360f;
						maxLimit = 360f;
						increment = 1f;
						break;
				}

				if (ImGui.DragFloat3(label, ref currentVector, increment, minLimit, maxLimit))
				{
					BoneTransform editsContainer = null;
					try
					{
						if (this.reset)
						{
							switch (BoneAttribute)
							{
								case BoneAttribute.Position:
								case BoneAttribute.Rotation:
									//currentVector.W = 0F;
									currentVector.X = 0F;
									currentVector.Y = 0F;
									currentVector.Z = 0F;
									break;
								case BoneAttribute.Scale:
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
						else if (this.boneValuesNew.Remove(codenameLocal, out BoneTransform removedContainer))
						{
							editsContainer = removedContainer;
							this.boneValuesNew[codenameLocal] = editsContainer;
						}
						else
							throw new Exception();

						switch (BoneAttribute)
						{
							case BoneAttribute.Position:
								editsContainer.Translation = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
							case BoneAttribute.Rotation:
								editsContainer.Rotation = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
							case BoneAttribute.Scale:
								editsContainer.Scaling = new Vector3(currentVector.X, currentVector.Y, currentVector.Z);
								break;
						}
					}
					catch
					{
						//throw new Exception();
					}
					if (automaticBoneAttribute)
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
			//CharacterProfile newBody = new CharacterProfile();
			CharacterProfile newBody = new CharacterProfile();

			for (int i = 0; i < this.boneCodenames.Count && i < this.boneValuesNew.Count; i++)
			{
				string legacyName = boneCodenamesUsed[i];

				newBody.Bones[legacyName] = this.boneValuesNew[legacyName];
			}

			newBody.Bones["n_root"] = this.rootEditsContainer;

			newBody.Enabled = true;
			newBody.ProfName = "IPC";
			newBody.CharName = newScaleCharacter;

			//newBody.RootScale = new HkVector4(this.newRootScale.X, this.newRootScale.Y, this.newRootScale.Z, 0);

			var bodyString = JsonConvert.SerializeObject(newBody);
			//PluginLog.Information($"{pi.PluginNames}");
			setCharacterProfile = pi.GetIpcSubscriber<string, string, object>("CustomizePlus.SetCharacterProfile");
			//PluginLog.Information($"{_setCharacterProfile}: -- {bodyString} -- {newBody.CharacterName}");
			setCharacterProfile.InvokeAction(bodyString, newBody.CharName);
		}

		private void GetFromIPC(string characterName, DalamudPluginInterface pi)
		{
			getCharacterProfile = pi.GetIpcSubscriber<string, string>("CustomizePlus.GetCharacterProfile");
			//PluginLog.Information($"{_setCharacterProfile}: -- {bodyString} -- {newBody.CharacterName}");
			var CharacterProfileString = getCharacterProfile.InvokeFunc(newScaleCharacter);

			//PluginLog.Information(CharacterProfileString);
			if (CharacterProfileString != null)
			{
				CharacterProfile? CharacterProfile = JsonConvert.DeserializeObject<CharacterProfile?>(CharacterProfileString);
				PluginLog.Information($"IPC request for {characterName} found scale named: {CharacterProfile.ProfName}");
			}
			else
			{
				PluginLog.Information($"No scale found on IPC request for {characterName}");
			}

			//if (CharacterProfile != null)
			//	this.ScaleUpdated = CharacterProfile;


		}

		private void RevertToOriginal(string characterName, DalamudPluginInterface pi) // Use to unassign override scale in IPC testing mode
		{
			revert = pi.GetIpcSubscriber<string, object>("CustomizePlus.Revert");
			revert.InvokeAction(newScaleCharacter);
		}

		private void UpdateCurrent(string boneName, BoneTransform boneValue)
		{
			CharacterProfile newBody = this.ScaleUpdated;

			newBody.Bones[boneName] = boneValue;
		}
	}
}
