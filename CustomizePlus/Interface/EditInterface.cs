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
	using ImGuiNET;
	using Newtonsoft.Json;
	using static CustomizePlus.BodyScale;

	public class EditInterface : WindowBase
	{
		protected BodyScale? Scale { get; private set; }

		//protected HkaPose Pose { get; private set; }
		protected override string Title => $"Edit Scale: {this.newScaleName}";

		private int scaleIndex = -1;

		private string newScaleName = string.Empty;
		private string newScaleCharacter = string.Empty;
		private string originalScaleName = string.Empty;
		private string originalScaleCharacter = string.Empty;
		private HkVector4 originalScaleValue = HkVector4.One;
		private Vector4 newScaleValue = HkVector4.One.GetAsNumericsVector();
		private Vector4 originalRootScale = new Vector4(1f,1f,1f,0f);
		private Vector4 newRootScale = HkVector4.One.GetAsNumericsVector();
		
		//protected BodyScale? ScaleStart { get; private set; }
		private BodyScale? scaleStart;
		protected BodyScale? scaleUpdated { get; private set; }
		//protected BodyScale? ScaleUpdated { get; private set; }

		//protected HkVector4 DefaultBone { get; private set; }

		//protected Dictionary<string, HkVector4>? BoneValuesOriginal { get; private set; }
		private Dictionary<string, HkVector4>? boneValuesOriginal = new Dictionary<string, HkVector4>();
		//protected Dictionary<string, HkVector4>? BoneValuesNew { get; private set; }
		private Dictionary<string, HkVector4>? boneValuesNew = new Dictionary<string, HkVector4>();
		//private List<HkVector4> boneValuesOriginal = new List<HkVector4>();
		//private List<HkVector4> boneValuesNew = new List<HkVector4>();
		//private List<Vector4> boneValuesOriginalVector = new List<Vector4>();
		//private List<Vector4> boneValuesNewVector = new List<Vector4>();
		//protected string[]? boneNamesLegacy { get; set; };
		private List<string> boneNamesLegacy = LegacyBoneNameConverter.GetLegacyNames();
		private List<string> boneNamesModern = LegacyBoneNameConverter.GetModernNames();
		private List<string> boneNamesModernUsed = new List<string>();
		private List<string> boneNamesLegacyUsed = new List<string>();
		private bool scaleEnabled = false;
		private bool autoAdjustEnabled = false;
		//private Vector4 originalRootScale;
		//private Vector4 newRootScale;

		public static void Show(BodyScale scale)
		{
			Configuration config = Plugin.Configuration;
			EditInterface editWnd = Plugin.InterfaceManager.Show<EditInterface>();
			editWnd.Scale = scale;
			editWnd.scaleUpdated = scale;
			if (scale == null)
			{
				scale = new BodyScale();
			}
			//editWnd.scaleIndex = index;

			editWnd.scaleStart = scale;
			editWnd.scaleUpdated = scale;
			editWnd.originalScaleName = scale.ScaleName;
			editWnd.originalScaleCharacter = scale.CharacterName;
			editWnd.newScaleCharacter = scale.CharacterName;

			editWnd.scaleEnabled = scale.BodyScaleEnabled;

			//editWnd.boneValuesOriginal = new HkVector4[scale.Bones.Count];
			//editWnd.boneValuesOriginal = new HkVector4[scale.Bones.Count];
			//scale.Bones.Values.CopyTo(editWnd.boneValuesOriginal, 0);
			//scale.Bones.Keys.CopyTo(editWnd.boneNamesLegacy, 0);
			//editWnd.boneNamesLegacy = new string[scale.Bones.Count];
			//editWnd.boneNamesLegacy = ;
			for (int i=0; i<editWnd.boneNamesLegacy.Count && i<editWnd.boneNamesModern.Count; i++)
			{
				HkVector4 tempBone = HkVector4.One;
				if(scale.Bones.TryGetValue(editWnd.boneNamesLegacy[i], out tempBone))
				{
					editWnd.boneValuesOriginal.Add(editWnd.boneNamesLegacy[i], tempBone);
					editWnd.boneValuesNew.Add(editWnd.boneNamesLegacy[i], tempBone);
					editWnd.boneNamesModernUsed.Add(editWnd.boneNamesModern[i]);
					editWnd.boneNamesLegacyUsed.Add(editWnd.boneNamesLegacy[i]);
				} //else if (scale.Bones.TryGetValue(editWnd.boneNamesModern[i], out tempBone))
				//{
					//editWnd.boneValuesOriginal.Add(editWnd.boneNamesModern[i], tempBone);
					//editWnd.boneValuesNew.Add(editWnd.boneNamesModern[i], tempBone);
				//}
				//else
				//{
					//tempBone = HkVector4.One;
					//editWnd.boneValuesOriginal.Add(editWnd.boneNamesLegacy[i], tempBone);
					//editWnd.boneValuesNew.Add(editWnd.boneNamesLegacy[i], tempBone);
				//}
			}
			//editWnd.boneValuesNew = editWnd.boneValuesOriginal;
			//editWnd.boneValuesOriginalVector = new Vector4[editWnd.boneValuesOriginal.Length];
			//editWnd.boneValuesNewVector = new Vector4[editWnd.boneValuesNew.Length];
			//editWnd.boneValuesNewVector = new Vector4[editWnd.boneValuesNew.Length];
			//editWnd.boneValuesOriginalVector.CopyTo(editWnd.boneValuesNewVector, 0);
			
			editWnd.originalRootScale = scale.RootScale.GetAsNumericsVector();
			//editWnd.newRootScale = new Vector4(1, 1, 1, 0);
			editWnd.newRootScale = editWnd.originalRootScale;

			editWnd.originalScaleName = scale.ScaleName;
			editWnd.originalScaleCharacter = scale.CharacterName;
			editWnd.newScaleName = editWnd.originalScaleName;
			editWnd.newScaleCharacter = editWnd.originalScaleCharacter;
			/*
			editWnd.boneNamesModern = new string[editWnd.boneNamesLegacy.Length];
			for (int i = 0; i < editWnd.boneValuesOriginal.Length; i++)
			{
				editWnd.boneValuesOriginalVector[i] = editWnd.boneValuesOriginal[i].GetAsNumericsVector();
				try
				{
					editWnd.boneNamesModern[i] = LegacyBoneNameConverter.GetModernName(editWnd.boneNamesLegacy[i]);
				} catch (Exception)
				{
					editWnd.boneNamesModern[i] = editWnd.boneNamesLegacy[i];
				}
				if (editWnd.boneNamesModern[i] == null)
				{
					editWnd.boneNamesModern[i] = editWnd.boneNamesLegacy[i];
				}
			}
			editWnd.boneValuesNewVector = new Vector4[editWnd.boneValuesNew.Length];
			editWnd.boneValuesOriginalVector.CopyTo(editWnd.boneValuesNewVector, 0);
			*/

			//editWnd.boneValuesNewVector = editWnd.boneValuesOriginalVector;
			//editWnd.DefaultBone = new HkVector4(1F, 1F, 1F, 0F);
			editWnd.scaleIndex = -1;
		}

		protected override void DrawContents()
		{
			
			Configuration config = Plugin.Configuration;
			/*
			if (config.AutomaticEditMode)
			{
				this.scaleIndex = getCurrentScaleIndex();
			}*/
			//HkVector4 originalRootScale = this.scaleStart.RootScale;
			//Vector4 newRootScale = originalRootScale.GetAsNumericsVector();
			//ImGui.Text("Coming Soon");
			//Scale = config.BodyScales[0];
			//this.Scale = config.BodyScales[0];

			string newScaleNameTemp = this.newScaleName;
			string newScaleCharacterTemp = this.newScaleCharacter;
			bool enabledTemp = this.scaleEnabled;

			//bool enable = true;

			if (ImGui.Checkbox("Enable", ref enabledTemp))
			{
				this.scaleEnabled = enabledTemp;
			}

			ImGui.SameLine();
			/*
			if (ImGui.BeginPopup("SaveAsNew"))
			{*/
				ImGui.SameLine();
			//ImGui.Text("Character Name:");
			ImGui.SetNextItemWidth(150);
			
				if(ImGui.InputText("Character Name", ref newScaleCharacterTemp, 1024))
				{
					this.newScaleCharacter = newScaleCharacterTemp;
				}

				ImGui.SameLine();

			//ImGui.Text("Scale Name:");
			ImGui.SetNextItemWidth(150);
			if (ImGui.InputText("Scale Name", ref newScaleNameTemp, 1024))
				{
					this.newScaleName = newScaleNameTemp;
				}
				/*
				if (ImGui.Button("OK"))
				{
					//BodyScale scale = new();
					this.scaleUpdated.CharacterName = newScaleCharacter;
					this.scaleUpdated.ScaleName = newScaleName;
					Plugin.Configuration.BodyScales.Add(scaleUpdated);
					Plugin.Configuration.ToggleOffAllOtherMatching(newScaleCharacter, newScaleName);
					ImGui.CloseCurrentPopup();
				}
				if (ImGui.Button("Cancel"))
				{
					ImGui.CloseCurrentPopup();
				}

				ImGui.EndPopup();
			}

			ImGui.SameLine();
			ImGui.SetNextItemWidth(ImGui.GetWindowSize().X - 123);
			ImGui.LabelText(string.Empty, string.Empty);

			if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
			{
				ImGui.OpenPopup("SaveAsNew");
			}

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip("Save as new");
				*/
			//ImGui.Text("Character: " + originalScaleCharacter + "  Name: " + originalScaleName);

			//this.originalRootScale = this.scaleUpdated.RootScale;
			//newRootScale = originalRootScale.GetAsNumericsVector();
			//if (rootScale.X )
			//{
			//rootScale = HkVector4.One;
			//}
			//Vector4 rootScaleNew = rootScale.GetAsNumericsVector();
			//this.newScaleValue = rootScale.GetAsNumericsVector();
			//float rootX = newRootScale.X;
			//float rootY = newRootScale.Y;
			//float rootZ = newRootScale.Z;

			ImGui.Separator();
			/*
			ImGui.SetNextItemWidth(150f);
			ImGui.Text("Root:");
			float valueWidth = (ImGui.GetWindowSize().X - 150f) / 3;

			ImGui.SameLine();
			ImGui.SetNextItemWidth(valueWidth);

			if (ImGui.InputFloat("X", ref rootX, 0.1f, 0.5f))
			{
				newRootScale.X = rootX;
				//rootScaleNew.X = rootX;
			}

			ImGui.SameLine();
			//ImGui.TableNextColumn();

			ImGui.SetNextItemWidth(valueWidth);
			if (ImGui.InputFloat("Y", ref rootY, 0.1f, 0.5f))
			{
				newRootScale.Y = rootY;
				//rootScaleNew.Y = rootY;
			}

			ImGui.SameLine();
			//ImGui.TableNextColumn();

			ImGui.SetNextItemWidth(valueWidth);
			if (ImGui.InputFloat("Z", ref rootZ, 0.1f, 0.5f))
			{
				newRootScale.Z = rootZ;
				//rootScaleNew.Z = rootZ;
			}
			*/

			//ImGui.Separator();

			//List<string> boneNamesModern = LegacyBoneNameConverter.GetLegacyNames();
			//List<string> boneNamesLegacy = new List<string>();
			//List<string> boneNamesModern = new List<string>();// LegacyBoneNameConverter.GetModernNames();
															  //Dictionary<string, HkVector4> boneValues = this.Scale.Bones;
															  //Dictionary<string, Vector4> boneValuesNew = new Dictionary<string, Vector4>();

			//Dictionary<string, Vector4> boneValuesOriginal = new Dictionary<string, Vector4>();
			//List<Vector4> boneValuesOriginal = new List<Vector4>();
			//List<Vector4> boneValuesNew = new List<Vector4>();
			//Dictionary<string, Vector4> boneValuesNew = this.Scale.Bones;
			//Dictionary<string, HkVector4>.Enumerator bonesLoop = this.scaleUpdated.Bones.GetEnumerator();
			/*
			while (BonesLoop.MoveNext())
			{
				BonesLoop.Current.Key()
				string modernBoneNameLocal = LegacyBoneNameConverter.GetModernName(boneNameLegacy)
				string legacyBoneNameLocal = LegacyBoneNameConverter.GetLegacyName(boneNamesLegacy);
			}
			*/
			//boneValuesNew = this.ScaleUpdated.Bones;
			/*
			while (bonesLoop.MoveNext())
			{
				string boneName = bonesLoop.Current.Key.ToString();
				string? boneNameModern = null;
				try
				{
					boneNameModern = LegacyBoneNameConverter.GetModernName(boneName);
				}
				catch
				{
					boneNameModern = boneName;
				}

				if (boneNameModern == null)
				{
					boneNameModern = "Not Found";
				}

				boneNamesModern.Add(boneNameModern);
				boneNamesLegacy.Add(boneName);
				boneValuesOriginal.Add(bonesLoop.Current.Value.GetAsNumericsVector());
				boneValuesNew.Add(bonesLoop.Current.Value.GetAsNumericsVector());
			}
			*/
			//bonesLoop.Dispose();

			//BonesLoop = boneValues.GetEnumerator();*/
			//int i = 0;

			//Dictionary<string, HkVector4> BonesLoop = Scale.Bones;
			//ImGui.Ne

			
			
			Vector4 rootScaleLocal = this.newRootScale;
			Vector3 rootScaleLocalProper = new Vector3((float)rootScaleLocal.X, (float)rootScaleLocal.Y, (float)rootScaleLocal.Z);
			if(ImGui.DragFloat3("Root", ref rootScaleLocalProper, 0.1f, 0f, 10f))
			{
				rootScaleLocal = new Vector4(rootScaleLocalProper.X, rootScaleLocalProper.Y, rootScaleLocalProper.Z, 0f);
				this.newRootScale = rootScaleLocal;
				if (config.AutomaticEditMode)
				{
					this.UpdateCurrent("Root", new HkVector4(rootScaleLocal.X, rootScaleLocal.Y, rootScaleLocal.Z, 0f));
				}
			}

			ImGui.Separator();

			ImGui.Text("Bones:");
			

			ImGui.BeginChild("scrolling", new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 56), false);
			/*
			ImGui.BeginTable("Bones", 5);

			ImGui.TableNextRow(ImGuiTableRowFlags.Headers, 15);
			ImGui.TableNextColumn();
			ImGui.Text("Bone Name:");
			ImGui.TableNextColumn();
			ImGui.Text("X Value:");
			ImGui.TableNextColumn();
			ImGui.Text("Y Value:");
			ImGui.TableNextColumn();
			ImGui.Text("Z Value:");
			ImGui.TableNextColumn();
			ImGui.Text("W Value:");
			*/
			
			for (int i = 0; i < this.boneValuesNew.Count; i++)
			{
				//Dictionary<string, HkVector4> boneValues = this.Scale.Bones;
				//KeyValuePair<string, HkVector4> currentPair = boneValues.TryGetValue


				string boneNameLocalLegacy = this.boneNamesLegacyUsed[i];
				//scale.Bones.ContainsKey

				string boneNameLocalModern = this.boneNamesModernUsed[i];
				
				ImGui.PushID(i);
				HkVector4 currentHkVector = HkVector4.One;
				String label = "Not Found";
				try
				{
					if(this.boneValuesNew.TryGetValue(boneNameLocalLegacy, out currentHkVector))
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
				//TODO: Make W a 'change the other 3' option
				if (currentVector4.X == currentVector4.Y && currentVector4.Y == currentVector4.Z)
				{
					//currentVector4.X = currentVector4.W;
					//currentVector4.Y = currentVector4.W;
					//currentVector4.Z = currentVector4.W;
					currentVector4.W = currentVector4.X;
				} else
				{
					currentVector4.W = 0;
				}	

				//if (currentVector4.X == currentVector4.Y && currentVector4.Y == currentVector4.Z)
				//{
				//	currentVector4.W = currentVector4.X;
				//}

				//if (boneNameLocalModern == null)
				//	boneNameLocalModern = boneNameLocalLegacy;
				//string boneNameLocalLegacy = boneNamesLegacy[i];
				//string boneNamesModern[i]
				//Vector4 originalScaleValue = this.boneValuesOriginalVector[i];

				//String boneNameModern = 
				//string label = boneNameLocalModern;

				ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 150);
				//Vector4 newScaleValue = this;
				if (ImGui.DragFloat4(label, ref currentVector4, 0.001f, 0f, 10f))
				{
					try
					{
						if (currentVector4.X != currentVector4.Y || currentVector4.Y != currentVector4.Z || currentVector4.X != currentVector4.Z)
							currentVector4.W = 0;
						if (currentVector4.W != 0)
						{
							currentVector4.X = currentVector4.W;
							currentVector4.Y = currentVector4.W;
							currentVector4.Z = currentVector4.W;
							//currentVector4.W = 0;
						}
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
						//this.boneValuesNew.Add(label, new HkVector4(currentVector4.X, currentVector4.Y, currentVector4.Z, currentVector4.W));
						//boneValuesNew.Add(boneNameLocalModern, newScaleValue);
					}
					catch
					{
						//throw new Exception();
					}
					if (config.AutomaticEditMode)
					{
						this.UpdateCurrent(boneNameLocalLegacy, new HkVector4(currentVector4.X, currentVector4.Y, currentVector4.Z, currentVector4.W));
					}

				}

				//HkVector4 originalScaleValue = HkVector4.One;
				//HkVector4 newScaleValue = HkVector4.One;
				/*try
				{
					if(this.Scale.Bones.TryGetValue(boneNameLocalLegacy, out originalScaleValue))
					{
						newScaleValue = originalScaleValue;
						//this.newBoneValues[boneNameLocalLegacy] = this.originalScaleValue;
					} else
					{
						newScaleValue = HkVector4.One;
						//this.newBoneValues[]
					}
				}
				catch
				{
					originalScaleValue = HkVector4.One;
					newScaleValue = originalScaleValue;
				}
						*/
				//HkVector4 currentHkVector = HkVector4.One;
				//this.newScaleValue = this.originalScaleValue.GetAsNumericsVector();

				//string legacyBoneNameLocal = LegacyBoneNameConverter.GetLegacyName(boneNameLocal);
				//float boneX = newScaleValue.X;
				//float boneY = newScaleValue.Y;
				//float boneZ = newScaleValue.Z;
				//float boneW = newScaleValue.W;

				//string boneName = current.Key;
				//string boneNameModern = boneName;
				/*
				try
				{
					boneNameModern = LegacyBoneNameConverter.GetLegacyName(boneName);
				} catch (Exception ex)
				{
					boneNameModern = boneName;
				}
				*/
				//Vector4 boneValueNew = currentVector;
				///boneValuesNew.TryAdd(boneNameModern, boneValueNew);
				//Vector4 boneValuesOriginal = current.Value.GetAsNumericsVector();
				//HkVector4 boneValueNewHk4 = currentHkVector;

				/*
				if (ImGui.InputFloat4(boneNameModern, ref boneValueNew))
				{
					boneValuesNew[boneNameModern] = boneValueNew;
				}*/
				//ImGui.TableNextRow(ImGuiTableRowFlags.None, 16f);
				//ImGui.TableNextColumn();

				//ImGui.Text(boneNameLocalModern);
				//ImGui.Text(boneNameModern + " -- " + boneValueNew.ToString());

				//float valueWidth = (ImGui.GetWindowSize().X - 150) / 4;

				//ImGui.SetNextItemWidth(150);

				//ImGui.TableNextColumn();

				//ImGui.Text(boneNames[i]);

				//ImGui.SameLine();

				//ImGui.SetNextItemWidth(valueWidth);

				//ImGui.TableNextColumn();
				//Vector4 newBoneValue = boneValuesNew[boneNames[i]];

				/*
				if (ImGui.InputFloat("X", ref newScaleValue.X, 0.1f, 0.5f))
				{
					newScaleValue.X = boneX;
					//boneValueNew.X = boneX;
					//boneValuesNew.Remove(boneNameLocalLegacy);
					//boneValuesNew.TryAdd(boneNameLocalLegacy, boneValueNew);
				}

				//ImGui.SameLine();
				ImGui.TableNextColumn();

				//ImGui.SetNextItemWidth(valueWidth);
				if (ImGui.InputFloat("Y", ref newScaleValue.Y, 0.1f, 0.5f))
				{
					newScaleValue.Y = boneY;
					//boneValueNew.Y = boneY;
				}

				//ImGui.SameLine();
				ImGui.TableNextColumn();

				//ImGui.SetNextItemWidth(valueWidth);
				if (ImGui.InputFloat("Z", ref newScaleValue.Z, 0.1f, 0.5f))
				{
					newScaleValue.Z = boneZ;
					//boneValueNew.Z = boneZ;
				}

				//ImGui.SameLine();
				ImGui.TableNextColumn();

				//ImGui.SetNextItemWidth(valueWidth);
				if (ImGui.InputFloat("W", ref newScaleValue.W, 0.1f, 0.5f))
				{
					newScaleValue.W = boneW;
					//boneValueNew.W = boneW;
				}

				//ImGui.TableNextRow(ImGuiTableRowFlags.None, 15f);
				*/
				ImGui.PopID();
			}
			//ImGui.EndTable();
			ImGui.EndChild();
			
			ImGui.Separator();

			if (ImGui.Button("Save"))
			{
				AddToConfig(this.newScaleName, this.newScaleCharacter);
				config.Save();
				Plugin.LoadConfig();
			}
			
			/*
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
		}
		
		private void AddToConfig(string scaleName, string characterName)
		{
			Configuration config = Plugin.Configuration;
			BodyScale newBody = new BodyScale();
			//foreach ((string boneName, HkVector4 boneScale) in this.boneValuesNew)
			//{
				//if (bone == null)
				//	continue;

				//if (bone.Scale == null)
				//	continue;

				//string? legacyName = LegacyBoneNameConverter.GetLegacyName(boneName);
				//if (legacyName == null)
					//legacyName = boneName;
				/*
				HkVector4 boneScale = new();
				boneScale.X = bone.Scale.X;
				boneScale.Y = bone.Scale.Y;
				boneScale.Z = bone.Scale.Z;
				*/
			for (int i = 0; i < this.boneNamesLegacy.Count && i < this.boneValuesNew.Count; i++)
			{		
				string legacyName = boneNamesLegacyUsed[i];
				//HkVector4 BoneScale = 

				if (!this.scaleUpdated.Bones.ContainsKey(legacyName))
					newBody.Bones.Add(legacyName, this.boneValuesNew[legacyName]);

				newBody.Bones[legacyName] = this.boneValuesNew[legacyName];

				newBody.BodyScaleEnabled = this.scaleEnabled;
				newBody.ScaleName = scaleName;
				newBody.CharacterName = characterName;
			//}
			}
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

		private void RevertToOriginal()
		{
			this.boneValuesNew = this.boneValuesOriginal;
			this.newRootScale = this.originalRootScale;
		}

		private void UpdateCurrent(string boneName, HkVector4 boneValue)
		{
			Configuration config = Plugin.Configuration;
			BodyScale newBody = this.scaleUpdated;
			//HkVector4 BoneScale = 
			//if (!this.scaleUpdated.Bones.ContainsKey(legacyName))
			//	newBody.Bones.Add(legacyName, this.boneValuesNew[legacyName]);
			if (boneName == "Root")
			{
				newBody.RootScale = boneValue;
			}
			else
			{
				newBody.Bones[boneName] = boneValue;
			}
			
			//newBody.Bones[legacyName] = this.boneValuesNew[legacyName];

			//newBody.BodyScaleEnabled = this.scaleEnabled;
			//newBody.ScaleName = this.originalScaleName;
			//newBody.CharacterName = this.originalScaleCharacter;
				//}
			//}
			//newBody.RootScale = new HkVector4(this.newRootScale.X, this.newRootScale.Y, this.newRootScale.Z, 0);
			if (this.scaleIndex == -1 || this.scaleIndex > config.BodyScales.Count)
			{
				this.scaleIndex = getCurrentScaleIndex(this.originalScaleName, this.originalScaleCharacter);
			}

			//config.BodyScales.RemoveAt(matchIndex);
			//	config.BodyScales.Insert(matchIndex, newBody);
			config.BodyScales[this.scaleIndex] = newBody;
			config.Save();
			Plugin.LoadConfig();
		}
		
		private int getCurrentScaleIndex(string scaleName, string scaleCharacter)
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
				//config.BodyScales.RemoveAt(matchIndex);
				//config.BodyScales.Insert(matchIndex, newBody);
			}
			return -1;
			//scaleUpdated
		}
	}
}
