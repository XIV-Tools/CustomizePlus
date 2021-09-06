// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.GameStructs
{
	using System;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Explicit)]
	internal struct Bones
	{
		[FieldOffset(0x10)]
		public int Count;

		[FieldOffset(0x18)]
		public IntPtr TransformArray;
	}
}
