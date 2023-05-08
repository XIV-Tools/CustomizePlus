// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Data
{
	internal static class Constants
	{
		/// <summary>
		/// Version of the configuration file, when increased a converter should be implemented if necessary.
		/// </summary>
		public const int ConfigurationVersion = 3;

		/// <summary>
		/// Default save location in which to store character profiles. Path will be expanded at runtime
		/// into regular "My Documents" folder, unless changed by user.
		/// </summary>
		public const string DefaultProfileDirectory = @"%UserProfile%\Documents\CustomizePlus\";

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
	}
}
