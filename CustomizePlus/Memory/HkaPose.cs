// © Customize+.
// Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace CustomizePlus.Memory
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct HkaPose
    {
        [FieldOffset(0x000)] public HkaSkeleton* Skeleton;
        [FieldOffset(0x010)] public CountAddressArray<Transform> Transforms;
    }
}