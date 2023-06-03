// © Customize+.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CustomizePlus.Data.Profile
{
    internal class ProfileEquality : EqualityComparer<CharacterProfile>
    {
        public override bool Equals(CharacterProfile? x, CharacterProfile? y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            return x.UniqueID == y.UniqueID
                   && x.CreationDate == y.CreationDate
                   && x.CharName == y.CharName
                   && x.ProfName == y.ProfName;
        }

        public override int GetHashCode([DisallowNull] CharacterProfile obj)
        {
            return obj.UniqueID;
        }
    }
}