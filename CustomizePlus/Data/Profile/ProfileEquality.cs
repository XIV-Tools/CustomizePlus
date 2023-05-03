// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
			else
			{
				return x.UniqueID == y.UniqueID
					&& x.CreationDate == y.CreationDate
					&& x.CharacterName == y.CharacterName
					&& x.ProfileName == y.ProfileName;
			}
		}

		public override int GetHashCode([DisallowNull] CharacterProfile obj)
		{
			return obj.UniqueID;
		}
	}
}
