// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Memory
{
	using System;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct HkaSkeleton
	{
		[FieldOffset(0x018)] public AddressCountArray<short> ParentIndices;
		[FieldOffset(0x028)] public AddressCountArray<HkaBone> Bones;
	}
}
