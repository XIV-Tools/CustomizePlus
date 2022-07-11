// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Memory
{
	using System;
	using System.Runtime.InteropServices;
	using Dalamud.Game.ClientState.Objects.Types;

	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct RenderSkeleton
	{
		[FieldOffset(0x050)] public short Length;
		[FieldOffset(0x068)] public PartialSkeleton* PartialSkeletons;
	}

	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct RenderObject
	{
		[FieldOffset(0x050 + 0x020)] public HkVector4 Scale;
		[FieldOffset(0x0A0)] public RenderSkeleton* Skeleton;

		public static RenderObject* FromActor(Character p)
		{
			if (p.Address == IntPtr.Zero)
				return null;

			IntPtr drawObject = Marshal.ReadIntPtr(p.Address, 0x00F0);
			if (drawObject == IntPtr.Zero)
				return null;

			return (RenderObject*)drawObject.ToPointer();
		}
	}
}
