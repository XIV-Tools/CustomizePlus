// © Customize+.
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CustomizePlus.Memory
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct AddressCountArray<T> : IEnumerable<T>
        where T : unmanaged
    {
        public T* Address;
        public int Count;

        public T this[int index]
        {
            get => Address[index];
            set => Address[index] = value;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }
    }
}