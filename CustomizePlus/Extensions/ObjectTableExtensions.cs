using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Extensions
{
    /// <summary>
    ///     Extensions for <see cref="ObjectTable" />.
    /// </summary>
    public static class ObjectTableExtensions
    {
        /// <summary>
        ///     Gets all <see cref="PlayerCharacter" />s in the <see cref="ObjectTable" />.
        /// </summary>
        /// <param name="objectTable"></param>
        /// <param name="includeSelf">Whether or not to include the local player.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="PlayerCharacter" />s.</returns>
        public static IEnumerable<PlayerCharacter> GetPlayerCharacters(this ObjectTable objectTable, bool includeSelf = true) => objectTable
            .Where(x => x is PlayerCharacter).Cast<PlayerCharacter>()
            .Where(x => includeSelf || x.ObjectId != DalamudServices.ClientState.LocalPlayer?.ObjectId)
            .Where(x => x.ObjectId > 240);

        /// <summary>
        ///     Gets all player owned Characters in the <see cref="ObjectTable" />.
        /// </summary>
        /// <param name="objectTable"></param>
        /// <returns></returns>
        public static IEnumerable<PlayerCharacter> GetPlayerOwnedCharacters(this ObjectTable objectTable) => objectTable
            .Where(x => x is PlayerCharacter).Cast<PlayerCharacter>()
            .Where(x => x.ObjectId > 240)
            .Where(x => x.OwnerId == DalamudServices.ClientState.LocalPlayer?.ObjectId || x.ObjectId == DalamudServices.ClientState.LocalPlayer?.ObjectId)
            .Where(x => x.ObjectId == 200 || x.ObjectId == 201);
    }
}
