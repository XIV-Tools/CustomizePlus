// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Newtonsoft.Json;

namespace CustomizePlus.Api
{
	public class CustomizePlusIpc : IDisposable
	{
		public static readonly string ApiVersion = "2.0";
		public const string LabelProviderApiVersion				= $"CustomizePlus.{nameof(GetApiVersion)}";
		public const string LabelGetBodyScale					= $"CustomizePlus.{nameof(GetBodyScale)}";
		public const string LabelGetBodyScaleFromCharacter		= $"CustomizePlus.{nameof(GetBodyScaleFromCharacter)}";
		public const string LabelSetBodyScale					= $"CustomizePlus.{nameof(SetBodyScale)}";
		public const string LabelSetBodyScaleToCharacter		= $"CustomizePlus.{nameof(SetBodyScaleToCharacter)}";
		public const string LabelRevert							= $"CustomizePlus.{nameof(Revert)}";
		public const string LabelRevertCharacter				= $"CustomizePlus.{nameof(RevertCharacter)}";
		public const string LabelOnScaleUpdate					= $"CustomizePlus.{nameof(OnScaleUpdate)}";
		private readonly ObjectTable objectTable;
		private readonly DalamudPluginInterface pluginInterface;


		internal ICallGateProvider<string, string?>?			ProviderGetBodyScale;
		internal ICallGateProvider<Character?, string?>?		ProviderGetBodyScaleFromCharacter;
		internal ICallGateProvider<string, string, object>?		ProviderSetBodyScale;
		internal ICallGateProvider<string, Character?, object>?	ProviderSetBodyScaleToCharacter;
		internal ICallGateProvider<string, object>?				ProviderRevert;
		internal ICallGateProvider<Character?, object>?			ProviderRevertCharacter;
		internal ICallGateProvider<string>?						ProviderGetApiVersion;
		internal ICallGateProvider<string?, object?>?			ProviderOnScaleUpdate; //Sends either bodyscale string or null at startup and when scales are saved in the ui

		public CustomizePlusIpc(ObjectTable objectTable, DalamudPluginInterface pluginInterface)
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
			ProviderGetBodyScaleFromCharacter?.UnregisterFunc();
			ProviderSetBodyScale?.UnregisterAction();
			ProviderSetBodyScaleToCharacter?.UnregisterAction();
			ProviderRevert?.UnregisterAction();
			ProviderRevertCharacter?.UnregisterAction();
			ProviderGetApiVersion?.UnregisterFunc();

		}

		private void InitializeProviders()
		{
			PluginLog.Debug("Initializing c+ ipc providers.");
			try
			{
				ProviderGetApiVersion = pluginInterface.GetIpcProvider<string>(LabelProviderApiVersion);
				ProviderGetApiVersion.RegisterFunc(GetApiVersion);
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, $"Error registering IPC provider for {LabelProviderApiVersion}.");
			}

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
				ProviderGetBodyScaleFromCharacter = pluginInterface.GetIpcProvider<Character?, string?>(LabelGetBodyScaleFromCharacter);
				ProviderGetBodyScaleFromCharacter.RegisterFunc(GetBodyScaleFromCharacter);
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, $"Error registering IPC provider for {LabelGetBodyScaleFromCharacter}.");
			}

			try
			{
				ProviderSetBodyScale =
					pluginInterface.GetIpcProvider<string, string, object>(LabelSetBodyScale);
				ProviderSetBodyScale.RegisterAction(SetBodyScale);
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, $"Error registering IPC provider for {LabelSetBodyScale}.");
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
				ProviderRevert =
					pluginInterface.GetIpcProvider<string, object>(LabelRevert);
				ProviderRevert.RegisterAction(Revert);
			}
			catch (Exception ex)
			{
				PluginLog.Error(ex, $"Error registering IPC provider for {LabelRevert}.");
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

		public void OnScaleUpdate(string bodyScaleString)
		{
			PluginLog.Debug("Sending c+ ipc scale message.");
			ProviderOnScaleUpdate?.SendMessage(bodyScaleString);
		}

		private static string GetApiVersion()
			=> ApiVersion;

		private string? GetBodyScale(string characterName)
		{
			BodyScale? bodyScale = Plugin.GetPlayerBodyScale(characterName);
			return bodyScale != null ? JsonConvert.SerializeObject(bodyScale) : null;
		}

		private unsafe string? GetBodyScaleFromCharacter(Character? character)
		{
			if (character == null)
				return null;

			return GetBodyScale(character.Name.ToString());
		}

		private void SetBodyScale(string bodyScaleString, string characterName)
		{
			//Character? character = FindCharacterByName(characterName);
			BodyScale? bodyScale = JsonConvert.DeserializeObject<BodyScale?>(bodyScaleString);
			if (bodyScale != null)
				Plugin.SetTemporaryCharacterScale(characterName, bodyScale);
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

		private Character? FindCharacterByName(string? characterName)
		{
			if (characterName == null)
				return null;

			return objectTable.FirstOrDefault(gameObject => gameObject.Name.ToString() == characterName) as Character;
		}
	}
}
