// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Memory;

namespace CustomizePlus.Data
{
	internal static class Constants
	{
		/// <summary>
		/// Version of the configuration file, when increased a converter should be implemented if necessary.
		/// </summary>
		public const int ConfigurationVersion = 3;

		/// <summary>
		/// The name of the root bone.
		/// </summary>
		public const string RootBoneName = "n_root";

		/// <summary>
		/// How many state changes the editor interface will track before forgetting old ones.
		/// This is a pretty arbitrary number -- it's not like it's so much data that performance
		/// would take a hit either way, probably.
		/// </summary>
		public const int MaxUndoFrames = 80;

		/// <summary>
		/// Dalamud object table index for "filler" event objects. Can be ignored(?).
		/// </summary>
		public const int ObjectTableFillerIndex = 245;

		/// <summary>
		/// Predicate function for determining if the given object table index represents a
		/// cutscene NPC object, with a small buffer in case the prior entries overflow past index 200.
		/// </summary>
		public static bool InObjectTableCutsceneNPCRange(int index) => index > 202 && index < 240;

		/// <summary>
		/// Predicate function for determining if the given object table index represents an
		/// NPC in a busy area (i.e. there are ~245 other objects already).
		/// </summary>
		public static bool InObjectTableBusyNPCRange(int index) => index > 245;

		/// <summary>
		/// A "null" havok vector. Since the type isn't inherently nullable, and the default value (0, 0, 0, 0)
		/// is valid input in a lot of cases, we can use this instead.
		/// </summary>
		public static readonly FFXIVClientStructs.Havok.hkVector4f NullVector = new()
		{
			X = float.NaN,
			Y = float.NaN,
			Z = float.NaN,
			W = float.NaN
		};
		/// <summary>
		/// A "null" havok quaternion. Since the type isn't inherently nullable, and the default value (0, 0, 0, 0)
		/// is valid input in a lot of cases, we can use this instead.
		/// </summary>
		public static readonly FFXIVClientStructs.Havok.hkQuaternionf NullQuaternion = new()
		{
			X = float.NaN,
			Y = float.NaN,
			Z = float.NaN,
			W = float.NaN
		};

		/// <summary>
		/// A "null" havok transform. Since the type isn't inherently nullable, and the default values
		/// aren't immediately obviously wrong, we can use this instead.
		/// </summary>
		public static readonly FFXIVClientStructs.Havok.hkQsTransformf NullTransform = new()
		{
			Translation = NullVector,
			Rotation = NullQuaternion,
			Scale = NullVector
		};
	}
}
