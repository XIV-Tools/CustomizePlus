// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusLib.Anamnesis.Skeletons
{
	using CustomizePlus.GameStructs;

	internal static class SkeletonService
	{
		private static readonly HrothgarSkeleton Hrothgar = new HrothgarSkeleton();
		private static readonly VieraSkeleton Viera = new VieraSkeleton();
		private static readonly MidlanderSkeleton Midlander = new MidlanderSkeleton();

		internal static SkeletonBase GetSkeleton(Appearance customize)
		{
			switch (customize.Race)
			{
				case Appearance.Races.Hrothgar: return Hrothgar;
				case Appearance.Races.Viera: return Viera;
				default: return Midlander;
			}
		}
	}
}
