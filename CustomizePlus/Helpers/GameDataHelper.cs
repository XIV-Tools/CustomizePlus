// © Customize+.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

using CustomizePlus.Data.Profile;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Penumbra.String;

using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

using DalamudObject = Dalamud.Game.ClientState.Objects.Types.GameObject;
using DalamudObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
using FFXIVClientCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using FFXIVClientObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace CustomizePlus.Helpers
{
    public static class GameDataHelper
    {
        public static DalamudObject? FindModelByName(string name)
        {
            return DalamudServices.ObjectTable.FirstOrDefault(x => x.Name.ToString() == name);
        }

        public static unsafe FFXIVClientObject* ToClientObject(this DalamudObject obj)
        {
            if (obj.Address is IntPtr objPtr
                && objPtr != IntPtr.Zero)
            {
                var clientObj = (FFXIVClientObject*)objPtr;
                if (clientObj != null)
                {
                    return clientObj;
                }
            }

            return null;
        }

        public static unsafe CharacterBase* ToCharacterBase(this DalamudObject obj)
        {
            return (CharacterBase*)ToClientObject(obj);
        }

        public static unsafe bool TryLookupCharacterBase(string name, out CharacterBase* cBase)
        {
            if (FindModelByName(name) is DalamudObject obj
                && obj.Address is IntPtr objPtr
                && objPtr != IntPtr.Zero)
            {
                var clientObj = (FFXIVClientObject*)objPtr;
                if (clientObj != null)
                {
                    cBase = (CharacterBase*)clientObj->DrawObject;
                    return true;
                }
            }

            cBase = null;
            return false;
        }

        //public static unsafe (CharacterProfile, bool) FindScale(int objectIndex)
        //{
        //    //Determine player object for root scale behavior later. Have to catch errors for zone transitions.
        //    uint playerObjId = 0;
        //    try
        //    {
        //        playerObjId = DalamudServices.ObjectTable[0].ObjectId;
        //    }
        //    catch (Exception ex) { }

        //    var obj = DalamudServices.ObjectTable[objectIndex];

        //    if (obj == null)
        //        return (null, false);

        //    CharacterProfile? scale = null;
        //    // Mare Support: Bool check to see if override table from IPC can be used
        //    FFXIVClientObject* objPtr = (FFXIVClientObject*)DalamudServices.ObjectTable.GetObjectAddress(objectIndex);
        //    scale = IdentifyBodyScale(objPtr, playerObjId != obj.ObjectId);

        //    bool isCutsceneNpc = false;
        //    switch (obj.ObjectKind)
        //    {
        //        case DalamudObjectKind.Player:
        //        //case DalamudObjectKind.Companion:
        //        case DalamudObjectKind.Retainer:
        //        case DalamudObjectKind.EventNpc:
        //        case DalamudObjectKind.BattleNpc:
        //            isCutsceneNpc = objectIndex >= 200 && objectIndex < 246;
        //            // Stop if NPCs disabled by config. Have to double check cutscene range due to the 200/201 issue.
        //            if (!ConfigurationManager.Configuration.ApplyToNpcs && !isCutsceneNpc)
        //                return (null, false);
        //            else if (isCutsceneNpc && !ConfigurationManager.Configuration.ApplyToNpcsInCutscenes)
        //                return (null, false);
        //            // Choose most appropriate default, or fallback to null.
        //            if (isCutsceneNpc)
        //                scale = scale ?? defaultCutsceneScale ?? defaultScale ?? null;
        //            else
        //                scale = scale ?? defaultScale ?? null;
        //            break;
        //        default:
        //            return (null, false);
        //    }

        //    // No scale to apply, move on.
        //    if (scale == null)
        //        return (null, false);

        //    // Don't apply root scales to NPCs in cutscenes or battle NPCs. Both cause animation or camera issues. Exception made for player pets
        //    bool applyRootScale = !isCutsceneNpc && (obj.ObjectKind != DalamudObjectKind.BattleNpc || obj.OwnerId == playerObjId);
        //    //!(isCutsceneNpc || (obj.ObjectKind == ObjectKind.BattleNpc && obj.OwnerId != playerObjId));

        //    return (scale, applyRootScale);
        //}

        //// All functions related to this process for non-named objects adapted from Penumbra logic. Credit to Ottermandias et al.
        //public static unsafe CharacterProfile? IdentifyBodyScale(FFXIVClientObject* obj, bool allowIPC)
        //{
        //    if (obj == null)
        //    {
        //        return null;
        //    }

        //    string? actorName = null;
        //    CharacterProfile? scale = null;

        //    try
        //    {
        //        actorName = new ByteString(obj->Name).ToString();

        //        if (string.IsNullOrEmpty(actorName))
        //        {
        //            string? actualName = null;

        //            // Check if in pvp intro sequence, which uses 240-244 for the 5 players, and only affect the first if so
        //            // TODO: Ensure player side only. First group, where one of the node textures is blue. Alternately, look for hidden party list UI and get names from there.
        //            if (DalamudServices.GameGui.GetAddonByName("PvPMKSIntroduction", 1) == IntPtr.Zero)
        //            {
        //                actualName = obj->ObjectIndex switch
        //                {
        //                    240 => GetPlayerName(), // character window
        //                    241 => GetInspectName() ?? GetGlamourName(), // GetCardName() ?? // inspect, character card, glamour plate editor. - Card removed due to logic issues
        //                    242 => GetPlayerName(), // try-on
        //                    243 => GetPlayerName(), // dye preview
        //                    244 => GetPlayerName(), // portrait preview
        //                    >= 200 => GetCutsceneName(obj),
        //                    _ => null,
        //                } ?? new ByteString(obj->Name).ToString();
        //            }
        //            else
        //            {
        //                actualName = obj->ObjectIndex switch
        //                {
        //                    240 => GetPlayerName(), // character window
        //                    _ => null,
        //                } ?? new ByteString(obj->Name).ToString();
        //            }

        //            if (actualName == null)
        //            {
        //                return null;
        //            }

        //            actorName = actualName;
        //        }

        //        scale = IdentifyBodyScaleByName(actorName, allowIPC);
        //    }
        //    catch (Exception e)
        //    {
        //        Dalamud.Logging.PluginLog.LogError($"Error identifying CharacterProfile:\n{e}");
        //        return null;
        //    }

        //    return scale;
        //}


        //private static CharacterProfile? IdentifyBodyScaleByName(string actorName, bool allowIPC = false, bool playerOnly = false)
        //{
        //    CharacterProfile? prof = null;
        //    if (allowIPC)
        //    {
        //        if (!scaleOverride.TryGetValue(actorName, out prof))
        //            NameToScale.TryGetValue(actorName, out prof);
        //    }
        //    else if (playerOnly && DalamudServices.ObjectTable[0] != null)
        //    {
        //        if (DalamudServices.ObjectTable[0].Name.TextValue == actorName)
        //            NameToScale.TryGetValue(actorName, out prof);
        //    }
        //    else
        //    {
        //        NameToScale.TryGetValue(actorName, out prof);
        //    }

        //    return prof;
        //}
        

        // Checks Customization (not ours) of the cutscene model vs the player model to see if
        // the player name should be used.
        [Obsolete]
        public static unsafe string? GetCutsceneName(FFXIVClientObject* gameObject)
        {
            if (gameObject->Name[0] != 0 || gameObject->ObjectKind != (byte)DalamudObjectKind.Player)
            {
                return null;
            }

            var player = DalamudServices.ObjectTable[0];
            if (player == null)
            {
                return null;
            }

            var customizeEqual = true;
            var customize1 = ((FFXIVClientCharacter*)gameObject)->CustomizeData;
            var customize2 = ((FFXIVClientCharacter*)player.Address)->CustomizeData;
            for (var i = 0; i < 26; i++)
            {
                var data1 = Marshal.ReadByte((IntPtr)customize1, i);
                var data2 = Marshal.ReadByte((IntPtr)customize2, i);
                if (data1 != data2)
                {
                    customizeEqual = false;
                    break;
                }
            }

            return customizeEqual ? player.Name.ToString() : null;
        }

        public static unsafe string? GetInspectName()
        {
            var addon = DalamudServices.GameGui.GetAddonByName("CharacterInspect");
            if (addon == IntPtr.Zero)
            {
                return null;
            }

            var ui = (AtkUnitBase*)addon;
            if (ui->UldManager.NodeListCount < 60)
            {
                return null;
            }

            var text = (AtkTextNode*)ui->UldManager.NodeList[59];
            if (text == null || !text->AtkResNode.IsVisible)
            {
                text = (AtkTextNode*)ui->UldManager.NodeList[60];
            }

            return text != null ? text->NodeText.ToString() : null;
        }

        // Obtain the name displayed in the Character Card from the agent.
        public static unsafe string? GetCardName()
        {
            var uiModule = (UIModule*)DalamudServices.GameGui.GetUIModule();
            var agentModule = uiModule->GetAgentModule();
            var agent = (byte*)agentModule->GetAgentByInternalID(393);
            if (agent == null)
            {
                return null;
            }

            var data = *(byte**)(agent + 0x28);
            if (data == null)
            {
                return null;
            }

            var block = data + 0x7A;
            return new ByteString(block).ToString();
        }

        // Obtain the name of the player character if the glamour plate edit window is open.
        public static string? GetGlamourName()
        {
            var addon = DalamudServices.GameGui.GetAddonByName("MiragePrismMiragePlate");
            return addon == IntPtr.Zero ? null : GetPlayerName();
        }

        public static string? GetPlayerName()
        {
            return DalamudServices.ObjectTable[0]?.Name.ToString();
        }

        public unsafe static string? GetPlayerTargetName()
        {
            DalamudObject? target = DalamudServices.ObjectTable[0]?.TargetObject;

            //make sure that the player is actually targeting something
            //and then make sure that the target in question is actually something with a skeleton

            if (target != null
                && target.Address is IntPtr tgtPtr
                && tgtPtr != IntPtr.Zero)
            {
                var clientObj = (FFXIVClientObject*)tgtPtr;
                if (clientObj != null)
                {
                    var cBase = (CharacterBase*)clientObj->DrawObject;
                    
                    if (cBase != null)
                    {
                        return new ByteString(clientObj->Name).ToString();
                    }
                }
            }

            return null;
        }

        /*
		public static void SetTemporaryCharacterScale(string characterName, CharacterProfile scale)
		{
			if (string.IsNullOrEmpty(characterName))
				return;
			scaleOverride[characterName] = scale;
		}

		public static bool RemoveTemporaryCharacterScale(string characterName)
		{
			return scaleOverride.TryRemove(characterName, out _);
		}

		public static CharacterProfile? GetBodyScale(string characterName)
		{
			if (string.IsNullOrEmpty(characterName))
				return null;
			return IdentifyBodyScaleByName(characterName, true);
		}

		public static CharacterProfile? GetPlayerBodyScale(string characterName)
		{
			if (string.IsNullOrEmpty(characterName))
				return null;
			return IdentifyBodyScaleByName(characterName, false, true);
		}
		*/
    }
}