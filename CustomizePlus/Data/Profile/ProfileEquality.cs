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
            return x != null && y != null
&& x.UniqueId == y.UniqueId
                   && x.CreationDate == y.CreationDate
                   && x.CharacterName == y.CharacterName
                   && x.ProfileName == y.ProfileName;
        }

        public override int GetHashCode([DisallowNull] CharacterProfile obj)
        {
            return obj.UniqueId;
        }
    }
}