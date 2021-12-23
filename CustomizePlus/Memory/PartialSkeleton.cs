// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Memory
{
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Explicit, Size = 448)]
	public unsafe struct PartialSkeleton
	{
		[FieldOffset(0x140)] public HkaPose* Pose1;
		[FieldOffset(0x148)] public HkaPose* Pose2;
		[FieldOffset(0x150)] public HkaPose* Pose3;
		[FieldOffset(0x158)] public HkaPose* Pose4;

		[FieldOffset(352)] public RenderSkeleton* RenderSkeletonPtr;
	}
}
