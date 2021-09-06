// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.GameStructs
{
	using System;
	using System.Runtime.InteropServices;

	// We dont know what this structure is
	[StructLayout(LayoutKind.Explicit)]
	internal struct SkeletonWrapper
	{
		[FieldOffset(0x68)] public IntPtr Skeleton;
	}
}
