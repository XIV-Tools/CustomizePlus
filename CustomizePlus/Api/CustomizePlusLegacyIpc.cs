// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using CustomizePlus.Data;
using CustomizePlus.Data.Configuration.Version0;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Newtonsoft.Json;

namespace CustomizePlus.Api
{
	//Temporary legacy endpoints we use to migrate to new data format while keeping compatibility with older IPC users (mare)
	public class CustomizePlusLegacyIpc : IDisposable
	{
		public const string LabelGetBodyScale = $"CustomizePlus.Legacy.{nameof(GetBodyScale)}";
		public const string LabelSetBodyScaleToCharacter = $"CustomizePlus.Legacy.{nameof(SetBodyScaleToCharacter)}";
		public const string LabelRevertCharacter = $"CustomizePlus.Legacy.{nameof(RevertCharacter)}";
		public const string LabelOnScaleUpdate = $"CustomizePlus.Legacy.{nameof(OnScaleUpdate)}";
		private readonly ObjectTable objectTable;
		private readonly DalamudPluginInterface pluginInterface;


		internal ICallGateProvider<string, string?>? ProviderGetBodyScale;
		internal ICallGateProvider<string, Character?, object>? ProviderSetBodyScaleToCharacter;
		internal ICallGateProvider<Character?, object>? ProviderRevertCharacter;
		internal ICallGateProvider<string?, object?>? ProviderOnScaleUpdate; //Sends either bodyscale string or null at startup and when scales are saved in the ui

		public CustomizePlusLegacyIpc(ObjectTable objectTable, DalamudPluginInterface pluginInterface)
		{
			this.objectTable = objectTable;
			this.pluginInterface = pluginInterface;

			InitializeProviders();
		}

		public void Dispose()
			=> DisposeProviders();

		private void DisposeProviders()
		{
			ProviderGetBodyScale?.UnregisterFunc();
			ProviderSetBodyScaleToCharacter?.UnregisterAction();
			ProviderRevertCharacter?.UnregisterAction();
		}

		private void InitializeProviders()
		{
			PluginLog.Debug("Initializing legacy c+ ipc providers.");

			try
			{
				ProviderGetBodyScale = pluginInterface.GetIpcProvider<string, string?>(LabelGetBodyScale);
				ProviderGetBodyScale.RegisterFunc(GetBodyScale);
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, $"Error registering IPC provider for {LabelGetBodyScale}.");
			}

			try
			{
				ProviderSetBodyScaleToCharacter =
					pluginInterface.GetIpcProvider<string, Character?, object>(LabelSetBodyScaleToCharacter);
				ProviderSetBodyScaleToCharacter.RegisterAction(SetBodyScaleToCharacter);
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, $"Error registering IPC provider for {LabelSetBodyScaleToCharacter}.");
			}

