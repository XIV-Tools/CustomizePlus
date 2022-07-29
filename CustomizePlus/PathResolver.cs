// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Data.Parsing.Uld;
using Penumbra.GameData;
using Penumbra.GameData.ByteString;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;


namespace CustomizePlus
{
	public partial class PathResolver
	{ 
		//private PathResolver? pr;
		public PathResolver()
		{
		}
		//public GCHandleProvider(object target)
		//{
		//	Handle = target.ToGcHandle();
		//}

		//public IntPtr Pointer => Handle.ToIntPtr();

		//public GCHandle Handle { get; }

		public static String? getName(GameObject obj)
		{
			try
			{
				return IdentifyBodyScale(obj);
			}
			catch (Exception ex)
			{
				return "Default";
			}
			return "Default";
		}

		public static string? GetCutsceneName(GameObject obj)
		{
			if (obj.Name.ToString().ToArray()[0] != 0 || obj.ObjectKind != ObjectKind.Player)
			{
				return "Default";
			}

			var player = Plugin.ObjectTable[0];
			if (player == null)
			{
				return "Default";
			}

			//var pc = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)player.Address;
			//return pc->ClassJob == ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)gameObject)->ClassJob ? player.Name.ToString() : null;
			//return player.ObjectKind == obj.ObjectKind ? player.Name.ToString() : null;
			return player.OwnerId == obj.OwnerId ? player.Name.ToString() : "Default";
		}

		private static string? GetPlayerName()
			=> Plugin.ObjectTable[0]?.Name.ToString();

		private static string IdentifyBodyScale(GameObject gameObject)
		{
			/*if (gameObject == null)
			{
				//return Penumbra.CollectionManager.Default;
				return null;
			}*/

			string? actorName = null;
			// Early return if we prefer the actors own name over its owner.
			//actorName = new Penumbra.GameData.ByteString.Utf8String(gameObject->Name).ToString();
			actorName = gameObject.Name.ToString();

			if (actorName.Length > 0)
			{
				return actorName;
			}

			try
			{
				// All these special cases are relevant for an empty name, so never collide with the above setting.
				// Only OwnerName can be applied to something with a non-empty name, and that is the specific case we want to handle.
				//var actualName = gameObject->ObjectIndex switch
				var actualName = gameObject.ObjectKind switch
				{
					ObjectKind.Player => GetPlayerName(), // character window
					ObjectKind.CardStand => GetPlayerName(), // character window
					ObjectKind.Cutscene => GetCutsceneName(gameObject), // character window
					//240 => GetPlayerName(), // character window
					//242 => GetPlayerName(), // try-on
					//243 => GetPlayerName(), // dye preview
					//>= 200 => GetCutsceneName(gameObject),
					_ => GetCutsceneName(gameObject),
					//_ => null,
				};

				// First check temporary character collections, then the own configuration, then special collections.
				//return gameObject.Name.ToString();
				return "Default";
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error identifying collection:\n{e}");
				return "Default";
			}
		}

	}
}

