// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
//using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Data.Parsing.Uld;
using Penumbra.GameData;
using Penumbra.GameData.ByteString;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;


namespace CustomizePlus
{
	public unsafe partial class PathResolverUnsafe
	{

		internal String? IdentifyBodyScale(GameObject obj)
		{
			try
			{
				//List<GameObject> list1 = new List<GameObject>();
				//list1.Add(obj);
				//GCHandle handle1 = GCHandle.Alloc(obj);
				//FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject parameter = ;
				// call WinAPi and pass the parameter here
				// then free the handle when not needed:
				//handle1.Free();
				
				//return this.IdentifyBodyScale(obj);
			}
			catch (Exception e){

			}
			return null;
		}

		private static string? GetCutsceneName(FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* gameObject)
		{
			if (gameObject->Name[0] != 0 || gameObject->ObjectKind != (byte)ObjectKind.Player)
			{
				return null;
			}

			var player = Plugin.ObjectTable[0];
			if (player == null)
			{
				return null;
			}

			var pc = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)player.Address;
			return pc->ClassJob == ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)gameObject)->ClassJob ? player.Name.ToString() : null;
		}

		private static string? GetPlayerName()
			=> Plugin.ObjectTable[0]?.Name.ToString();


		public static string IdentifyBodyScale(GameObject* gameObject)
		{
			/*if (gameObject == null)
			{
				//return Penumbra.CollectionManager.Default;
				return null;
			}*/

				string? actorName = null;
			// Early return if we prefer the actors own name over its owner.
			//actorName = new Penumbra.GameData.ByteString.Utf8String(gameObject->Name).ToString();
			actorName = new Penumbra.GameData.ByteString.Utf8String(gameObject->Name).ToString();

				if (actorName.Length > 0) {
					return actorName;
				}

			try
			{
				// All these special cases are relevant for an empty name, so never collide with the above setting.
				// Only OwnerName can be applied to something with a non-empty name, and that is the specific case we want to handle.
				//var actualName = gameObject->ObjectIndex switch
				var actualName = gameObject->ObjectIndex switch
				{
					240 => GetPlayerName(), // character window
					242 => GetPlayerName(), // try-on
					243 => GetPlayerName(), // dye preview
					>= 200 => GetCutsceneName(gameObject),
					_ => null,
				};

				// First check temporary character collections, then the own configuration, then special collections.
				return new Penumbra.GameData.ByteString.Utf8String(gameObject->Name).ToString();
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error identifying collection:\n{e}");
				return "Default";
			}
		}

	}


}
