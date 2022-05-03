// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Memory
{
	using System.Collections;
	using System.Collections.Generic;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct AddressCountArray<T> : IEnumerable<T>
		where T : unmanaged
	{
		public T* Address;
		public int Count;

		public T this[int index]
		{
			get => this.Address[index];
			set => this.Address[index] = value;
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < this.Count; i++)
			{
				yield return this[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			for (int i = 0; i < this.Count; i++)
			{
				yield return this[i];
			}
		}
	}
}
