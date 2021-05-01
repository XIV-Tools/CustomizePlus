// © Anamnesis.
// Developed by W and A Walsh.
// Licensed under the MIT license.

namespace Anamnesis.Memory
{
	using System;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Explicit)]
	public struct Model
	{
		[FieldOffset(0x050)] public Transform Transform;
		[FieldOffset(0x0A0)] public IntPtr Skeleton;
		[FieldOffset(0x148)] public IntPtr Bust;
		[FieldOffset(0x240)] public IntPtr ExtendedAppearance;
		[FieldOffset(0x26C)] public float Height;
	}
}
