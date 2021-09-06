// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.GameStructs
{
	using System;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Explicit)]
	public struct Skeleton
	{
		[FieldOffset(0x140)] public IntPtr Body;
		[FieldOffset(0x300)] public IntPtr Head;
		[FieldOffset(0x4C0)] public IntPtr Hair;
		[FieldOffset(0x680)] public IntPtr Met;
		[FieldOffset(0x840)] public IntPtr Top;
	}
}
