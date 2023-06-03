// © Customize+.
// Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace CustomizePlus.Memory
{
    [StructLayout(LayoutKind.Explicit)]
    public struct HkaSkeleton
    {
        [FieldOffset(0x018)] public AddressCountArray<short> ParentIndices;
        [FieldOffset(0x028)] public AddressCountArray<HkaBone> Bones;
    }
}