			try
			{
				ProviderRevertCharacter =
					pluginInterface.GetIpcProvider<Character?, object>(LabelRevertCharacter);
				ProviderRevertCharacter.RegisterAction(RevertCharacter);
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, $"Error registering IPC provider for {LabelRevertCharacter}.");
			}

			try
			{
				ProviderOnScaleUpdate = pluginInterface.GetIpcProvider<string?, object?>(LabelOnScaleUpdate);
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, $"Error registering IPC provider for {LabelOnScaleUpdate}.");
			}

		}

		public void OnScaleUpdate(BodyScale bodyScale)
		{
			PluginLog.Debug("Sending legacy c+ ipc scale message.");
			Version0BodyScale v0BodyScale = bodyScale != null ? BuildLegacyBodyScale(bodyScale) : null;
			ProviderOnScaleUpdate?.SendMessage(JsonConvert.SerializeObject(v0BodyScale));
		}

		private string? GetBodyScale(string characterName)
		{
			BodyScale? bodyScale = Plugin.GetPlayerBodyScale(characterName);
			return bodyScale != null ? JsonConvert.SerializeObject(BuildLegacyBodyScale(bodyScale)) : null;
		}

		private void SetBodyScale(string bodyScaleString, string characterName)
		{
			//Character? character = FindCharacterByName(characterName);
			Version0BodyScale? bodyScale = JsonConvert.DeserializeObject<Version0BodyScale?>(bodyScaleString);
			if (bodyScale != null)
				Plugin.SetTemporaryCharacterScale(characterName, BuildModernBodyScale(bodyScale));
		}

		private void SetBodyScaleToCharacter(string bodyScaleString, Character? character)
		{
			if (character == null)
				return;

			SetBodyScale(bodyScaleString, character.Name.ToString());
		}

		private void Revert(string characterName)
		{
			if (string.IsNullOrEmpty(characterName))
				return;

			Plugin.RemoveTemporaryCharacterScale(characterName);
		}

		private void RevertCharacter(Character? character)
		{
			if (character == null)
				return;

			Revert(character.Name.ToString());
		}

		private Version0BodyScale BuildLegacyBodyScale(BodyScale bodyScale)
		{
			Version0BodyScale v0BodyScale = new Version0BodyScale
			{
				BodyScaleEnabled = bodyScale.BodyScaleEnabled,
				CharacterName = bodyScale.CharacterName,
				ScaleName = bodyScale.ScaleName
			};

			BoneTransform rootContainer = bodyScale.Bones["n_root"];
			float w = 0;
			if (rootContainer.Scaling.X == rootContainer.Scaling.Y && rootContainer.Scaling.Y == rootContainer.Scaling.Z && rootContainer.Scaling.X == rootContainer.Scaling.Z)
				w = rootContainer.Scaling.X;
			v0BodyScale.RootScale = new CustomizePlus.Memory.HkVector4(rootContainer.Scaling.X, rootContainer.Scaling.Y, rootContainer.Scaling.Z, w);
			foreach (var kvPair in bodyScale.Bones)
			{
				if (kvPair.Key == "n_root")
					continue;

				w = 0;
				if (kvPair.Value.Scaling.X == kvPair.Value.Scaling.Y && kvPair.Value.Scaling.Y == kvPair.Value.Scaling.Z && kvPair.Value.Scaling.X == kvPair.Value.Scaling.Z)
					w = kvPair.Value.Scaling.X;

				v0BodyScale.Bones[kvPair.Key] = new CustomizePlus.Memory.HkVector4(kvPair.Value.Scaling.X, kvPair.Value.Scaling.Y, kvPair.Value.Scaling.Z, w);
			}

			return v0BodyScale;
		}

		private BodyScale BuildModernBodyScale(Version0BodyScale bodyScale)
		{
			BodyScale newBodyScale = new BodyScale
			{
				ScaleName = bodyScale.ScaleName,
				BodyScaleEnabled = bodyScale.BodyScaleEnabled,
				CharacterName = bodyScale.CharacterName
			};

			foreach (var kvPair in bodyScale.Bones)
			{
				BoneTransform boneEditsContainer = new BoneTransform
				{
					Translation = Vector3.Zero,
					EulerRotation = Vector3.Zero,
					Scaling = Vector3.One
				};

				if (kvPair.Value.W != 0)
					boneEditsContainer.Scaling = new Vector3(kvPair.Value.W, kvPair.Value.W, kvPair.Value.W);
				else if (kvPair.Value.X != 0 || kvPair.Value.Y != 0 || kvPair.Value.Z != 0)
					boneEditsContainer.Scaling = new Vector3(kvPair.Value.X, kvPair.Value.Y, kvPair.Value.Z);

				newBodyScale.Bones.Add(kvPair.Key, boneEditsContainer);
			}

			newBodyScale.Bones["n_root"] = new BoneTransform
			{
				Translation = Vector3.Zero,
				EulerRotation = Vector3.Zero
			};

			if (bodyScale.RootScale.W != 0)
				newBodyScale.Bones["n_root"].Scaling = new Vector3(bodyScale.RootScale.W, bodyScale.RootScale.W, bodyScale.RootScale.W);
			else if (bodyScale.RootScale.X != 0 || bodyScale.RootScale.Y != 0 || bodyScale.RootScale.Z != 0)
				newBodyScale.Bones["n_root"].Scaling = new Vector3(bodyScale.RootScale.X, bodyScale.RootScale.Y, bodyScale.RootScale.Z);

			return newBodyScale;
		}
	}
}
