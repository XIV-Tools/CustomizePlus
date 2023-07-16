using CustomizePlus.Core;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Collections.Generic;

namespace CustomizePlus.Services
{
    internal class PlayerOwnedObjectsService : ServiceBase<PlayerOwnedObjectsService>
    {
        public List<nint> PlayerOwnedObjects { get; private set; } = new();
        public override void Tick(float delta)
        {
            PlayerOwnedObjects.Clear();

            var player = DalamudServices.ClientState.LocalPlayer;
            if (player == null) return;

            var playerPointer = player.Address;
            PlayerOwnedObjects.Add(playerPointer);

            var minion = GetMinionOrMount(playerPointer);
            if (minion != nint.Zero) PlayerOwnedObjects.Add(minion);

            var pet = GetPet(playerPointer);
            if (pet != nint.Zero) PlayerOwnedObjects.Add(pet);

            var companion = GetCompanion(playerPointer);
            if (companion != nint.Zero) PlayerOwnedObjects.Add(companion);
        }

        private unsafe nint GetMinionOrMount(nint playerPointer)
        {
            return DalamudServices.ObjectTable.GetObjectAddress(((GameObject*)playerPointer)->ObjectIndex + 1);
        }

        private unsafe nint GetPet(nint playerPointer)
        {
            var mgr = CharacterManager.Instance();
            if ((nint)mgr == nint.Zero) return nint.Zero;
            return (nint)mgr->LookupPetByOwnerObject((BattleChara*)playerPointer);
        }

        private unsafe nint GetCompanion(nint playerPointer)
        {
            var mgr = CharacterManager.Instance();
            if ((nint)mgr == nint.Zero) return nint.Zero;
            return (nint)mgr->LookupBuddyByOwnerObject((BattleChara*)playerPointer);
        }
    }
}
