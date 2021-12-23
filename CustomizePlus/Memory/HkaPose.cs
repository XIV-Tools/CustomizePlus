// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Memory
{
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct HkaPose
	{
		[FieldOffset(0x000)] public HkaSkeleton* Skeleton;
		[FieldOffset(0x010)] public CountAddressArray<Transform> Transforms;
	}
}
